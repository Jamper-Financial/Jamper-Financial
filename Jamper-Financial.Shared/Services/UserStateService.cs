using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public class UserStateService
    {
        public int UserId { get; private set; }
        public string? Username { get; private set; }
        public string? Email { get; private set; }
        public bool IsLoggedIn => UserId > 0;
        public event Action? OnChange;



        public void SetUser(int userId, string? username, string? email)
        {
            UserId = userId;
            Username = username;
            Email = email;
            NotifyStateChanged();
            Console.WriteLine($"UserStateService: User set - ID: {UserId}, Name: {Username}");
        }


        public void ClearUser()
        {
            UserId = 0;
            Username = null;
            Email = null;
            NotifyStateChanged();
            Console.WriteLine("UserStateService: User cleared.");
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}