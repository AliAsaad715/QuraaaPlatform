using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.User.ValueObjects
{
    public class Interest : ValueObjectRoot
    {
        public static readonly Interest SpaceScience = new("space_science", "علوم الفضاء", "Space Science");
        public static readonly Interest Geography = new("geography", "جغرافيا الأرض", "Geography");
        public static readonly Interest History = new("history", "تاريخ", "History");
        public static readonly Interest Encyclopedias = new("encyclopedias", "موسوعات", "Encyclopedias");
        public static readonly Interest Patrols = new("patrols", "دوريات", "Patrols");
        public static readonly Interest Culture = new("culture", "ثقافة", "Culture");
        public static readonly Interest Science = new("science", "علوم", "Science");
        public static readonly Interest Novels = new("novels", "روايات", "Novels");
        public static readonly Interest Policy = new("policy", "سياسة", "Policy");
        public static readonly Interest Dictionary = new("dictionary", "قواميس", "Dictionary");
        public static readonly Interest Education = new("education", "تعليم", "Education");
        public static readonly Interest Technology = new("technology", "تكنولوجيا", "Technology");
        public static readonly Interest Art = new("art", "فن", "Art");
        public static readonly Interest Literature = new("literature", "أدب", "Literature");
        public static readonly Interest Other = new("other", "أخرى", "Other");

        private static readonly Dictionary<string, Interest> AllInterests = new()
        {
            { SpaceScience.Code, SpaceScience },
            { Geography.Code, Geography },
            { History.Code, History },
            { Encyclopedias.Code, Encyclopedias },
            { Patrols.Code, Patrols },
            { Culture.Code, Culture },
            { Science.Code, Science },
            { Novels.Code, Novels },
            { Policy.Code, Policy },
            { Dictionary.Code, Dictionary },
            { Education.Code, Education },
            { Technology.Code, Technology },
            { Art.Code, Art },
            { Literature.Code, Literature },
            { Other.Code, Other }
        };

        public string Code { get; init; }
        public string NameAr { get; init; }
        public string NameEn { get; init; }

        private Interest(string code, string nameAr, string nameEn)
        {
            Code = code;
            NameAr = nameAr;
            NameEn = nameEn;
        }
        public static Interest? FromCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var normalized = code.Trim().ToLower();
            return AllInterests.TryGetValue(normalized, out var interest) ? interest : null;
        }

        public static IReadOnlyCollection<Interest> List() => AllInterests.Values;

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Code;
        }
    }
}
