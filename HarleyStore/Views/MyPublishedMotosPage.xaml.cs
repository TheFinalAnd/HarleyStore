using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    public partial class MyPublishedMotosPage : ContentPage
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;
        private List<Moto> _misMotos = new();

        public MyPublishedMotosPage()
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
                await Shell.Current.GoToAsync("//login");
                return;
            }

            await CargarMisMotosAsync();
        }

        private async Task CargarMisMotosAsync()
        {
            _misMotos = await _supabaseService.GetMotosByUsuarioAsync(_sessionService.UsuarioActual!.IdUsuario);
            MisMotosCollectionView.ItemsSource = _misMotos;
        }

        private async void OnMotoSelected(object sender, SelectionChangedEventArgs e)
        {
            var moto = e.CurrentSelection.FirstOrDefault() as Moto;
            if (moto == null) return;

            ((CollectionView)sender).SelectedItem = null;

            await Shell.Current.GoToAsync(nameof(MotorcycleDetailPage), true, new Dictionary<string, object>
            {
                ["MotoSeleccionada"] = moto
            });
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var moto = button?.CommandParameter as Moto;

            if (moto == null) return;

            await Shell.Current.GoToAsync(nameof(EditMotoPage), true, new Dictionary<string, object>
            {
                ["MotoEditar"] = moto
            });
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var moto = button?.CommandParameter as Moto;

            if (moto == null) return;

            bool confirmar = await DisplayAlertAsync(
                "Eliminar",
                "¿Seguro que deseas eliminar esta moto? También se eliminará la foto.",
                "Sí",
                "No");

            if (!confirmar) return;

            var ok = await _supabaseService.EliminarMotoConFotoAsync(moto);

            await DisplayAlertAsync("Resultado",
                ok ? "Moto eliminada correctamente." : "No se pudo eliminar la moto.",
                "OK");

            if (ok)
                await CargarMisMotosAsync();
        }
    }
}