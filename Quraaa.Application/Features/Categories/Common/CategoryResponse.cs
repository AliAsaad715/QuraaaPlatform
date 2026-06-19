namespace Quraaa.Application.Features.Categories.Common
{
    public record CategoryResponse(
        Guid Id,
        string Code,
        string NameAr,
        string NameEn,
        Guid? ParentCategoryId,
        bool IsActive
    );
}