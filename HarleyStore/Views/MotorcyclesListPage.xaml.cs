using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views;

[QueryProperty(nameof(SearchText), "search")]
public partial class MotorcycleListPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private List<Moto> _motos = new();

    public string SearchText { get; set; } = string.Empty;

    public MotorcycleListPage()
    {
        InitializeComponent();
        _supabaseService = Application.Current!.Handler!.MauiContext!.Services.GetService<SupabaseService>()!;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            _motos = await _supabaseService.GetMotosAsync();
        else
            _motos = await _supabaseService.BuscarMotosAsync(SearchText);

        BuscarSearchBar.Text = SearchText;
        MotosCollectionView.ItemsSource = _motos;
    }

    private async void OnSearchButtonPressed(object sender, EventArgs e)
    {
        SearchText = BuscarSearchBar.Text ?? string.Empty;
        await CargarAsync();
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
}