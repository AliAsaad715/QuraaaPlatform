using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Domain.Catalog
{
    /// <summary>
    /// Maps free-text language values (ISO codes or full names, as returned by the
    /// Google Books API or supplied via the Accept-Language header) to the
    /// strongly-typed <see cref="Language"/> enum.
    /// </summary>
    public static class LanguageCodeMapper
    {
        public static Language Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Language.Other;

            return value.Trim().ToLowerInvariant() switch
            {
                "arabic" or "ar" or "ara" => Language.Arabic,
                "english" or "en" or "eng" => Language.English,
                "french" or "fr" or "fra" or "fre" => Language.French,
                _ => Language.Other,
            };
        }
    }
}
