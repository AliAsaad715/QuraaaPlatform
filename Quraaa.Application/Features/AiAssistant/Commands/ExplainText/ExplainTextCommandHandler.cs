using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.AiAssistant.Commands.ExplainText
{
    public class ExplainTextCommandHandler
        : BaseApplicationService<ExplainTextCommandHandler>,
          IRequestHandler<ExplainTextCommand, AppResult<ExplainTextResponse>>
    {
        // Always explains in Arabic regardless of the input language's
        // script — that's FR-AI-03 as written ("...in simple Arabic"), not a
        // language the client chooses, unlike TranslateText's TargetLanguage.
        // {0}/{1} are the book's title/author, giving the model context about
        // what the reader is actually reading.
        private const string SystemPromptTemplate =
            "You are a reading assistant inside a book app. The reader is reading " +
            "\"{0}\" by {1} and selected a difficult word or sentence from it that " +
            "they want explained simply. Always reply in Arabic, in plain, easy " +
            "language suitable for someone building their vocabulary — regardless " +
            "of what language the selected text is in. Reply with only the " +
            "explanation, no preamble.";

        private readonly IBookPurchaseRepository _purchaseRepository;
        private readonly IOpenAiService _openAiService;
        private readonly IAiUsageLimiterService _usageLimiter;

        public ExplainTextCommandHandler(
            IBookPurchaseRepository purchaseRepository,
            IOpenAiService openAiService,
            IAiUsageLimiterService usageLimiter,
            ILogger<ExplainTextCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _purchaseRepository = purchaseRepository;
            _openAiService = openAiService;
            _usageLimiter = usageLimiter;
        }

        public async Task<AppResult<ExplainTextResponse>> Handle(
            ExplainTextCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                // Ownership check happens before the usage check, and before any AI
                // call — so a purchase that doesn't exist or belongs to someone else
                // never costs the caller a daily AI request. A missing purchase and
                // someone else's purchase both surface as 404, so this can't be used
                // to enumerate valid purchase IDs.
                var context = await _purchaseRepository.GetPurchaseBookContextAsync(
                    request.PurchaseId, cancellationToken);

                if (context is null || context.UserId != request.UserId)
                    throw new NotFoundException("Purchase not found.");

                var usage = await _usageLimiter.TryConsumeAsync(request.UserId, cancellationToken);
                if (!usage.Allowed)
                    throw new ApplicationBusinessException(
                        $"Daily AI usage limit reached ({usage.DailyLimit} requests/day). Try again tomorrow.");

                var systemPrompt = string.Format(SystemPromptTemplate, context.Title, context.Author);

                var explanation = await _openAiService.GetCompletionAsync(
                    systemPrompt, request.SelectedText, maxTokens: 800, cancellationToken);

                if (explanation is null)
                    throw new ApplicationBusinessException("The AI assistant is temporarily unavailable. Please try again.");

                return new ExplainTextResponse(explanation);
            }, "Text explained successfully.");
        }
    }
}