using BrainDumpster.Data;
using BrainDumpster.Services;
using Microsoft.EntityFrameworkCore; // Add this if using EF Core
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BrainDumpster
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

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Add this line to register your AppDbContext
            builder.Services.AddSingleton<AppDbContext>();


            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<JournalService>();
            builder.Services.AddScoped<SettingsService>();

            // OR if you're using Entity Framework Core with SQLite:
            /*
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "braindumpster.db3");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));
            */

            return builder.Build();
        }
    }
}