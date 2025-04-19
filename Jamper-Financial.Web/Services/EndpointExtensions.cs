using Jamper_Financial.Shared.Models;
using Jamper_Financial.Shared.Services;
using Microsoft.AspNetCore.Http;

namespace Jamper_Financial.Web.Services
{
    // Input model for standard login
    public record LoginRequest(string Identifier, string Password);
    // Input model for Google login
    public record GoogleLoginRequest(string Email, string DisplayName); // Or potentially an ID token to verify server-side

    public static class AuthEndpoints
    {
        public static void MapAuthApi(this IEndpointRouteBuilder endpoints)
        {
            var authGroup = endpoints.MapGroup("/api/auth");

            // POST /api/auth/login
            authGroup.MapPost("/login", async (LoginRequest request, HttpContext httpContext, IAuthenticationService authService) =>
            {
                if (string.IsNullOrWhiteSpace(request.Identifier) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Results.BadRequest("Identifier and password are required.");
                }

                var user = await authService.ValidateCredentialsAsync(request.Identifier, request.Password);
                if (user == null)
                {
                    return Results.Unauthorized(); // Use Unauthorized (401) for bad credentials
                }

                try
                {
                    // Create session and set cookie using the HttpContext from the API endpoint
                    await authService.CreateSessionAndSetCookieAsync(user, httpContext);

                    // Return OK (200) or perhaps user info (excluding sensitive data)
                    return Results.Ok(user);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"API Login Error: {ex}");
                    return Results.Problem("An error occurred during login."); // 500 Internal Server Error
                }
            })
            .AllowAnonymous(); // Ensure this endpoint can be called without prior auth

            // POST /api/auth/google-login
            authGroup.MapPost("/google-login", async (GoogleLoginRequest request, HttpContext httpContext, IAuthenticationService authService) =>
            {
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
                {
                    return Results.BadRequest("Email and display name are required for Google login.");
                }
                // Consider validating a Google ID token here instead of trusting client-sent email/name

                var user = await authService.GetOrCreateGoogleUserAsync(request.Email, request.DisplayName);
                if (user == null)
                {
                    // Failed to create or find user
                    return Results.Problem("Could not process Google Sign-In.");
                }

                try
                {
                    // Create session and set cookie
                    await authService.CreateSessionAndSetCookieAsync(user, httpContext);

                    return Results.Ok(user);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"API Google Login Error: {ex}");
                    return Results.Problem("An error occurred during Google login processing.");
                }
            })
             .AllowAnonymous(); // Ensure this endpoint can be called without prior auth

            // POST /api/auth/logout
            // Use POST for logout to prevent CSRF if triggered by simple links
            authGroup.MapPost("/logout", async (HttpContext httpContext, IAuthenticationService authService) =>
            {
                try
                {
                    await authService.LogoutAsync(httpContext);
                    return Results.Ok(new { Message = "Logout successful" });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"API Logout Error: {ex}");
                    return Results.Problem("An error occurred during logout.");
                }
            });
            // Note: Logout might implicitly require authentication if only logged-in users can log out,
            // but often it's designed to clear state regardless. If it needs auth, remove AllowAnonymous
            // if it was added, or rely on default policy. The SessionValidationMiddleware might block it
            // if not excluded and no valid cookie exists. Add "/api/auth/logout" to excluded paths if needed.

            // GET /api/auth/userinfo (Example protected endpoint)
            authGroup.MapGet("/userinfo", (HttpContext httpContext, UserStateService userStateService) =>
            {
                // This demonstrates getting user info *after* the middleware has validated the cookie.
                // You could also get the UserId from context.Items["UserId"] set by the middleware.
                if (userStateService.IsLoggedIn)
                {
                    return Results.Ok(new { userStateService.UserId, userStateService.Username, userStateService.Email });
                }
                // If middleware didn't run or failed, this might return Ok with logged-out state,
                // or middleware might have already redirected. Consider adding .RequireAuthorization() here.
                return Results.Unauthorized();
            }).RequireAuthorization(); // Explicitly require authorization (which SessionValidationMiddleware handles via cookie check)

        }
    }
}