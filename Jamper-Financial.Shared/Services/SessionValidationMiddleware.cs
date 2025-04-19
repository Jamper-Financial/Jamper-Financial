using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Jamper_Financial.Shared.Services; // Adjust namespace if needed
using System;
using System.Threading.Tasks;
using System.Security.Claims;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PathString[] _excludedPaths = new PathString[] {
        "/css",
        "/js",
        "/images",
        "/lib",
        "/_framework",
        "/_content",
        "/credentials",
        "/login",
        "/",
        "/api/auth/login",
        "/api/auth/google-login",
        "/api/auth/logout",
        "/Jamper-Financial.Web.styles.css",
        "/Jamper-Financial.Web.bundle.scp.css",
        "/temp",
        "/_blazor"
    };



    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
    {
        var path = context.Request.Path;
        Console.WriteLine($"--> SessionMiddleware: Path requested: {path}");

        bool isExcluded = false;
        foreach (var excludedPath in _excludedPaths)
        {
            // Use StartsWithSegments for directories/prefixes, use Equals for specific files if needed
            if (path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase) ||
                path.Equals(excludedPath, StringComparison.OrdinalIgnoreCase)) // Check for exact file matches too
            {
                Console.WriteLine($"--> SessionMiddleware: Path EXCLUDED: {path}");
                isExcluded = true;
                break;
            }
        }

        if (isExcluded)
        {
            await _next(context);
            return;
        }

        Console.WriteLine($"--> SessionMiddleware: Path NOT excluded, proceeding with validation: {path}");
        var token = context.Request.Cookies["authToken"];

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("--> SessionMiddleware: Redirecting! Reason: Auth token cookie not found.");
            // Don't set context.User, just proceed or redirect
            // If you want unauthenticated users to proceed to Blazor, call await _next(context); here
            // If all non-excluded paths MUST be authenticated, redirect:
            RedirectToLogin(context);
            return;
        }

        try
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
                Console.WriteLine($"--> SessionMiddleware: Checking token '{token}' for path '{path}'");
                var session = await sessionRepository.GetSessionByTokenAsync(token);

                if (session == null || session.ExpiresAt < DateTime.UtcNow)
                {
                    Console.WriteLine($"--> SessionMiddleware: Redirecting! Reason: Invalid/Expired Token. Token='{token}', SessionFound={session != null}, ExpiresAt='{session?.ExpiresAt}', UtcNow='{DateTime.UtcNow}', IsExpired={session?.ExpiresAt < DateTime.UtcNow}");

                    // Clear potentially invalid cookie from browser
                    context.Response.Cookies.Delete("authToken", new CookieOptions { Path = "/", HttpOnly = true, Secure = false, SameSite = SameSiteMode.Lax }); // Match setting options minus Secure/Expires

                    // Delete session from DB if it exists but is expired
                    if (session != null) { await sessionRepository.DeleteSessionByTokenAsync(token); }

                    // Don't set context.User
                    RedirectToLogin(context);
                    return;
                }

                // *** SESSION IS VALID - CREATE USER IDENTITY ***
                Console.WriteLine($"--> SessionMiddleware: Token valid for UserId {session.UserId}. Creating ClaimsPrincipal.");

                // Create claims for the user. You need at least one claim.
                // Using NameIdentifier is standard for User ID. Add others as needed (like name, roles).
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                    // Optional: Add name claim if you retrieve it (might need IUserRepository)
                    // new Claim(ClaimTypes.Name, userNameFromDb), 
                    // Optional: Add role claims if you have roles
                    // new Claim(ClaimTypes.Role, "Admin"),
                    // new Claim(ClaimTypes.Role, "User"),
                };

                // Create the identity and principal
                // Use a custom authentication type name (e.g., "CustomCookieAuth")
                var identity = new ClaimsIdentity(claims, "CustomCookieAuth");
                var principal = new ClaimsPrincipal(identity);

                // Set HttpContext.User
                context.User = principal;
                Console.WriteLine($"--> SessionMiddleware: HttpContext.User set for UserId {session.UserId}. IsAuthenticated: {context.User.Identity?.IsAuthenticated}");

                // Store UserId in Items as well (optional, might be redundant now)
                context.Items["UserId"] = session.UserId;
            }

            // Call the next middleware in the pipeline (including Blazor endpoint execution)
            await _next(context);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in SessionValidationMiddleware: {ex}");
            // Avoid redirecting on internal errors, let higher-level handlers deal with it
            // Consider clearing cookie if error might be related to it
            // context.Response.Cookies.Delete("authToken", new CookieOptions { Path = "/", ... }); 
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal Server Error during session validation.");
        }
    }

    private void RedirectToLogin(HttpContext context)
    {
        // Prevent redirect loops if already on the login page somehow
        if (!context.Request.Path.Equals("/login", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/login");
        }
        else
        {
            Console.WriteLine("--> SessionMiddleware: Validation failed but already on /login. No redirect.");
        }
    }
}