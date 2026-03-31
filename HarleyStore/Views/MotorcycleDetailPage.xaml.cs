using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    [QueryProperty(nameof(MotoSeleccionada), "MotoSeleccionada")]
    public partial class MotorcycleDetailPage : ContentPage, IQueryAttributable
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;
        private Moto? _moto;

        public Moto MotoSeleccionada
        {
            get => _moto!;
            set
            {
                _moto = value;
                CargarVista();
            }
        }

        public MotorcycleDetailPage()
        {
            InitializeComponent();
            _supabaseService = ServiceHelper.GetService<SupabaseService>();
            _sessionService = ServiceHelper.GetService<SessionService>();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("MotoSeleccionada", out var motoObj) && motoObj is Moto moto)
            {
                _moto = moto;
                CargarVista();
            }
        }

        private void CargarVista()
        {
            if (_moto == null)
                return;

            MotoImage.Source = _moto.FotoMostrada;
            TituloLabel.Text = $"{_moto.Modelo?.Marca?.NombreMarca} {_moto.Modelo?.NombreModelo}";
            DescripcionLabel.Text = _moto.Descripcion;
            EngineLabel.Text = $"Engine Type: {_moto.Modelo?.Motor?.TipoMotor}";
            PowerLabel.Text = $"Power: {_moto.Modelo?.Motor?.Hp} hp";
            MileageLabel.Text = $"Mileage: {_moto.Millas:N0} millas";
            PriceLabel.Text = $"Precio: ${_moto.PrecioPublicado:N0}";
        }

        private async void OnFavoritoClicked(object sender, EventArgs e)
        {
            if (_moto == null)
                return;

            if (_sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión para usar favoritos.", "OK");
                return;
            }

            var yaEs = await _supabaseService.EsFavoritoAsync(_sessionService.UsuarioActual.IdUsuario, _moto.IdMoto);
            bool ok;

            if (yaEs)
            {
                ok = await _supabaseService.QuitarFavoritoAsync(_sessionService.UsuarioActual.IdUsuario, _moto.IdMoto);
                await DisplayAlertAsync("Favoritos",
                    ok ? "Moto eliminada de favoritos." : "No se pudo quitar de favoritos.",
                    "OK");
            }
            else
            {
                ok = await _supabaseService.AgregarFavoritoAsync(new Favorito
                {
                    IdUsuario = _sessionService.UsuarioActual.IdUsuario,
                    IdMoto = _moto.IdMoto,
                    FechaAgregado = DateTime.Today
                });

                await DisplayAlertAsync("Favoritos",
                    ok ? "Moto agregada a favoritos." : "No se pudo guardar en favoritos.",
                    "OK");
            }
        }

        private async void OnContactarClicked(object sender, EventArgs e)
        {
            await DisplayAlertAsync("Contacto", "Aquí luego puedes conectar ofertas o soporte.", "OK");
        }
    }
}