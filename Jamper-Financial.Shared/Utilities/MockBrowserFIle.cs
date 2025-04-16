using Microsoft.AspNetCore.Components.Forms;
using System.IO;

namespace Jamper_Financial.Shared.Utilities
{
    public class MockBrowserFile : IBrowserFile
    {
        private readonly string _filePath;

        public MockBrowserFile(string filePath)
        {
            _filePath = filePath;
            Name = Path.GetFileName(filePath);
            Size = new FileInfo(filePath).Length;
            LastModified = File.GetLastWriteTime(filePath);
            ContentType = GetMimeType(filePath);
        }

        public string Name { get; }
        public long Size { get; }
        public DateTimeOffset LastModified { get; }
        public string ContentType { get; }

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private static string GetMimeType(string filePath)
        {
            // Basic MIME type detection based on file extension
            return Path.GetExtension(filePath).ToLower() switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream", // Default MIME type
            };
        }
    }
}
