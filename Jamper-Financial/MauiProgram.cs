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

            // Initialize the database // as this is the mobile. Initial db should be done in the web project
            //DatabaseHelper.InitializeDatabase();

            // Add device-specific services used by the Jamper_Financial.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
