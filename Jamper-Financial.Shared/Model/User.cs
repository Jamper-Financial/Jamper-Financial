namespace Jamper_Financial.Shared.Models
{
    public class User
    {
        public int UserId { get; set; }
        public int Id => UserId; // Add this as an alias for compatibility
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int IsGoogleSignIn { get; set; } = 0;
    }
}
