using HarleyStore.Models;

namespace HarleyStore.Services
{
    public class SessionService
    {
        public Usuario? UsuarioActual { get; private set; }

        public bool EstaLogueado => UsuarioActual != null;

        public void SetUsuario(Usuario usuario)
        {
            UsuarioActual = usuario;
        }

        public void Logout()
        {
            UsuarioActual = null;
        }
    }
}