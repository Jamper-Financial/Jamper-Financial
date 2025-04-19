namespace Jamper_Financial.Shared.Models
{
    public class Session
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
