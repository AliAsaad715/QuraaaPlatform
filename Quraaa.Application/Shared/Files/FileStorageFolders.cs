namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// The sub-folders <see cref="IFileStorageService.SaveAsync"/> accepts. Each
    /// one also selects the document types the store will take:
    /// <see cref="LibraryBooks"/> and <see cref="BookPdfs"/> accept PDF, and
    /// <see cref="BookWordDocuments"/> accepts .doc and .docx.
    /// </summary>
    public static class FileStorageFolders
    {
        /// <summary>
        /// A library's digital listing asset. Buyers receive it only through the
        /// authorized, ownership-checked purchase-stream endpoint.
        /// </summary>
        public const string LibraryBooks = "books";

        /// <summary>A catalog book's canonical PDF, from bulk upload.</summary>
        public const string BookPdfs = "books/pdf";

        /// <summary>A catalog book's canonical Word document, from bulk upload.</summary>
        public const string BookWordDocuments = "books/docs";
    }
}
