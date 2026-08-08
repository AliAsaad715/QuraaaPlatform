using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.AiAssistant.Commands.TranslateText
{
    public class TranslateTextCommandHandler
        : BaseApplicationService<TranslateTextCommandHandler>,
          IRequestHandler<TranslateTextCommand, AppResult<TranslateTextResponse>>
    {
        // A translated page runs about as long as its source, and a dense printed
        // page can hold more text than the old free-text selection allowed (5,000
        // chars) — sized generously so the translation isn't cut off mid-page.
        private const int MaxTranslationTokens = 1500;

        private readonly IBookPurchaseRepository _purchaseRepository;
        private readonly IDocumentTextExtractionService _textExtractionService;
        private readonly IOpenAiService _openAiService;
        private readonly IAiUsageLimiterService _usageLimiter;

        public TranslateTextCommandHandler(
            IBookPurchaseRepository purchaseRepository,
            IDocumentTextExtractionService textExtractionService,
            IOpenAiService openAiService,
            IAiUsageLimiterService usageLimiter,
            ILogger<TranslateTextCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _purchaseRepository = purchaseRepository;
            _textExtractionService = textExtractionService;
            _openAiService = openAiService;
            _usageLimiter = usageLimiter;
        }

        public async Task<AppResult<TranslateTextResponse>> Handle(
            TranslateTextCommand request,
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

                // Split so the two failure modes map to the request property a client
                // can actually act on: a missing canonical file is a book-level problem
                // (DocumentUrl), while unreadable page content means try another page
                // number (PageNumber) — neither is the caller being unauthenticated.
                if (string.IsNullOrWhiteSpace(context.CanonicalPdfUrl))
                    throw new ApplicationBusinessException(
                        "This book doesn't have a readable PDF available for translation.",
                        "DocumentUrl");

                var pageText = await _textExtractionService.ExtractPageTextAsync(
                    context.CanonicalPdfUrl, request.PageNumber, cancellationToken);

                if (string.IsNullOrWhiteSpace(pageText))
                    throw new ApplicationBusinessException(
                        "Could not extract text from that page. Try a different page.",
                        "PageNumber");

                var targetLanguageName = request.TargetLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase)
                    ? "Arabic" : "English";

                var systemPrompt =
                    $"You are a translator inside a book reading app. Translate the " +
                    $"page text below into {targetLanguageName}. Reply with only the " +
                    $"translation — no explanation, no notes.";

                var translated = await _openAiService.GetCompletionAsync(
                    systemPrompt, pageText, maxTokens: MaxTranslationTokens, cancellationToken);

                if (translated is null)
                    throw new ApplicationBusinessException("The AI assistant is temporarily unavailable. Please try again.");

                return new TranslateTextResponse(translated);
            }, "Text translated successfully.");
        }
    }
}