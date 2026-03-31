using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;
        private List<Moto> _motos = new();

        public HomePage()
        {
            InitializeComponent();

            _supabaseService = ServiceHelper.GetService<SupabaseService>();
            _sessionService = ServiceHelper.GetService<SessionService>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarMotosAsync();
        }

        private async Task CargarMotosAsync()
        {
            try
            {
                _motos = await _supabaseService.GetMotosAsync();
                MotosCollectionView.ItemsSource = _motos.Take(6).ToList();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudieron cargar las motos: {ex.Message}", "OK");
            }
        }

        private async Task IrAFavoritosAsync()
        {
            if (_sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            await Shell.Current.GoToAsync(nameof(FavoritesPage));
        }

        private async Task IrAPerfilAsync()
        {
            if (_sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            await Shell.Current.GoToAsync(nameof(ProfilePage));
        }

        private async Task IrAPublicarMotoAsync()
        {
            if (_sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            await Shell.Current.GoToAsync(nameof(PublishMotoPage));
        }

        private async Task IrAMisMotosAsync()
        {
            if (_sessionService.UsuarioActual == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            await Shell.Current.GoToAsync(nameof(MyPublishedMotosPage));
        }

        private async void OnSearchButtonPressed(object sender, EventArgs e)
        {
            try
            {
                var texto = BuscarSearchBar.Text ?? string.Empty;
                await Shell.Current.GoToAsync($"{nameof(MotorcycleListPage)}?search={Uri.EscapeDataString(texto)}");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnFavoritesClicked(object sender, EventArgs e)
        {
            try
            {
                await IrAFavoritosAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnProfileClicked(object sender, EventArgs e)
        {
            try
            {
                await IrAPerfilAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnPublishClicked(object sender, EventArgs e)
        {
            try
            {
                await IrAPublicarMotoAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnMyBikesClicked(object sender, EventArgs e)
        {
            try
            {
                await IrAMisMotosAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnSeeAllTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(MotorcycleListPage));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnCategoriesTapped(object sender, TappedEventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(CategoriesPage));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnCategoriesClicked(object sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(CategoriesPage));
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnMotoSelected(object sender, SelectionChangedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async void OnMenuClicked(object sender, EventArgs e)
        {
            try
            {
                string action = await DisplayActionSheet(
                    "Menú",
                    "Cancelar",
                    null,
                    "Perfil",
                    "Favoritos",
                    "Publicar moto",
                    "Mis motos",
                    "Cerrar sesión");

                switch (action)
                {
                    case "Perfil":
                        await IrAPerfilAsync();
                        break;

                    case "Favoritos":
                        await IrAFavoritosAsync();
                        break;

                    case "Publicar moto":
                        await IrAPublicarMotoAsync();
                        break;

                    case "Mis motos":
                        await IrAMisMotosAsync();
                        break;

                    case "Cerrar sesión":
                        _sessionService.Logout();
                        await Shell.Current.GoToAsync("//login");
                        break;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
    }
}