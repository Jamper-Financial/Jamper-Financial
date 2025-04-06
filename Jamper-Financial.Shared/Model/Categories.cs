namespace Jamper_Financial.Shared.Models
{
    public class Category
    {
        public int CategoryID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#000000";
        public string TransactionType { get; set; } = "e"; // "e" for expense, "i" for income
        public int? ParentCategoryID { get; set; }
    }
}