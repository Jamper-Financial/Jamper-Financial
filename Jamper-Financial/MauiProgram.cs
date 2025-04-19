using Jamper_Financial.Services;
using Jamper_Financial.Shared.Data;
using Jamper_Financial.Shared.Services;
using Microsoft.Extensions.Logging;

namespace Jamper_Financial
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the Jamper_Financial.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            // Add Blazor WebView for MAUI
            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            // Add developer tools in debug mode
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Register shared services
            builder.Services.AddSingleton<GoalState>();

            return builder.Build();
        }
    }
}
