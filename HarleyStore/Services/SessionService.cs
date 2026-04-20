using HarleyStore.Models;

namespace HarleyStore.Services
{
    public class SessionService
    {
        /// <summary>
        /// Usuario actualmente autenticado en la sesión de la aplicación.
        /// </summary>
        /// <remarks>
        /// Servicio con alcance singleton que mantiene en memoria el usuario
        /// autenticado. No persiste datos en almacenamiento local; la persistencia
        /// habría que añadirla (SecureStorage) si se desea sesión persistente.
        /// </remarks>
        public Usuario? UsuarioActual { get; private set; }

        /// <summary>
        /// Indica si hay un usuario logueado.
        /// </summary>
        public bool EstaLogueado => UsuarioActual != null;

        private const string SessionKey = "harleystore_session_usuario";

        /// <summary>
        /// Inicializa el servicio leyendo la sesión almacenada en SecureStorage, si existe.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var json = await Microsoft.Maui.Storage.SecureStorage.Default.GetAsync(SessionKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    UsuarioActual = Newtonsoft.Json.JsonConvert.DeserializeObject<Usuario>(json);
                }
            }
            catch
            {
                // Ignorar errores (ej. SecureStorage no disponible en algunos entornos).
            }
        }

        /// <summary>
        /// Establece el usuario actual en sesión y persiste la información en SecureStorage.
        /// </summary>
        /// <param name="usuario">Instancia de <see cref="Usuario"/> autenticada.</param>
        public async Task SetUsuarioAsync(Usuario usuario)
        {
            UsuarioActual = usuario;
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(usuario);
                await Microsoft.Maui.Storage.SecureStorage.Default.SetAsync(SessionKey, json);
            }
            catch
            {
                // Ignorar fallos de persistencia segura.
            }
        }

        /// <summary>
        /// Cierra la sesión local (no revoca tokens remotos) y elimina la sesión persistida.
        /// </summary>
        public async Task LogoutAsync()
        {
            UsuarioActual = null;
            try
            {
                Microsoft.Maui.Storage.SecureStorage.Default.Remove(SessionKey);
            }
            catch
            {
                // Ignorar.
            }
        }
    }
}