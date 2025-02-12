using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public class UserStateService
    {
        public int UserId { get; private set; }
        public string Username { get; private set; }
        public LoggedInUser LoggedInUser { get; private set; } = new LoggedInUser();


        public void SetUser(int userId, string username, string email)
        {
            UserId = userId;
            Username = username;
            LoggedInUser.UserName = username;
            LoggedInUser.Email = email;
        }

        public void ClearUser()
        {
            UserId = 0;
            Username = string.Empty;
            LoggedInUser.UserName = string.Empty;
            LoggedInUser.Email = string.Empty;
        }

    }
}