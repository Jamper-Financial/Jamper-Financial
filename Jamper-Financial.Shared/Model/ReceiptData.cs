using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jamper_Financial.Shared.Models
{
    public class ReceiptData
    {
        public int ReceiptID { get; set; }
        public string ReceiptDescription { get; set; } = string.Empty; // Initialize to avoid nullability issues
        public byte[] ReceiptFileData { get; set; } = Array.Empty<byte>(); 
        public int TransactionID { get; set; }
    }
}