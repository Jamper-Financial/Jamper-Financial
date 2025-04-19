using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Jamper_Financial.Shared.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("api/receipts")]
    public class ReceiptsController : ControllerBase
    {
        private readonly string _tempDirectory;

        // Configure the temporary directory path (same as in your Blazor page)
        public ReceiptsController()
        {
            _tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "temp", "JamperFinancial_BulkUploads");
        }

        [HttpGet("{fileName}")]
        public IActionResult GetReceipt(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name cannot be empty.");
            }

            var filePath = Path.Combine(_tempDirectory, fileName);

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var fileBytes = System.IO.File.ReadAllBytes(filePath);
                    var contentType = GetContentType(fileName);
                    return File(fileBytes, contentType);
                }
                catch (IOException)
                {
                    return StatusCode(500, "Error reading the file.");
                }
            }
            else
            {
                return NotFound("Receipt image not found.");
            }
        }

        private string GetContentType(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();
            switch (extension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                // Add other supported image types as needed
                default:
                    return "application/octet-stream";
            }
        }
    }
}
