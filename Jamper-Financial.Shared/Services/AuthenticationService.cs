using Jamper_Financial.Shared.Data; // Assuming DatabaseHelper is here
using Jamper_Financial.Shared.Models; // Assuming User, Session, UserProfile models are here
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http; // Required for HttpContext parameter
using System;
using System.Threading.Tasks;
using System.Net.Http; // Required for HttpClient
using System.Net.Http.Json; // Required for PostAsJsonAsync, etc.
using Microsoft.AspNetCore.Builder; // Required for MapPost, etc.
using Microsoft.AspNetCore.Routing; // Required for IEndpointRouteBuilder
using Microsoft.Extensions.DependencyInjection; // Required for IServiceProvider

namespace Jamper_Financial.Shared.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        // No longer needs IHttpContextAccessor
        private readonly NavigationManager _navigationManager;
        private readonly UserStateService _userStateService;
        private readonly IUserRepository _userRepository;
        private readonly ISessionRepository _sessionRepository;

        public AuthenticationService(
            NavigationManager navigationManager,
            UserStateService userStateService,
            IUserRepository userRepository,
            ISessionRepository sessionRepository) // Removed IHttpContextAccessor
        {
            _navigationManager = navigationManager;
            _userStateService = userStateService;
            _userRepository = userRepository;
            _sessionRepository = sessionRepository;
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashedInputPassword = HashPassword(password);
            return hashedInputPassword == hashedPassword;
        }

        /// <summary>
        /// Validates standard user credentials.
        /// </summary>
        /// <param name="identifier">Username or Email</param>
        /// <param name="password">Password</param>
        /// <returns>The User object if valid, otherwise null.</returns>
        public async Task<User?> ValidateCredentialsAsync(string identifier, string password)
        {
            Console.WriteLine($"ValidateCredentialsAsync: Identifier={identifier}");
            var user = await _userRepository.GetUserByUsernameOrEmailAsync(identifier);

            if (user != null && VerifyPassword(password, user.Password))
            {
                if (user.IsGoogleSignIn == 1)
                {
                    Console.WriteLine($"User {identifier} signed up via Google. Standard login denied.");
                    return null;
                }
                Console.WriteLine($"Credentials valid for user: {user.Username}");
                return user;
            }
            Console.WriteLine($"Invalid credentials for identifier: {identifier}");
            return null;
        }

        /// <summary>
        /// Gets an existing user by email or creates a new one for Google Sign-In.
        /// </summary>
        /// <param name="email">User's email from Google.</param>
        /// <param name="displayName">User's display name from Google.</param>
        /// <returns>The existing or newly created User object.</returns>
        public async Task<User?> GetOrCreateGoogleUserAsync(string email, string displayName)
        {
            Console.WriteLine($"GetOrCreateGoogleUserAsync: Email={email}");
            var user = await _userRepository.GetUserByUsernameOrEmailAsync(email);

            if (user == null)
            {
                Console.WriteLine($"Creating new Google user for email: {email}");
                var newUser = new User { Username = displayName, Email = email, Password = string.Empty, IsGoogleSignIn = 1 };
                var userProfile = new UserProfile { FirstName = displayName.Split(' ')[0], LastName = displayName.Split(' ').Length > 1 ? displayName.Split(' ')[1] : string.Empty, Email = email, Username = newUser.Username, Password = string.Empty };

                // Ideally, refactor CreateUserAccount to be async Task and use repositories
                CreateUserAccount(newUser, userProfile);

                user = await _userRepository.GetUserByUsernameOrEmailAsync(email);
                if (user == null)
                {
                    Console.Error.WriteLine($"Failed to retrieve newly created Google user: {email}");
                    return null;
                }
                Console.WriteLine($"New Google user created with UserId: {user.UserId}");
            }
            else
            {
                if (user.IsGoogleSignIn != 1)
                {
                    Console.WriteLine($"User {email} exists but is not marked as a Google Sign-In user. Allowing login.");
                    // Optionally update flag: user.IsGoogleSignIn = 1; await _userRepository.UpdateUserAsync(user);
                }
                Console.WriteLine($"Existing Google user found: {user.Username}, UserId: {user.UserId}");
            }
            return user;
        }

        /// <summary>
        /// Creates a session, stores it, and sets the auth cookie in the HTTP response.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <param name="httpContext">The current HttpContext to set the cookie.</param>
        /// <returns>The session token.</returns>
        public async Task<string> CreateSessionAndSetCookieAsync(User user, HttpContext httpContext)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext), "HttpContext must be provided by the caller (API endpoint).");

            var sessionToken = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddHours(1);

            await _sessionRepository.CreateSessionAsync(new Session { UserId = user.UserId, Token = sessionToken, CreatedAt = DateTime.UtcNow, ExpiresAt = expiresAt });
            Console.WriteLine($"Session created for UserId {user.UserId} with token {sessionToken}");

            // TEMPORARY INSECURE TEST - REMOVE ALL FLAGS
            httpContext.Response.Cookies.Append("authToken", sessionToken);

            //httpContext.Response.Cookies.Append("authToken", sessionToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = expiresAt });
            //Console.WriteLine($"Auth cookie set via HttpContext for token {sessionToken}");

            return sessionToken;
        }


        /// <summary>
        /// Logs the user out by deleting the session and the cookie.
        /// </summary>
        public async Task LogoutAsync(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext), "HttpContext must be provided by the caller (API endpoint).");

            var token = httpContext.Request.Cookies["authToken"];
            if (!string.IsNullOrEmpty(token))
            {
                await _sessionRepository.DeleteSessionByTokenAsync(token);
                Console.WriteLine($"Session deleted for token {token}");
                _userStateService.ClearUser(); // Clear client-side state
                httpContext.Response.Cookies.Delete("authToken", new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
                Console.WriteLine("Auth cookie deleted via HttpContext.");
            }
        }


        // --- CreateUserAccount (Keep original but consider making async Task) ---
        // NOTE: This method uses DatabaseHelper directly. It might be better to
        // use _userRepository and potentially a _profileRepository for consistency.
        // Also, it's async void, which should be avoided. Refactor to `async Task`.
        public /*async Task*/ void CreateUserAccount(User user, UserProfile userProfile)
        {
            Console.WriteLine($"Creating account: User={user.Username}, Profile={userProfile.Username}");
            if (user.IsGoogleSignIn != 1 && !string.IsNullOrEmpty(userProfile.Password))
            {
                user.Password = HashPassword(userProfile.Password);
            }
            else
            {
                user.Password = string.Empty;
            }

            DatabaseHelper.InsertUser(user);
            int userId = DatabaseHelper.GetUserIdByUsername(user.Username ?? string.Empty); // Risky if username not unique
            if (userId <= 0) { Console.Error.WriteLine($"Failed to get UserId after inserting user: {user.Username}"); return; }
            Console.WriteLine($"User inserted with UserId: {userId}");
            user.UserId = userId;

            DatabaseHelper.InsertProfile(userId, userProfile.FirstName ?? string.Empty, userProfile.LastName ?? string.Empty);
            int adminRoleId = DatabaseHelper.GetRoleIdByName("Admin");
            if (adminRoleId > 0) { DatabaseHelper.AssignRoleToUser(userId, adminRoleId); } else { Console.Error.WriteLine("Admin role not found."); }
            DatabaseHelper.InsertDefaultCategories(userId);
            Console.WriteLine($"Account creation complete for UserId: {userId}");
        }

        public async Task<string?> LoginAsync(string username, string password)
        {
            Console.WriteLine("LoginAsync: " + username + ", " + password);

            var user = await _userRepository.GetUserByUsernameOrEmailAsync(username);
            if (user != null && VerifyPassword(password, user.Password))
            {
                // Generate a unique session token
                var sessionToken = Guid.NewGuid().ToString();

                // Store the session token in the database
                await _userRepository.CreateSessionAsync(new Session
                {
                    UserId = user.UserId,
                    Token = sessionToken,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(1) // Set expiration time
                });

                // Update the user state
                _userStateService.SetUser(user.UserId, user.Username, user.Email);

                return sessionToken;
            }

            return null;
        }
    }
}
