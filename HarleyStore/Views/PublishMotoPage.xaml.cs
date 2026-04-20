using HarleyStore.Models;
using HarleyStore.Services;
using System.Net.Http;
using System.Text;
using System.Text.Json;
namespace HarleyStore.Views
{
    public partial class PublishMotoPage : ContentPage
    {
        private FileResult? _fotoSeleccionada;

        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;

        private List<Marca> _marcas = new();
        private List<Modelo> _modelos = new();
        private List<Modelo> _modelosFiltrados = new();

        private bool _isPublishing = false;

        public PublishMotoPage(SupabaseService supabaseService, SessionService sessionService)
        {
            InitializeComponent();
            _supabaseService = supabaseService;
            _sessionService = sessionService;
        }

        private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
        {
            try
            {
                _fotoSeleccionada = await MediaPicker.Default.PickPhotoAsync();

                if (_fotoSeleccionada != null)
                {
                    FotoPreview.Source = ImageSource.FromFile(_fotoSeleccionada.FullPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarDatosAsync();
        }
        private async Task EnviarEmailConfirmacion(Moto moto)
        {
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "re_XAPo228r_Q1zrEjydqxM6SuSNkNA3Xarn");

                // Aquí construimos el cuerpo HTML incluyendo la imagen
                // Asumimos que moto.FotoUrl contiene la URL pública de la imagen en Supabase
                string htmlBody = $@"
            <h1>¡Hola!</h1>
            <p>Tu moto ha sido publicada correctamente en HarleyStore.</p>
            <p><strong>Detalles:</strong> {moto.Descripcion}</p>
            <br/>
            <p><strong>Foto de tu moto:</strong></p>
            <img src='{moto.FotoUrl}' alt='Foto de la moto' style='max-width: 600px; border-radius: 8px;' />";

                var emailData = new
                {
                    from = "andres@arcetest.online",
                    to = new[] { _sessionService.UsuarioActual.Correo },
                    subject = "¡Moto publicada con éxito!",
                    html = htmlBody
                };

                var json = JsonSerializer.Serialize(emailData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                await client.PostAsync("https://api.resend.com/emails", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar email: {ex.Message}");
            }
        }
        private async Task CargarDatosAsync()
        {
            try
            {
                _marcas = await _supabaseService.GetMarcasAsync();
                _modelos = await _supabaseService.GetModelosAsync();

                MarcaPicker.ItemsSource = _marcas;
                ModeloPicker.ItemsSource = null;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudieron cargar marcas/modelos: {ex.Message}", "OK");
            }
        }

        private void OnMarcaChanged(object sender, EventArgs e)
        {
            if (MarcaPicker.SelectedIndex < 0)
                return;

            var marcaSeleccionada = (Marca)MarcaPicker.SelectedItem;

            _modelosFiltrados = _modelos
                .Where(m => m.IdMarca == marcaSeleccionada.IdMarca)
                .ToList();

            ModeloPicker.ItemsSource = _modelosFiltrados;
            ModeloPicker.SelectedIndex = -1;
        }

        private async void OnPublicarClicked(object sender, EventArgs e)
        {
            try
            {
                if (_isPublishing)
                    return;

                _isPublishing = true;
                PublicarButton.IsEnabled = false;

                if (_sessionService.UsuarioActual == null)
                {
                    await DisplayAlert("Sesión", "Debes iniciar sesión.", "OK");
                    await Shell.Current.GoToAsync("//login");
                    return;
                }

                if (MarcaPicker.SelectedIndex < 0 ||
                    ModeloPicker.SelectedIndex < 0 ||
                    string.IsNullOrWhiteSpace(PrecioEntry.Text) ||
                    string.IsNullOrWhiteSpace(MillasEntry.Text) ||
                    string.IsNullOrWhiteSpace(DescripcionEditor.Text))
                {
                    await DisplayAlert("Validación", "Completa todos los campos obligatorios.", "OK");
                    return;
                }

                if (!float.TryParse(PrecioEntry.Text.Trim(), out var precioIngresado))
                {
                    await DisplayAlert("Validación", "El precio no es válido.", "OK");
                    return;
                }

                if (!float.TryParse(MillasEntry.Text.Trim(), out var millasIngresadas))
                {
                    await DisplayAlert("Validación", "Las millas no son válidas.", "OK");
                    return;
                }

                float? minPrimaConvertida = null;
                if (!string.IsNullOrWhiteSpace(MinPrimaEntry.Text))
                {
                    if (!float.TryParse(MinPrimaEntry.Text.Trim(), out var minPrimaIngresada))
                    {
                        await DisplayAlert("Validación", "La prima mínima no es válida.", "OK");
                        return;
                    }

                    minPrimaConvertida = minPrimaIngresada / 1000f;
                }

                float? minInteresConvertido = null;
                if (!string.IsNullOrWhiteSpace(MinInteresEntry.Text))
                {
                    if (!float.TryParse(MinInteresEntry.Text.Trim(), out var minInteresIngresado))
                    {
                        await DisplayAlert("Validación", "El interés mínimo no es válido.", "OK");
                        return;
                    }

                    minInteresConvertido = minInteresIngresado;
                }

                string? fotoUrl = null;
                if (_fotoSeleccionada != null)
                {
                    fotoUrl = await _supabaseService.SubirFotoMotoAsync(_fotoSeleccionada);
                }

                var estadoDisponible = await _supabaseService.GetEstadoMotoDisponibleIdAsync();
                if (estadoDisponible == null)
                {
                    await DisplayAlert("Base de datos", "No existe el estado 'Disponible' para motos.", "OK");
                    return;
                }

                var modelo = (Modelo)ModeloPicker.SelectedItem;

                var moto = new Moto
                {
                    IdUsuario = _sessionService.UsuarioActual.IdUsuario,
                    IdModelo = modelo.IdModelo,
                    PrecioPublicado = precioIngresado / 1000f,
                    Millas = millasIngresadas / 1000f,
                    Descripcion = DescripcionEditor.Text.Trim(),
                    IdEstado = estadoDisponible.Value,
                    FechaPublicacion = DateTime.Today,
                    FotoUrl = fotoUrl,
                    MinPrima = minPrimaConvertida,
                    MinInteres = minInteresConvertido
                };

                // ... dentro de OnPublicarClicked ...

                var ok = await _supabaseService.PublicarMotoAsync(moto);

                if (ok)
                {
                    // 1. Enviamos el correo en segundo plano
                    _ = EnviarEmailConfirmacion(moto); // El guion bajo indica que no esperamos el resultado para continuar

                    // 2. Mostramos el mensaje (Alert o Toast)
                    await DisplayAlert("Éxito", "Moto publicada correctamente. Hemos enviado un correo de confirmación.", "OK");

                    // 3. Navegamos atrás
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo publicar la moto.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                _isPublishing = false;
                PublicarButton.IsEnabled = true;
            }
        }
    }
}