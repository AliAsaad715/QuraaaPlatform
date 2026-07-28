using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.AiAssistant.Commands.TranslateText
{
    public class TranslateTextCommandHandler
        : BaseApplicationService<TranslateTextCommandHandler>,
          IRequestHandler<TranslateTextCommand, AppResult<TranslateTextResponse>>
    {
        private readonly IOpenAiService _openAiService;
        private readonly IAiUsageLimiterService _usageLimiter;

        public TranslateTextCommandHandler(
            IOpenAiService openAiService,
            IAiUsageLimiterService usageLimiter,
            ILogger<TranslateTextCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _openAiService = openAiService;
            _usageLimiter = usageLimiter;
        }

        public async Task<AppResult<TranslateTextResponse>> Handle(
            TranslateTextCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var usage = await _usageLimiter.TryConsumeAsync(request.UserId, cancellationToken);
                if (!usage.Allowed)
                    throw new ApplicationBusinessException(
                        $"Daily AI usage limit reached ({usage.DailyLimit} requests/day). Try again tomorrow.");

                var targetLanguageName = request.TargetLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase)
                    ? "Arabic" : "English";

                var systemPrompt =
                    $"You are a translator inside a book reading app. Translate the reader's " +
                    $"selected text into {targetLanguageName}. Reply with only the translation " +
                    $"— no explanation, no notes.";

                var translated = await _openAiService.GetCompletionAsync(
                    systemPrompt, request.Text, maxTokens: 600, cancellationToken);

                if (translated is null)
                    throw new ApplicationBusinessException("The AI assistant is temporarily unavailable. Please try again.");

                return new TranslateTextResponse(translated);
            }, "Text translated successfully.");
        }
    }
}