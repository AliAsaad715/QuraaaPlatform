using FluentValidation;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public class RegisterLibraryCommandValidator : AbstractValidator<RegisterLibraryCommand>
    {
        private const long MaxImageSizeInBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
        };

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
        };

        public RegisterLibraryCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Registration token is required.")
                .MaximumLength(128).WithMessage("Registration token is invalid.");

            RuleFor(x => x.LibraryName)
                .NotEmpty().WithMessage("Library name is required.")
                .MaximumLength(100).WithMessage("Library name must not exceed 100 characters.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.")
                .MaximumLength(250).WithMessage("Location must not exceed 250 characters.");

            RuleFor(x => x.LibraryImage)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("LibraryImage is required.")
                .Must(file => file!.Length > 0).WithMessage("LibraryImage is required.")
                .Must(file => file!.Length <= MaxImageSizeInBytes).WithMessage("LibraryImage must not exceed 5 MB.")
                .Must(HasAllowedImageExtension).WithMessage("LibraryImage must be a JPG, PNG image.")
                .Must(HasAllowedImageContentType).WithMessage("LibraryImage content type is not supported.")
                .Must(ImageFileSignature.MatchesDeclaredExtension)
                    .WithMessage("LibraryImage content does not match its file extension.");

            RuleFor(x => x.HeaderImage)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("HeaderImage is required.")
                .Must(file => file!.Length > 0).WithMessage("HeaderImage is required.")
                .Must(file => file!.Length <= MaxImageSizeInBytes).WithMessage("HeaderImage must not exceed 5 MB.")
                .Must(HasAllowedImageExtension).WithMessage("HeaderImage must be a JPG, PNG image.")
                .Must(HasAllowedImageContentType).WithMessage("HeaderImage content type is not supported.")
                .Must(ImageFileSignature.MatchesDeclaredExtension)
                    .WithMessage("HeaderImage content does not match its file extension.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

        }

        private static bool HasAllowedImageExtension(IUploadedFile? file)
        {
            if (file == null)
            {
                return false;
            }

            var extension = Path.GetExtension(file.FileName);
            return !string.IsNullOrWhiteSpace(extension) && AllowedImageExtensions.Contains(extension);
        }

        private static bool HasAllowedImageContentType(IUploadedFile? file)
        {
            return file != null && AllowedImageContentTypes.Contains(file.ContentType);
        }
    }
}
