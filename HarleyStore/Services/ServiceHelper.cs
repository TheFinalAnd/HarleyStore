namespace HarleyStore.Services
{
    public static class ServiceHelper
    {

        /// <summary>
        /// Proveedor de servicios global utilizado por code-behind que no soporta
        /// inyección de dependencias por constructor (por ejemplo páginas XAML).
        /// </summary>
        /// <remarks>
        /// Se establece desde <see cref="MauiProgram.CreateMauiApp"/> tras la construcción
        /// del <see cref="MauiApp"/>. Preferir pasar dependencias por constructor
        /// cuando sea posible; usar este helper únicamente en escenarios donde
        /// el contenedor no se puede propagar fácilmente.
        /// </remarks>
        public static IServiceProvider? Services { get; set; }

        // Nota: la propiedad anterior puede ser null durante fases tempranas de arranque.
        // El uso correcto es acceder después de que el MauiApp haya sido construido.

        /// <summary>
        /// Resuelve un servicio registrado o lanza una excepción clara si no existe.
        /// </summary>
        /// <typeparam name="T">Tipo del servicio a resolver.</typeparam>
        /// <returns>Instancia del servicio.</returns>
        /// <exception cref="InvalidOperationException">Si el servicio no está registrado.</exception>
        public static T GetService<T>() where T : class
        {
            // Forzamos el operador null (!) porque queremos que falle tempranamente
            // si Services no ha sido inicializado; esto facilita detectar errores
            // de configuración en tiempo de ejecución.
            var provider = Services!;

            // Intentamos resolver el servicio por su tipo.
            var service = provider.GetService(typeof(T)) as T;

            // Si el servicio no está registrado devolvemos una excepción con mensaje
            // claro para ayudar a depurar problemas de registro/DI.
            return service ?? throw new InvalidOperationException($"Servicio no registrado: {typeof(T).Name}");
        }
    }
}