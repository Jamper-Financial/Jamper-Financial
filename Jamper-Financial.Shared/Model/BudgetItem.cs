namespace Jamper_Financial.Shared.Models
{
    public class BudgetItem
    {
        public string Category { get; set; }
        public decimal PlannedAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public string TransactionType { get; set; } // Add this property
    }

    public class BudgetInsight
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
