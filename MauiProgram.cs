using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Mero_Dainiki.Data;
using Mero_Dainiki.Services;

namespace Mero_Dainiki
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

            builder.Services.AddMauiBlazorWebView();

            // Register Database Context
            builder.Services.AddDbContext<AppDbContext>();

            // Register Services
            builder.Services.AddSingleton<IThemeService, ThemeService>();
            builder.Services.AddScoped<IJournalService, JournalService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ISecurityService, SecurityService>();
            builder.Services.AddScoped<IExportService, ExportService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure database is created at startup
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            return app;
        }
    }
}
