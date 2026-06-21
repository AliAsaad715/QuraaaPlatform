using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Domain.Category
{
    public class CategoryAggregate : AggregateRoot
    {
        public string Code { get; private set; } = null!;
        public string NameAr { get; private set; } = null!;
        public string NameEn { get; private set; } = null!;
        public Guid? ParentCategoryId { get; private set; }
        public bool IsActive { get; private set; }

        private CategoryAggregate() { }

        public CategoryAggregate(Guid id, string code, string nameAr, string nameEn, Guid? parentCategoryId = null)
        {
            Id = id;
            Code = code;
            NameAr = nameAr;
            NameEn = nameEn;
            ParentCategoryId = parentCategoryId;
            IsActive = true;
        }

        public void UpdateDetails(string nameAr, string nameEn, Guid modifiedBy)
        {
            NameAr = nameAr;
            NameEn = nameEn;
            UpdateAudit(modifiedBy);
        }

        public void Deactivate(Guid modifiedBy)
        {
            IsActive = false;
            UpdateAudit(modifiedBy);
        }

        public void Activate(Guid modifiedBy)
        {
            IsActive = true;
            UpdateAudit(modifiedBy);
        }
    }
}