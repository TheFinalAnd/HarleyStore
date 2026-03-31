using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly SessionService _sessionService;
        private readonly SupabaseService _supabaseService;
        private readonly CryptoService _cryptoService;
        private bool _editando;

        public ProfilePage()
        {
            InitializeComponent();
            _sessionService = ServiceHelper.GetService<SessionService>();
            _supabaseService = ServiceHelper.GetService<SupabaseService>();
            _cryptoService = ServiceHelper.GetService<CryptoService>();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CargarUsuario();
        }

        private void CargarUsuario()
        {
            var u = _sessionService.UsuarioActual;
            if (u == null) return;

            NombreHeaderLabel.Text = u.Nombre;
            NombreEntry.Text = u.Nombre;
            CorreoEntry.Text = u.Correo;
            TelefonoEntry.Text = u.Telefono;
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            if (!_editando)
            {
                _editando = true;
                NombreEntry.IsEnabled = true;
                TelefonoEntry.IsEnabled = true;
                EditarPerfilButton.Text = "Guardar";
                return;
            }

            var u = _sessionService.UsuarioActual;
            if (u == null) return;

            u.Nombre = NombreEntry.Text?.Trim() ?? u.Nombre;
            u.Telefono = TelefonoEntry.Text?.Trim() ?? u.Telefono;

            var ok = await _supabaseService.ActualizarUsuarioAsync(u);
            await DisplayAlertAsync("Perfil", ok ? "Perfil actualizado." : "No se pudo actualizar.", "OK");

            if (ok)
            {
                _editando = false;
                NombreEntry.IsEnabled = false;
                TelefonoEntry.IsEnabled = false;
                EditarPerfilButton.Text = "Editar Perfil";
                CargarUsuario();
            }
        }

        private void OnMostrarCambioContrasenaClicked(object sender, EventArgs e)
        {
            CambioPasswordOverlay.IsVisible = true;
        }

        private void OnCancelarCambioContrasenaClicked(object sender, EventArgs e)
        {
            CambioPasswordOverlay.IsVisible = false;
            LimpiarCamposPassword();
        }

        private void LimpiarCamposPassword()
        {
            ContrasenaActualEntry.Text = string.Empty;
            NuevaContrasenaEntry.Text = string.Empty;
            ConfirmarNuevaContrasenaEntry.Text = string.Empty;
        }

        private async void OnActualizarContrasenaClicked(object sender, EventArgs e)
        {
            try
            {
                var usuario = _sessionService.UsuarioActual;
                if (usuario == null)
                {
                    await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(ContrasenaActualEntry.Text) ||
                    string.IsNullOrWhiteSpace(NuevaContrasenaEntry.Text) ||
                    string.IsNullOrWhiteSpace(ConfirmarNuevaContrasenaEntry.Text))
                {
                    await DisplayAlertAsync("Validación", "Completa todos los campos.", "OK");
                    return;
                }

                var actualHash = _cryptoService.ToSha256(ContrasenaActualEntry.Text.Trim());

                if (actualHash != usuario.Contrasena)
                {
                    await DisplayAlertAsync("Contraseña", "La contraseña actual no es correcta.", "OK");
                    return;
                }

                if (NuevaContrasenaEntry.Text.Trim() != ConfirmarNuevaContrasenaEntry.Text.Trim())
                {
                    await DisplayAlertAsync("Contraseña", "Las nuevas contraseñas no coinciden.", "OK");
                    return;
                }

                var nuevaHash = _cryptoService.ToSha256(NuevaContrasenaEntry.Text.Trim());
                var ok = await _supabaseService.CambiarContrasenaAsync(usuario.IdUsuario, nuevaHash);

                if (!ok)
                {
                    await DisplayAlertAsync("Contraseña", "No se pudo actualizar.", "OK");
                    return;
                }

                usuario.Contrasena = nuevaHash;

                CambioPasswordOverlay.IsVisible = false;
                LimpiarCamposPassword();

                await DisplayAlertAsync("Éxito", "Contraseña actualizada correctamente.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnSoporteClicked(object sender, EventArgs e)
        {
            await Launcher.Default.OpenAsync("mailto:soporte@harleystore.com?subject=Ayuda%20HarleyStore");
        }
    }
}