using System;
using System.Collections.Generic;

namespace Jamper_Financial.Shared.Models
{
    public class CsvUploadEventArgs
    {
        public byte[] CsvData { get; set; } = Array.Empty<byte>();
        public string? FileName { get; set; }
        public int AccountId { get; set; }
        public Dictionary<string, int> ColumnMappings { get; set; } = new();
        public string Delimiter { get; set; } = ",";
        public string[] AdditionalDateFormats { get; set; } = Array.Empty<string>();
        public bool IsAmountInverted { get; set; } = false; // New property
    }
}
