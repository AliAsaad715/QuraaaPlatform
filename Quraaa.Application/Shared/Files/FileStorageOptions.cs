namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Binds to the "Storage" configuration section.
    /// </summary>
    public sealed class FileStorageOptions
    {
        /// <summary>
        /// Root folder for privately-stored files (digital book assets, etc).
        /// A relative value is resolved against the host's content root, NOT wwwroot,
        /// so files placed here are never reachable through static file middleware.
        /// </summary>
        public string RootPath { get; set; } = "storage";
    }
}
