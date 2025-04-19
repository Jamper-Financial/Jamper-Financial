using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameOrEmailAsync(string usernameOrEmail);
        Task CreateSessionAsync(Session session); // Add this method
    }

}
