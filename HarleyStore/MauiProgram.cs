using Microsoft.Extensions.Logging;
using HarleyStore.Services;

namespace HarleyStore
{
    public static class MauiProgram
    {
        /// <summary>
        /// Construye y configura la instancia <see cref="MauiApp"/>.
        /// </summary>
        /// <remarks>
        /// Aquí se registran servicios de aplicación (DI) con tiempo de vida singleton
        /// para que puedan ser resueltos desde cualquier parte de la app. Mantener
        /// las dependencias en singleton es apropiado para servicios sin estado
        /// por usuario o que gestionan estado compartido centralizado (como la
        /// sesión o cliente HTTP).
        /// </remarks>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            // Configuración básica de la app: XAML, fuentes y types de DI.
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Registro de servicios core. Se usan singletons intencionadamente:
            // - SessionService: mantiene el usuario actual en memoria.
            // - SupabaseService: cliente Http para la API remota.
            // - CryptoService: utilitario puro para hashing.
            builder.Services.AddSingleton<SessionService>();
            builder.Services.AddSingleton<SupabaseService>();
            builder.Services.AddSingleton<CryptoService>();
            // Servicios añadidos para manejo de compras y notificaciones.
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<IPurchaseService, PurchaseService>();

#if DEBUG
            // Añadir logging en modo debug para facilitar diagnósticos durante desarrollo.
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            // Guardamos el proveedor de servicios para resoluciones manuales desde
            // componentes que no aceptan DI por constructor (p. ej. XAML code-behind).
            ServiceHelper.Services = app.Services;

            // Inicializar servicios que requieran trabajo asíncrono al arrancar.
            var session = ServiceHelper.GetService<SessionService>();
            // No await en CreateMauiApp; iniciar tarea en background y no bloquear UI.
            _ = session.InitializeAsync();
            return app;
        }
    }
}