namespace Jamper_Financial.Shared.Models 
{
    public class Category
    {
        public int CategoryID { get; set; }
        public int UserID { get; set; } 
        public string Name { get; set; } = string.Empty;
    }

}