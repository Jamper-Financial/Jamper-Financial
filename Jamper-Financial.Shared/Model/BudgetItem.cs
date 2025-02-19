namespace Jamper_Financial.Shared.Models
{
    public class BudgetItem
    {
        public string Category { get; set; }
        public decimal PlannedAmount { get; set; }
        public decimal CurrentAmount { get; set; }
    }

    public class BudgetInsight
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
