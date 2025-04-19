using Jamper_Financial.Shared.Models;
using Microsoft.AspNetCore.Http; // Required for IHttpContextAccessor

namespace Jamper_Financial.Shared.Services
{
    public interface IAuthenticationService
    {
        Task<User?> ValidateCredentialsAsync(string identifier, string password);
        Task<User?> GetOrCreateGoogleUserAsync(string email, string displayName);
        // HttpContext is now passed in by the caller (API endpoint)
        Task<string> CreateSessionAndSetCookieAsync(User user, HttpContext httpContext);
        // HttpContext is now passed in by the caller (API endpoint)
        Task LogoutAsync(HttpContext httpContext);
        void CreateUserAccount(User user, UserProfile userProfile);
        // Task CreateUserAccountAsync(User user, UserProfile userProfile); // Consider async
    }
}
