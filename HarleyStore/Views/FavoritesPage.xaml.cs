using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class FavoritesPage : ContentPage
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;
        private List<Moto> _favoritas = new();

        public FavoritesPage()
        {
            InitializeComponent();
            _supabaseService = ServiceHelper.GetService<SupabaseService>();
            _sessionService = ServiceHelper.GetService<SessionService>();   
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                return;
            }

            await CargarFavoritosAsync();
        }

        private async Task CargarFavoritosAsync()
        {
            var favoritos = await _supabaseService.GetFavoritosAsync(_sessionService.UsuarioActual!.IdUsuario);
            var motos = await _supabaseService.GetMotosAsync();

            _favoritas = motos
                .Where(m => favoritos.Any(f => f.IdMoto == m.IdMoto))
                .ToList();

            FavoritosCollectionView.ItemsSource = _favoritas;
        }

        private async void OnMotoTapped(object sender, TappedEventArgs e)
        {
            try
            {
                if (sender is not Frame frame || frame.BindingContext is not Moto moto)
                    return;

                await Shell.Current.GoToAsync(nameof(MotorcycleDetailPage), true, new Dictionary<string, object>
                {
                    ["MotoSeleccionada"] = moto
                });
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnMotoSelected(object sender, SelectionChangedEventArgs e)
        {
            var moto = e.CurrentSelection.FirstOrDefault() as Moto;

            if (moto == null)
                return;

            ((CollectionView)sender).SelectedItem = null;

            await Shell.Current.GoToAsync(nameof(MotorcycleDetailPage), true, new Dictionary<string, object>
            {
                ["MotoSeleccionada"] = moto
            });
        }
    }
}