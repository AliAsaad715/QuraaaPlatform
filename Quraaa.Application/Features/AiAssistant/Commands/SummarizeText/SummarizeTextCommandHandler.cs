using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.AiAssistant.Commands.SummarizeText
{
    public class SummarizeTextCommandHandler
        : BaseApplicationService<SummarizeTextCommandHandler>,
          IRequestHandler<SummarizeTextCommand, AppResult<SummarizeTextResponse>>
    {
        // Prompt strategy lives here, in the Application layer, not inside
        // OpenAiService — wording/tone is a product decision, calling the
        // API is an Infrastructure concern. Keeps them separable.
        private const string SystemPrompt =
            "You are a reading assistant inside a book app. Summarize the text " +
            "the reader selected concisely, preserving key points and structure. " +
            "Reply with only the summary — no preamble, no restating the instructions.";

        private readonly IOpenAiService _openAiService;
        private readonly IAiUsageLimiterService _usageLimiter;

        public SummarizeTextCommandHandler(
            IOpenAiService openAiService,
            IAiUsageLimiterService usageLimiter,
            ILogger<SummarizeTextCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _openAiService = openAiService;
            _usageLimiter = usageLimiter;
        }

        public async Task<AppResult<SummarizeTextResponse>> Handle(
            SummarizeTextCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                // Checked BEFORE calling OpenAI — FR-AI-05 exists for cost
                // control, which only works if a rejected request never
                // actually reaches the (paid) API call.
                var usage = await _usageLimiter.TryConsumeAsync(request.UserId, cancellationToken);
                if (!usage.Allowed)
                    throw new ApplicationBusinessException(
                        $"Daily AI usage limit reached ({usage.DailyLimit} requests/day). Try again tomorrow.");

                var summary = await _openAiService.GetCompletionAsync(
                    SystemPrompt, request.Text, maxTokens: 400, cancellationToken);

                if (summary is null)
                    throw new ApplicationBusinessException("The AI assistant is temporarily unavailable. Please try again.");

                return new SummarizeTextResponse(summary);
            }, "Text summarized successfully.");
        }
    }
}