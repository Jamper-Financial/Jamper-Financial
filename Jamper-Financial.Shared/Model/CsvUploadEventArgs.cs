using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Models
{
    public class CsvUploadEventArgs
    {
        public byte[] CsvData { get; set; } = Array.Empty<byte>();
        public string? FileName { get; set; }
        public int AccountId { get; set; }
        public string BankName { get; set; } = "RBC"; // Default to RBC
    }
}
