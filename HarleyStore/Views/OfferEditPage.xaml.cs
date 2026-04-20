using System;
using System.Collections.Generic;
using System.Linq;
using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views;

[QueryProperty(nameof(OfertaParam), "OfertaParam")]
public partial class OfferEditPage : ContentPage, IQueryAttributable
{
    private readonly SupabaseService _supabaseService;
    private readonly SessionService _sessionService;
    private Oferta? OfertaParam;

    public OfferEditPage()
    {
        InitializeComponent();
        _supabaseService = ServiceHelper.GetService<SupabaseService>();
        _sessionService = ServiceHelper.GetService<SessionService>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("OfertaParam", out var obj) && obj is Oferta o)
        {
            OfertaParam = o;
            CargarEnPantalla();
        }
    }

    private void CargarEnPantalla()
    {
        if (OfertaParam == null) return;

        // mostrar en unidades (la base guarda 'miles')
        PrecioEntry.Text = (OfertaParam.PrecioOfertado * 1000f).ToString();
        SolicitaCuotasSwitch.IsToggled = OfertaParam.SolicitaCuotas;
        CantidadCuotasEntry.Text = OfertaParam.CantidadCuotas?.ToString() ?? string.Empty;
        PrimaEntry.Text = OfertaParam.Prima.HasValue ? (OfertaParam.Prima.Value * 1000f).ToString() : string.Empty;
        InteresEntry.Text = OfertaParam.Interes.HasValue ? OfertaParam.Interes.Value.ToString("N2") : "";

        CantidadCuotasEntry.IsEnabled = SolicitaCuotasSwitch.IsToggled;
    }

    private void OnSolicitaCuotasToggled(object sender, ToggledEventArgs e)
    {
        CantidadCuotasEntry.IsEnabled = e.Value;
    }

    private async void OnGuardarClicked(object sender, EventArgs e)
    {
        if (OfertaParam == null) return;

        if (_sessionService?.UsuarioActual == null)
        {
            await DisplayAlertAsync("Sesión", "Debes iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        // Validar: si la oferta ya fue aceptada y tiene pagos no permitir editar
        var cuotas = await _supabaseService.GetCuotasByOfertaAsync(OfertaParam.IdOferta);
        if (OfertaParam.IdEstado == 5 && cuotas.Any())
        {
            await DisplayAlertAsync("Operación no permitida", "No se puede editar una oferta que ya tiene pagos registrados.", "OK");
            return;
        }

        // parsear campos
        if (!float.TryParse(PrecioEntry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var precioUnits) || precioUnits <= 0)
        {
            await DisplayAlertAsync("Error", "Precio inválido.", "OK");
            return;
        }

        OfertaParam.PrecioOfertado = precioUnits / 1000f; // guardar en 'miles'
        OfertaParam.SolicitaCuotas = SolicitaCuotasSwitch.IsToggled;

        if (OfertaParam.SolicitaCuotas)
        {
            if (!int.TryParse(CantidadCuotasEntry.Text, out var cuotasNum) || cuotasNum <= 0)
            {
                await DisplayAlertAsync("Error", "Cantidad de cuotas inválida.", "OK");
                return;
            }

            OfertaParam.CantidadCuotas = (short)cuotasNum;

            if (!string.IsNullOrWhiteSpace(PrimaEntry.Text) && float.TryParse(PrimaEntry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var primaUnits))
            {
                OfertaParam.Prima = primaUnits / 1000f; // guardar en miles
            }

            // interest is read-only and comes from moto min interes; we don't allow user override
        }
        else
        {
            OfertaParam.CantidadCuotas = null;
            OfertaParam.Prima = null;
            OfertaParam.Interes = null;
        }

        // If offer was accepted, move back to pending
        if (OfertaParam.IdEstado == 5)
            OfertaParam.IdEstado = 4; // pendiente

        var ok = await _supabaseService.ActualizarOfertaCompletaAsync(OfertaParam);
        await DisplayAlertAsync(ok ? "Resultado" : "Error", ok ? "Oferta actualizada." : "No se pudo actualizar la oferta.", "OK");

        if (ok)
            await Shell.Current.GoToAsync("..", true);
    }

    private async void OnCancelarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..", true);
    }
}
