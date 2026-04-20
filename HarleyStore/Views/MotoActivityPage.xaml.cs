using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views;

public partial class MotoActivityPage : ContentPage, IQueryAttributable
{
    private readonly SupabaseService _supabaseService;
    private readonly SessionService _sessionService;
    private long _motoId;

    public MotoActivityPage()
    {
        InitializeComponent();
        _supabaseService = ServiceHelper.GetService<SupabaseService>();
        _sessionService = ServiceHelper.GetService<SessionService>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("MotoId", out var idObj) && idObj is long id)
        {
            // Guardar id y cargar datos
            _motoId = id;
            _ = LoadActivityAsync();
        }
    }

    private async Task LoadActivityAsync()
    {
        try
        {
            // Cargar ofertas recibidas para la moto
            var ofertas = await _supabaseService.GetOfertasByMotoAsync(_motoId);
            ReceivedOffersCollectionView.ItemsSource = ofertas;

            // Cargar pagos recibidos (cuotas) para las ofertas de esta moto
            var pagos = new List<Cuota>();
            foreach (var o in ofertas)
            {
                var cuotas = await _supabaseService.GetCuotasByOfertaAsync(o.IdOferta);
                pagos.AddRange(cuotas);
            }

            ReceivedPaymentsCollectionView.ItemsSource = pagos;

            // Cargar mis ofertas hechas
            if (_sessionService.UsuarioActual != null)
            {
                var misOfertas = await _supabaseService.GetOfertasByUsuarioAsync(_sessionService.UsuarioActual.IdUsuario);
                MyOffersCollectionView.ItemsSource = misOfertas.Where(o => o.IdMoto == _motoId);

                var misPagos = new List<Cuota>();
                foreach (var o in misOfertas.Where(o => o.IdMoto == _motoId))
                {
                    var cuotas = await _supabaseService.GetCuotasByOfertaAsync(o.IdOferta);
                    misPagos.AddRange(cuotas);
                }

                MyPaymentsCollectionView.ItemsSource = misPagos;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", ex.Message, "OK");
        }
    }
}
