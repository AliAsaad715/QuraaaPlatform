using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.AiAssistant.Commands.ExplainText
{
    public class ExplainTextCommandHandler
        : BaseApplicationService<ExplainTextCommandHandler>,
          IRequestHandler<ExplainTextCommand, AppResult<ExplainTextResponse>>
    {
        // Always explains in Arabic regardless of the input language's
        // script — that's FR-AI-03 as written ("...in simple Arabic"), not a
        // language the client chooses, unlike TranslateText's TargetLanguage.
        private const string SystemPrompt =
            "You are a reading assistant inside a book app. The reader selected a " +
            "difficult word or sentence and wants it explained simply. Always reply " +
            "in Arabic, in plain, easy language suitable for someone building their " +
            "vocabulary — regardless of what language the selected text is in. Reply " +
            "with only the explanation, no preamble.";

        private readonly IOpenAiService _openAiService;
        private readonly IAiUsageLimiterService _usageLimiter;

        public ExplainTextCommandHandler(
            IOpenAiService openAiService,
            IAiUsageLimiterService usageLimiter,
            ILogger<ExplainTextCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _openAiService = openAiService;
            _usageLimiter = usageLimiter;
        }

        public async Task<AppResult<ExplainTextResponse>> Handle(
            ExplainTextCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var usage = await _usageLimiter.TryConsumeAsync(request.UserId, cancellationToken);
                if (!usage.Allowed)
                    throw new ApplicationBusinessException(
                        $"Daily AI usage limit reached ({usage.DailyLimit} requests/day). Try again tomorrow.");

                var explanation = await _openAiService.GetCompletionAsync(
                    SystemPrompt, request.Text, maxTokens: 200, cancellationToken);

                if (explanation is null)
                    throw new ApplicationBusinessException("The AI assistant is temporarily unavailable. Please try again.");

                return new ExplainTextResponse(explanation);
            }, "Text explained successfully.");
        }
    }
}