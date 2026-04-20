using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    [QueryProperty(nameof(MotoEditar), "MotoEditar")]
    public partial class EditMotoPage : ContentPage, IQueryAttributable
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;

        private List<Marca> _marcas = new();
        private List<Modelo> _modelos = new();
        private List<Modelo> _modelosFiltrados = new();

        private Moto? _moto;
        private FileResult? _fotoSeleccionada;

        public Moto MotoEditar
        {
            get => _moto!;
            set
            {
                _moto = value;
            }
        }

        private void OnGuardarCompleted(object sender, EventArgs e)
        {
            OnGuardarClicked(sender, e);
        }
        public EditMotoPage()
        {
            InitializeComponent();
            _supabaseService = ServiceHelper.GetService<SupabaseService>();
            _sessionService = ServiceHelper.GetService<SessionService>();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("MotoEditar", out var motoObj) && motoObj is Moto moto)
            {
                _moto = moto;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_sessionService == null || _sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            if (_moto == null)
            {
                await DisplayAlertAsync("Error", "No se recibió la moto a editar.", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            await CargarDatosAsync();
            CargarMotoEnPantalla();
        }

        private async Task CargarDatosAsync()
        {
            try
            {
                _marcas = await _supabaseService.GetMarcasAsync();
                _modelos = await _supabaseService.GetModelosAsync();

                MarcaPicker.ItemsSource = _marcas;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudieron cargar marcas/modelos: {ex.Message}", "OK");
            }
        }

        private void CargarMotoEnPantalla()
        {
            if (_moto == null)
                return;

            // Mostrar valores en unidades para que el usuario edite (la base usa 'miles')
            PrecioEntry.Text = (_moto.PrecioPublicado * 1000f).ToString();
            MillasEntry.Text = _moto.Millas.ToString();
            DescripcionEditor.Text = _moto.Descripcion;
            FotoPreview.Source = _moto.FotoMostrada;

            if (_moto.Modelo == null)
                return;

            var marcaActual = _marcas.FirstOrDefault(m => m.IdMarca == (_moto.Modelo.Marca?.IdMarca ?? 0));
            if (marcaActual != null)
            {
                MarcaPicker.SelectedItem = marcaActual;

                _modelosFiltrados = _modelos
                    .Where(m => m.IdMarca == marcaActual.IdMarca)
                    .ToList();

                ModeloPicker.ItemsSource = _modelosFiltrados;

                var modeloActual = _modelosFiltrados.FirstOrDefault(m => m.IdModelo == _moto.IdModelo);
                if (modeloActual != null)
                {
                    ModeloPicker.SelectedItem = modeloActual;
                }
            }

            // Mostrar condiciones financieras en las unidades que el usuario entiende (miles y porcentaje)
            if (_moto.MinPrima.HasValue)
                MinPrimaEntry.Text = (_moto.MinPrima.Value * 1000f).ToString();

            if (_moto.MinInteres.HasValue)
                MinInteresEntry.Text = _moto.MinInteres.Value.ToString();
        }

        private void OnMarcaChanged(object sender, EventArgs e)
        {
            if (MarcaPicker.SelectedItem is not Marca marcaSeleccionada)
                return;

            _modelosFiltrados = _modelos
                .Where(m => m.IdMarca == marcaSeleccionada.IdMarca)
                .ToList();

            ModeloPicker.ItemsSource = _modelosFiltrados;
        }

        private async void OnSeleccionarFotoClicked(object sender, EventArgs e)
        {
            try
            {
                _fotoSeleccionada = await MediaPicker.Default.PickPhotoAsync();

                if (_fotoSeleccionada != null)
                {
                    using var stream = await _fotoSeleccionada.OpenReadAsync();
                    var memory = new MemoryStream();
                    await stream.CopyToAsync(memory);
                    memory.Position = 0;

                    FotoPreview.Source = ImageSource.FromStream(() => new MemoryStream(memory.ToArray()));
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudo seleccionar la foto: {ex.Message}", "OK");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            try
            {
                if (_moto == null)
                {
                    await DisplayAlertAsync("Error", "No se encontró la moto.", "OK");
                    return;
                }

                if (_sessionService == null || _sessionService.UsuarioActual == null)
                {
                    await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                    return;
                }

                if (_moto.IdUsuario != _sessionService.UsuarioActual.IdUsuario)
                {
                    await DisplayAlertAsync("Permiso", "No puedes editar una moto que no es tuya.", "OK");
                    return;
                }

                if (MarcaPicker.SelectedItem is not Marca)
                {
                    await DisplayAlertAsync("Validación", "Selecciona una marca.", "OK");
                    return;
                }

                if (ModeloPicker.SelectedItem is not Modelo modeloSeleccionado)
                {
                    await DisplayAlertAsync("Validación", "Selecciona un modelo.", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(PrecioEntry.Text) ||
                    string.IsNullOrWhiteSpace(MillasEntry.Text) ||
                    string.IsNullOrWhiteSpace(DescripcionEditor.Text))
                {
                    await DisplayAlertAsync("Validación", "Completa todos los campos.", "OK");
                    return;
                }

                // Validar que se establezcan los mínimos de prima e interés
                if (string.IsNullOrWhiteSpace(MinPrimaEntry.Text) || !float.TryParse(MinPrimaEntry.Text.Trim(), out var minPrimaUnits) || minPrimaUnits < 0)
                {
                    await DisplayAlertAsync("Validación", "Ingresa una prima mínima válida (en miles).", "OK");
                    return;
                }

                if (string.IsNullOrWhiteSpace(MinInteresEntry.Text) || !float.TryParse(MinInteresEntry.Text.Trim(), out var minInteres) || minInteres < 0)
                {
                    await DisplayAlertAsync("Validación", "Ingresa un porcentaje de interés mínimo válido.", "OK");
                    return;
                }

                string? nuevaFotoUrl = _moto.FotoUrl;

                if (_fotoSeleccionada != null)
                {
                    var fotoSubida = await _supabaseService.SubirFotoMotoAsync(_fotoSeleccionada);
                    if (string.IsNullOrWhiteSpace(fotoSubida))
                    {
                        await DisplayAlertAsync("Foto", "No se pudo subir la nueva foto.", "OK");
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(_moto.FotoUrl))
                    {
                        await _supabaseService.EliminarFotoMotoAsync(_moto.FotoUrl);
                    }

                    nuevaFotoUrl = fotoSubida;
                }

                _moto.IdModelo = modeloSeleccionado.IdModelo;
                // Precio: usuario ingresa unidades, la base espera 'miles' -> dividir por 1000
                _moto.PrecioPublicado = float.Parse(PrecioEntry.Text.Trim()) / 1000f;
                _moto.Millas = float.Parse(MillasEntry.Text.Trim());
                _moto.Descripcion = DescripcionEditor.Text.Trim();
                _moto.FotoUrl = nuevaFotoUrl;

                // Campos opcionales de condiciones financieras (ya validados obligatorios más arriba)
                _moto.MinPrima = minPrimaUnits / 1000f;
                _moto.MinInteres = minInteres;

                var ok = await _supabaseService.ActualizarMotoAsync(_moto);

                await DisplayAlertAsync("Resultado",
                    ok ? "Moto actualizada correctamente." : "No se pudo actualizar la moto.",
                    "OK");

                if (ok)
                    await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}