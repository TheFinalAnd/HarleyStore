using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly SupabaseService _supabaseService;
        private readonly CryptoService _cryptoService;
        private readonly SessionService _sessionService;

        public LoginPage(SupabaseService supabaseService, CryptoService cryptoService, SessionService sessionService)
        {
            InitializeComponent();
            _supabaseService = supabaseService;
            _cryptoService = cryptoService;
            _sessionService = sessionService;
        }

        private void OnLoginCompleted(object sender, EventArgs e)
        {
            OnLoginClicked(sender, e);
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
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CorreoEntry.Text) || string.IsNullOrWhiteSpace(ContrasenaEntry.Text))
                {
                    await DisplayAlertAsync("Validación", "Debes ingresar correo y contraseña.", "OK");
                    return;
                }

                var hash = _cryptoService.ToSha256(ContrasenaEntry.Text.Trim());
                var usuario = await _supabaseService.LoginAsync(CorreoEntry.Text.Trim(), hash);

                if (usuario == null)
                {
                    await DisplayAlertAsync("Login", "Correo o contraseña incorrectos.", "OK");
                    return;
                }

                _sessionService.SetUsuario(usuario);
                await Shell.Current.GoToAsync("//home");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnRegisterTapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync("//register");
        }
    }
}