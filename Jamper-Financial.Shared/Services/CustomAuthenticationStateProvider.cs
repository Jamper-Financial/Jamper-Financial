using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using Jamper_Financial.Shared.Services;

// In Jamper_Financial.Shared.Services namespace (or client-side project)
public class CustomAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly UserStateService _userStateService;

    public CustomAuthenticationStateProvider(UserStateService userStateService)
    {
        _userStateService = userStateService;
        // Subscribe to the event so we know when to re-evaluate auth state
        _userStateService.OnChange += HandleUserStateChange;
        Console.WriteLine("CustomAuthenticationStateProvider: Initialized and subscribed to UserStateService.OnChange");
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsPrincipal principal;

        if (_userStateService.IsLoggedIn)
        {
            Console.WriteLine($"CustomAuthenticationStateProvider: GetAuthenticationStateAsync - User IS Logged In (ID: {_userStateService.UserId})");
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, _userStateService.UserId.ToString()),
                new Claim(ClaimTypes.Name, _userStateService.Username ?? string.Empty),
                new Claim(ClaimTypes.Email, _userStateService.Email ?? string.Empty),
                // Add other claims like roles if available in UserStateService
            }, "CustomCookieAuth"); // Match the scheme name used in middleware if needed, or use a distinct client-side name
            principal = new ClaimsPrincipal(identity);
        }
        else
        {
            Console.WriteLine("CustomAuthenticationStateProvider: GetAuthenticationStateAsync - User IS NOT Logged In");
            principal = new ClaimsPrincipal(new ClaimsIdentity()); // Anonymous user
        }

        return Task.FromResult(new AuthenticationState(principal));
    }

    private void HandleUserStateChange()
    {
        Console.WriteLine("CustomAuthenticationStateProvider: Notifying Blazor of auth state change due to UserStateService update.");
        // Notify Blazor framework that the authentication state may have changed.
        // This triggers components using [AuthorizeView] etc. to re-render.
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void Dispose()
    {
        // Unsubscribe to prevent memory leaks
        _userStateService.OnChange -= HandleUserStateChange;
        Console.WriteLine("CustomAuthenticationStateProvider: Disposed and unsubscribed from UserStateService.OnChange");
    }
}