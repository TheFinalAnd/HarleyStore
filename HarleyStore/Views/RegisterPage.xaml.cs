using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class RegisterPage : ContentPage
    {
        private readonly SupabaseService _supabaseService;
        private readonly CryptoService _cryptoService;

        public RegisterPage()
        {
            InitializeComponent();
            _supabaseService = new SupabaseService();
            _cryptoService = new CryptoService();
        }

        private void OnRegisterCompleted(object sender, EventArgs e)
        {
            OnRegisterClicked(sender, e);
        }
        private bool _passwordVisible = false;
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                ContrasenaEntry.IsPassword = false;
                TogglePasswordButton.Source = "eye.png";
                ContrasenaEntry.Placeholder = "contraseña";
            }
            else
            {
                ContrasenaEntry.IsPassword = true;
                TogglePasswordButton.Source = "eye_off.png";
                ContrasenaEntry.Placeholder = "******";
            }

            ContrasenaEntry.Focus();
            ContrasenaEntry.CursorPosition = ContrasenaEntry.Text?.Length ?? 0;
        }

        private void OnToggleConfirmPasswordClicked(object sender, EventArgs e)
        {
            _passwordVisible = !_passwordVisible;

            if (_passwordVisible)
            {
                ConfirmarEntry.IsPassword = false;
                ToggleConfirmPasswordButton.Source = "eye.png";
                ConfirmarEntry.Placeholder = "contraseña";
            }
            else
            {
                ConfirmarEntry.IsPassword = true;
                ToggleConfirmPasswordButton.Source = "eye_off.png";
                ConfirmarEntry.Placeholder = "******";
            }

            ConfirmarEntry.Focus();
            ConfirmarEntry.CursorPosition = ConfirmarEntry.Text?.Length ?? 0;
        }
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NombreEntry.Text) ||
                    string.IsNullOrWhiteSpace(CorreoEntry.Text) ||
                    string.IsNullOrWhiteSpace(TelefonoEntry.Text) ||
                    string.IsNullOrWhiteSpace(ContrasenaEntry.Text) ||
                    string.IsNullOrWhiteSpace(ConfirmarEntry.Text))
                {
                    await DisplayAlertAsync("Validación", "Todos los campos son obligatorios.", "OK");
                    return;
                }

                if (ContrasenaEntry.Text.Trim() != ConfirmarEntry.Text.Trim())
                {
                    await DisplayAlertAsync("Validación", "Las contraseñas no coinciden.", "OK");
                    return;
                }

                if (await _supabaseService.ExisteCorreoAsync(CorreoEntry.Text.Trim()))
                {
                    await DisplayAlertAsync("Registro", "Ese correo ya existe.", "OK");
                    return;
                }

                var usuario = new Usuario
                {
                    Nombre = NombreEntry.Text.Trim(),
                    Correo = CorreoEntry.Text.Trim(),
                    Telefono = TelefonoEntry.Text.Trim(),
                    Contrasena = _cryptoService.ToSha256(ContrasenaEntry.Text.Trim()),
                    EsAdmin = false,
                    FechaRegistro = DateTime.Today
                };

                var ok = await _supabaseService.CrearUsuarioAsync(usuario);

                if (!ok)
                {
                    await DisplayAlertAsync("Registro", "No se pudo registrar el usuario.", "OK");
                    return;
                }

                await DisplayAlertAsync("Éxito", "Usuario registrado correctamente.", "OK");
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}