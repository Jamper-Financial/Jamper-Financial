using Jamper_Financial.Shared.Models;

namespace Jamper_Financial.Shared.Services
{
    public interface ISessionRepository
    {
        Task<Session?> GetSessionByTokenAsync(string token);
        Task CreateSessionAsync(Session session);
        Task DeleteSessionByTokenAsync(string token); // Add this method
        Task<bool> ValidateSessionTokenAsync(string token); // Add this method

    }

}
