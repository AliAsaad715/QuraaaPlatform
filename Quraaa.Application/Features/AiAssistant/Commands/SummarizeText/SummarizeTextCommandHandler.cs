using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Features.Purchases.Common;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Files;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.AiAssistant.Commands.SummarizeText
{
    public class SummarizeTextCommandHandler
        : BaseApplicationService<SummarizeTextCommandHandler>,
          IRequestHandler<SummarizeTextCommand, AppResult<SummarizeTextResponse>>
    {
        // Roughly 4-5k tokens of source text — enough for a real summary without
        // sending an entire book through a paid completion call. This summarizes
        // the book's opening rather than a true whole-book (map-reduce) digest;
        // revisit if full-book coverage becomes a requirement.
        private const int MaxExtractedCharacters = 20_000;

        // Arabic script tokenizes at roughly 2-3x the characters-per-token rate of
        // Latin script, so an Arabic-heavy excerpt needs a proportionally smaller
        // character budget to land in the same token budget — which matters on the
        // free tier's strict TPM limits (see InfrastructureDependencyInjectionHandler's
        // OpenAI resilience policy for the RPM side of that same constraint).
        private const int MaxExtractedCharactersArabic = 8_000;

        // Prompt strategy lives here, in the Application layer, not inside
        // OpenAiService — wording/tone is a product decision, calling the
        // API is an Infrastructure concern. Keeps them separable.
        private const string SystemPromptWithContent =
            "You are a reading assistant inside a book app. Below is the opening " +
            "portion of the book \"{0}\" by {1}. Summarize it concisely, preserving " +
            "key points and structure. Reply with only the summary — no preamble, " +
            "no restating the instructions.";

        // Used when the book's Word document has no extractable text (missing file,
        // empty body, unreadable/corrupt .docx, etc.) — falls back to the catalog
        // description instead of failing the request outright.
        private const string SystemPromptMetadataOnly =
            "You are a reading assistant inside a book app. Using the book's title, " +
            "author, and description below, write a concise summary that gives the " +
            "reader a clear sense of what the book is about. Reply with only the " +
            "summary — no preamble, no restating the instructions.";

        private readonly IBookPurchaseRepository _purchaseRepository;
        private readonly IDocxTextExtractionService _textExtractionService;
        private readonly IOpenAiService _openAiService;
        private readonly IAiUsageLimiterService _usageLimiter;

        public SummarizeTextCommandHandler(
            IBookPurchaseRepository purchaseRepository,
            IDocxTextExtractionService textExtractionService,
            IOpenAiService openAiService,
            IAiUsageLimiterService usageLimiter,
            ILogger<SummarizeTextCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _purchaseRepository = purchaseRepository;
            _textExtractionService = textExtractionService;
            _openAiService = openAiService;
            _usageLimiter = usageLimiter;
        }

        public async Task<AppResult<SummarizeTextResponse>> Handle(
            SummarizeTextCommand request,
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

                // Checked BEFORE calling OpenAI — FR-AI-05 exists for cost
                // control, which only works if a rejected request never
                // actually reaches the (paid) API call.
                var usage = await _usageLimiter.TryConsumeAsync(request.UserId, cancellationToken);
                if (!usage.Allowed)
                    throw new ApplicationBusinessException(
                        $"Daily AI usage limit reached ({usage.DailyLimit} requests/day). Try again tomorrow.");

                var (systemPrompt, userContent, isMetadataOnly) = await BuildPromptAsync(context, cancellationToken);

                var summary = await _openAiService.GetCompletionAsync(
                    systemPrompt, userContent, maxTokens: 400, cancellationToken);

                if (summary is null && !isMetadataOnly)
                {
                    // The book-content attempt failed outright — most likely still
                    // rate-limited after the HttpClient's own retry-with-backoff policy
                    // was exhausted (see InfrastructureDependencyInjectionHandler). Fall
                    // back once to the much shorter metadata-only prompt, which is far
                    // less likely to hit the free tier's token/rate limits, rather than
                    // failing the request outright.
                    var (fallbackPrompt, fallbackContent, _) = BuildMetadataOnlyPrompt(context);
                    summary = await _openAiService.GetCompletionAsync(
                        fallbackPrompt, fallbackContent, maxTokens: 400, cancellationToken);
                }

                if (summary is null)
                    throw new ApplicationBusinessException("The AI assistant is temporarily unavailable. Please try again.");

                return new SummarizeTextResponse(summary);
            }, "Book summarized successfully.");
        }

        private async Task<(string SystemPrompt, string UserContent, bool IsMetadataOnly)> BuildPromptAsync(
            PurchaseBookContext context,
            CancellationToken cancellationToken)
        {
            var extractedText = string.IsNullOrWhiteSpace(context.CanonicalWordDocUrl)
                ? null
                : await _textExtractionService.ExtractDocxTextAsync(
                    context.CanonicalWordDocUrl, MaxExtractedCharacters, cancellationToken);

            if (string.IsNullOrWhiteSpace(extractedText))
                return BuildMetadataOnlyPrompt(context);

            if (IsArabicHeavy(extractedText) && extractedText.Length > MaxExtractedCharactersArabic)
            {
                extractedText = extractedText[..MaxExtractedCharactersArabic];
            }

            return (string.Format(SystemPromptWithContent, context.Title, context.Author), extractedText, false);
        }

        private static (string SystemPrompt, string UserContent, bool IsMetadataOnly) BuildMetadataOnlyPrompt(
            PurchaseBookContext context)
        {
            var metadataOnly =
                $"Title: {context.Title}\n" +
                $"Author: {context.Author}\n" +
                $"Description: {context.Description}";

            return (SystemPromptMetadataOnly, metadataOnly, true);
        }

        // Samples the first 500 letters rather than the whole excerpt — enough to
        // classify the script reliably without scanning 20k characters on every call.
        private static bool IsArabicHeavy(string text)
        {
            var sampleLength = Math.Min(text.Length, 500);
            var arabicLetterCount = 0;
            var letterCount = 0;

            for (var i = 0; i < sampleLength; i++)
            {
                var c = text[i];
                if (!char.IsLetter(c))
                    continue;

                letterCount++;
                if (c is >= '؀' and <= 'ۿ')
                    arabicLetterCount++;
            }

            return letterCount > 0 && arabicLetterCount / (double)letterCount > 0.5;
        }
    }
}