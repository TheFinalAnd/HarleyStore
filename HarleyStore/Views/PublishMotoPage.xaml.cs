using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class PublishMotoPage : ContentPage
    {

        private FileResult? _fotoSeleccionada;
        private string? _fotoUrlPublica;

        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;

        private List<Marca> _marcas = new();
        private List<Modelo> _modelos = new();
        private List<Modelo> _modelosFiltrados = new();


        public PublishMotoPage(SupabaseService supabaseService, SessionService sessionService)
        {
            InitializeComponent();
            _supabaseService = supabaseService;
            _sessionService = sessionService;
        }

        private void OnPublicarCompleted(object sender, EventArgs e)
        {
            OnPublicarClicked(sender, e);
        }

        private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
        {
            try
            {
                _fotoSeleccionada = await MediaPicker.Default.PickPhotoAsync();

                if (_fotoSeleccionada != null)
                {
                    var localPath = _fotoSeleccionada.FullPath;
                    FotoPreview.Source = ImageSource.FromFile(localPath);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarDatosAsync();
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
                await DisplayAlertAsync("Error", $"No se pudieron cargar marcas/modelos: {ex.Message}", "OK");
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

                string? fotoUrl = null;

                if (_fotoSeleccionada != null)
                {
                    fotoUrl = await _supabaseService.SubirFotoMotoAsync(_fotoSeleccionada);
                }

                if (_sessionService.UsuarioActual == null)
                {
                    await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                    return;
                }

                if (MarcaPicker.SelectedIndex < 0 ||
                    ModeloPicker.SelectedIndex < 0 ||
                    string.IsNullOrWhiteSpace(PrecioEntry.Text) ||
                    string.IsNullOrWhiteSpace(MillasEntry.Text) ||
                    string.IsNullOrWhiteSpace(DescripcionEditor.Text))
                {
                    await DisplayAlertAsync("Validación", "Completa todos los campos.", "OK");
                    return;
                }

                var estadoDisponible = await _supabaseService.GetEstadoMotoDisponibleIdAsync();
                if (estadoDisponible == null)
                {
                    await DisplayAlertAsync("Base de datos", "No existe el estado 'Disponible' para motos.", "OK");
                    return;
                }

                var modelo = (Modelo)ModeloPicker.SelectedItem;
                var moto = new Moto
                {
                    IdUsuario = _sessionService.UsuarioActual.IdUsuario,
                    IdModelo = modelo.IdModelo,
                    PrecioPublicado = float.Parse(PrecioEntry.Text.Trim()),
                    Millas = float.Parse(MillasEntry.Text.Trim()),
                    Descripcion = DescripcionEditor.Text.Trim(),
                    IdEstado = estadoDisponible.Value,
                    FechaPublicacion = DateTime.Today,
                    FotoUrl = fotoUrl
                };

                var ok = await _supabaseService.PublicarMotoAsync(moto);

                await DisplayAlertAsync("Publicación",
                    ok ? "Moto publicada correctamente." : "No se pudo publicar la moto.",
                    "OK");

                if (ok)
                    await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
    }
}