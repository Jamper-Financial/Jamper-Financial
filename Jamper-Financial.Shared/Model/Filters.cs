using System;

namespace Jamper_Financial.Shared.Models
{
    public class Filter
    {
        public string Category { get; set; } = string.Empty;
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? AccountId { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public bool? HasEndDate { get; set; }
        public bool? HasReceipt { get; set; }
        public bool? IsPaid { get; set; }
    }
}
