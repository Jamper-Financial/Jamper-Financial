namespace Jamper_Financial.Shared.Services
{
    public class UserStateService
    {
        public int UserId { get; private set; }
        public string Username { get; private set; }

        public void SetUser(int userId, string username)
        {
            UserId = userId;
            Username = username;
        }

        public void ClearUser()
        {
            UserId = 0;
            Username = string.Empty;
        }
    }
}
