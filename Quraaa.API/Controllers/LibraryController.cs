using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Libraries;
using Quraaa.Application.Features.Libraries.Commands.RegisterLibrary;
using Quraaa.Application.Features.Libraries.Common;

namespace Quraaa.API.Controllers
{
    public class LibraryController : ApiClientController
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

        private readonly IWebHostEnvironment _environment;

        public LibraryController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LibraryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromForm] RegisterLibraryRequest request)
        {
            var imageValidationError = ValidateImageFile(request.LibraryImage, nameof(request.LibraryImage))
                ?? ValidateImageFile(request.HeaderImage, nameof(request.HeaderImage));

            if (imageValidationError != null)
            {
                return BadRequest(new
                {
                    type = "ValidationFailure",
                    title = "Validation Error",
                    errors = new[]
                    {
                        new { Field = imageValidationError.Value.Field, Message = imageValidationError.Value.Message }
                    }
                });
            }

            string? libraryImagePath = null;
            string? headerImagePath = null;

            try
            {
                libraryImagePath = await SaveImageAsync(request.LibraryImage!);
                headerImagePath = await SaveImageAsync(request.HeaderImage!);

                var command = new RegisterLibraryCommand(
                    request.LibraryName,
                    request.Location,
                    libraryImagePath,
                    headerImagePath,
                    request.Email,
                    request.UserId
                );

                var result = await Mediator.Send(command);
                if (!result.IsT0)
                {
                    TryDeleteUploadedFile(libraryImagePath);
                    TryDeleteUploadedFile(headerImagePath);
                }

                return HandleResult(result);
            }
            catch
            {
                TryDeleteUploadedFile(libraryImagePath);
                TryDeleteUploadedFile(headerImagePath);
                throw;
            }
        }

        private static (string Field, string Message)? ValidateImageFile(IFormFile? file, string fieldName)
        {
            if (file == null || file.Length == 0)
            {
                return (fieldName, $"{fieldName} is required.");
            }

            if (file.Length > MaxImageSizeInBytes)
            {
                return (fieldName, $"{fieldName} must not exceed 5 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageExtensions.Contains(extension))
            {
                return (fieldName, $"{fieldName} must be a JPG, PNG image.");
            }

            if (!AllowedImageContentTypes.Contains(file.ContentType))
            {
                return (fieldName, $"{fieldName} content type is not supported.");
            }

            return null;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var uploadDirectory = Path.Combine(webRootPath, "uploads", "libraries");
            Directory.CreateDirectory(uploadDirectory);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew);
            await file.CopyToAsync(stream);

            return $"/uploads/libraries/{fileName}";
        }

        private void TryDeleteUploadedFile(string? storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath))
            {
                return;
            }

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var normalizedPath = storedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.GetFullPath(Path.Combine(webRootPath, normalizedPath));
            var webRootFullPath = Path.GetFullPath(webRootPath);

            if (!fullPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
