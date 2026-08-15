using System.Text;
using System.Text.RegularExpressions;
using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Domain.Catalog
{
    /// <summary>
    /// Produces a stable, lowercase, Unicode-normalized key from a book text field.
    /// Used for case-insensitive duplicate detection across Arabic and English metadata.
    ///
    /// Normalization steps applied in order:
    ///   1. Trim leading/trailing whitespace.
    ///   2. Unicode NFKC — decomposes then recomposes (handles accents, ligatures).
    ///   3. Remove Arabic tatweel (U+0640).
    ///   4. Remove Arabic diacritics / tashkeel (U+064B–U+065F, U+0670).
    ///   5. Collapse alef variants (أ إ آ ٱ) → plain alef (ا).
    ///   6. Collapse ya variant (ى) → ya (ي).
    ///   7. Collapse internal whitespace runs → single space.
    ///   8. Lower-case (invariant culture).
    /// </summary>
    public static partial class BookTextNormalizer
    {
        // Arabic tatweel (elongation) — U+0640
        [GeneratedRegex(@"ـ+")]
        private static partial Regex TatweelRegex();

        // Arabic diacritics: tashkeel, shadda, sukun, superscript alef — U+064B–U+065F, U+0670
        [GeneratedRegex(@"[ً-ٰٟ]+")]
        private static partial Regex DiacriticsRegex();

        /// <summary>
        /// Normalizes a single text field (title, author, or language) for comparison.
        /// Returns an empty string for null or whitespace-only input.
        /// </summary>
        public static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var text = input.Trim().Normalize(NormalizationForm.FormKC);

            text = TatweelRegex().Replace(text, string.Empty);
            text = DiacriticsRegex().Replace(text, string.Empty);

            // Alef variants → plain alef (ا U+0627)
            text = text
                .Replace('أ', 'ا')   // أ  hamza above
                .Replace('إ', 'ا')   // إ  hamza below
                .Replace('آ', 'ا')   // آ  madda above
                .Replace('ٱ', 'ا');  // ٱ  wasla

            // Ya variant: ى (alef maqsura U+0649) → ي (ya U+064A)
            text = text.Replace('ى', 'ي');

            // Collapse internal whitespace and lower-case
            return string.Join(' ',
                text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
        }

        /// <summary>
        /// Returns a pipe-delimited composite key over all three deduplication fields.
        /// Suitable as a <see cref="HashSet{T}"/> key with <see cref="StringComparer.OrdinalIgnoreCase"/>.
        /// Language is a fixed enum (no free-text variance), so it is embedded as-is.
        /// </summary>
        public static string CompositeKey(string title, string? author, Language language)
            => $"{Normalize(title)}|{Normalize(author)}|{language}";
    }
}
