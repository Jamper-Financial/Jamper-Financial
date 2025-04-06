using System;

namespace Jamper_Financial.Shared.Models
{
    public class Filter
    {
        public string Category { get; set; } = string.Empty;
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? AccountId { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public DateTime? EndDateFrom { get; set; }
        public DateTime? EndDateTo { get; set; }
        public bool? HasReceipt { get; set; }
        public bool? IsPaid { get; set; }
    }
}
