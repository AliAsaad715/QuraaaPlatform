using Quraaa.Domain.Category;

namespace Quraaa.Persistence.Seed
{
    public static class CategorySeedData
    {
        public static IReadOnlyCollection<CategoryAggregate> GetSeedCategories() => new List<CategoryAggregate>
        {
            new(CategoryIds.SpaceScience, "space_science", "علوم الفضاء", "Space Science"),
            new(CategoryIds.Geography, "geography", "جغرافيا الأرض", "Geography"),
            new(CategoryIds.History, "history", "تاريخ", "History"),
            new(CategoryIds.Encyclopedias, "encyclopedias", "موسوعات", "Encyclopedias"),
            new(CategoryIds.Patrols, "patrols", "دوريات", "Patrols"),
            new(CategoryIds.Culture, "culture", "ثقافة", "Culture"),
            new(CategoryIds.Science, "science", "علوم", "Science"),
            new(CategoryIds.Novels, "novels", "روايات", "Novels"),
            new(CategoryIds.Policy, "policy", "سياسة", "Policy"),
            new(CategoryIds.Dictionary, "dictionary", "قواميس", "Dictionary"),
            new(CategoryIds.Education, "education", "تعليم", "Education"),
            new(CategoryIds.Technology, "technology", "تكنولوجيا", "Technology"),
            new(CategoryIds.Art, "art", "فن", "Art"),
            new(CategoryIds.Literature, "literature", "أدب", "Literature"),
            new(CategoryIds.Other, "other", "أخرى", "Other"),
        };
    }
}