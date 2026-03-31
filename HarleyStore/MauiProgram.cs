using Microsoft.Extensions.Logging;
using HarleyStore.Services;

namespace HarleyStore
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<SupabaseService>();
            builder.Services.AddSingleton<CryptoService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            ServiceHelper.Services = app.Services;
            return app;
        }
    }
}