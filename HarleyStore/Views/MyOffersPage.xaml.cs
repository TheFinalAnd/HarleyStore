using System.Linq;
using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views;

public partial class MyOffersPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly SessionService _sessionService;

    private OfertaViewModel? _selectedOfferVm;
    private Cuota? _selectedPayment;
    private bool _selectedOfferIsMine;

    public MyOffersPage()
    {
        InitializeComponent();
        _supabaseService = ServiceHelper.GetService<SupabaseService>();
        _sessionService = ServiceHelper.GetService<SessionService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_sessionService?.UsuarioActual == null)
        {
            await DisplayAlert("Sesión", "Debes iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        OcultarDetalle();
        await CargarOfertasAsync();
    }

    private void OcultarDetalle()
    {
        DetailsFrame.IsVisible = false;
        EditPanel.IsVisible = false;
        RegisterPaymentPanel.IsVisible = false;
        ConfirmPaymentButton.IsVisible = false;

        _selectedPayment = null;
        _selectedOfferVm = null;
        PaymentsCollection.SelectedItem = null;
    }

    // Añade esto en MyOffersPage.xaml.cs
    private async Task EnviarEmailPagoAprobado(string emailCliente, string motoNombre, decimal monto)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "re_XAPo228r_Q1zrEjydqxM6SuSNkNA3Xarn");

            var emailData = new
            {
                from = "andres@arcetest.online",
                to = new[] { emailCliente },
                subject = "¡Tu pago ha sido aprobado!",
                html = $@"
                <h1>¡Pago Aprobado!</h1>
                <p>Tu pago de <strong>${monto:N2}</strong> por la moto <strong>{motoNombre}</strong> ha sido aprobado exitosamente.</p>
                <p>Gracias por tu confianza en HarleyStore.</p>"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            await client.PostAsync("https://api.resend.com/emails", content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar email: {ex.Message}");
        }
    }
    private async Task CargarOfertasAsync()
    {
        try
        {
            var usuarioId = _sessionService.UsuarioActual!.IdUsuario;
            var todas = await _supabaseService.GetOfertasRelacionadasConUsuarioAsync(usuarioId);
            var motos = await _supabaseService.GetMotosAsync();

            var queHice = new List<OfertaViewModel>();
            var queMeHicieron = new List<OfertaViewModel>();

            foreach (var o in todas)
            {
                var moto = motos.FirstOrDefault(m => m.IdMoto == o.IdMoto);
                if (moto == null)
                    continue;

                var cuotas = await _supabaseService.GetCuotasByOfertaAsync(o.IdOferta);

                var totalPagado = cuotas
                    .Where(c => c.Aceptada.HasValue && c.Aceptada.Value)
                    .Sum(c => c.Monto);

                var prima = o.Prima ?? 0f;
                var deudaBase = o.PrecioOfertado - prima;
                var deudaPendiente = deudaBase - totalPagado;
                if (deudaPendiente < 0)
                    deudaPendiente = 0f;

                var precioPorCuota = 0f;
                if (o.SolicitaCuotas && o.CantidadCuotas.HasValue && o.CantidadCuotas.Value > 0)
                {
                    var interesVal = o.Interes ?? 0f;
                    precioPorCuota = FinanceHelper.CalculateInstallment(
                        deudaPendiente * 1000f,
                        interesVal,
                        o.CantidadCuotas.Value);
                }

                var usuario = await _supabaseService.GetUsuarioByIdAsync(o.IdUsuario);

                var vm = new OfertaViewModel
                {
                    IdOferta = o.IdOferta,
                    IdMoto = o.IdMoto,
                    IdUsuario = o.IdUsuario,
                    UsuarioEmail = usuario.Correo,
                    PrecioOfertado = o.PrecioOfertado,
                    Prima = o.Prima,
                    Interes = o.Interes,
                    CantidadCuotas = o.CantidadCuotas,
                    SolicitaCuotas = o.SolicitaCuotas,
                    IdEstado = (short)o.IdEstado,
                    Fecha = o.Fecha,
                    TotalPagado = totalPagado,
                    DeudaPendiente = deudaPendiente,
                    PrecioPorCuota = precioPorCuota,
                    MotoPreview = moto.FotoMostrada,
                    MotoNombre = $"{moto.Modelo?.Marca?.NombreMarca} {moto.Modelo?.NombreModelo}"
                };

                vm.PrecioUnits = vm.PrecioOfertado * 1000f;
                vm.PrimaUnits = vm.Prima.HasValue ? vm.Prima.Value * 1000f : 0f;

                AsignarEstadoVisual(vm);

                if (o.IdUsuario == usuarioId)
                    queHice.Add(vm);

                if (moto.IdUsuario == usuarioId && o.IdUsuario != usuarioId)
                    queMeHicieron.Add(vm);
            }

            OfertasQueHiceCollection.ItemsSource = queHice
                .OrderByDescending(x => x.Fecha)
                .ToList();

            OfertasRecibidasCollection.ItemsSource = queMeHicieron
                .OrderByDescending(x => x.Fecha)
                .ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void AsignarEstadoVisual(OfertaViewModel vm)
    {
        switch (vm.IdEstado)
        {
            case 5:
                vm.EstadoTexto = "Aceptada";
                vm.EstadoColor = Colors.Green;
                break;
            case 6:
                vm.EstadoTexto = "Rechazada";
                vm.EstadoColor = Colors.Red;
                break;
            case 4:
                vm.EstadoTexto = "Pendiente";
                vm.EstadoColor = Colors.Orange;
                break;
            default:
                vm.EstadoTexto = "Pendiente";
                vm.EstadoColor = Colors.Orange;
                break;
        }
    }

    private async Task<OfertaViewModel?> BuscarOfertaVmPorIdAsync(long idOferta)
    {
        var usuarioId = _sessionService.UsuarioActual!.IdUsuario;
        var todas = await _supabaseService.GetOfertasRelacionadasConUsuarioAsync(usuarioId);
        var motos = await _supabaseService.GetMotosAsync();

        var o = todas.FirstOrDefault(x => x.IdOferta == idOferta);
        if (o == null) return null;

        var moto = motos.FirstOrDefault(m => m.IdMoto == o.IdMoto);
        if (moto == null) return null;

        var cuotas = await _supabaseService.GetCuotasByOfertaAsync(o.IdOferta);

        var totalPagado = cuotas
            .Where(c => c.Aceptada.HasValue && c.Aceptada.Value)
            .Sum(c => c.Monto);

        var prima = o.Prima ?? 0f;
        var deudaBase = o.PrecioOfertado - prima;
        var deudaPendiente = deudaBase - totalPagado;
        if (deudaPendiente < 0)
            deudaPendiente = 0f;

        var precioPorCuota = 0f;
        if (o.SolicitaCuotas && o.CantidadCuotas.HasValue && o.CantidadCuotas.Value > 0)
        {
            var interesVal = o.Interes ?? 0f;
            precioPorCuota = FinanceHelper.CalculateInstallment(
                deudaPendiente * 1000f,
                interesVal,
                o.CantidadCuotas.Value);
        }

        var vm = new OfertaViewModel
        {
            IdOferta = o.IdOferta,
            IdMoto = o.IdMoto,
            IdUsuario = o.IdUsuario,
            PrecioOfertado = o.PrecioOfertado,
            Prima = o.Prima,
            Interes = o.Interes,
            CantidadCuotas = o.CantidadCuotas,
            SolicitaCuotas = o.SolicitaCuotas,
            IdEstado = (short)o.IdEstado,
            Fecha = o.Fecha,
            TotalPagado = totalPagado,
            DeudaPendiente = deudaPendiente,
            PrecioPorCuota = precioPorCuota,
            MotoPreview = moto.FotoMostrada,
            MotoNombre = $"{moto.Modelo?.Marca?.NombreMarca} {moto.Modelo?.NombreModelo}"
        };

        vm.PrecioUnits = vm.PrecioOfertado * 1000f;
        vm.PrimaUnits = vm.Prima.HasValue ? vm.Prima.Value * 1000f : 0f;
        AsignarEstadoVisual(vm);

        return vm;
    }

    private async void OnAgregarClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(OfferPage));
    }

    private async void OnOfertaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        var ofertaVm = e.CurrentSelection.FirstOrDefault() as OfertaViewModel;
        ((CollectionView)sender).SelectedItem = null;

        if (ofertaVm == null)
            return;

        _selectedOfferIsMine = true;
        await MostrarDetalleOfertaAsync(ofertaVm);
    }

    private async void OnOfertaRecibidaSeleccionada(object sender, SelectionChangedEventArgs e)
    {
        var ofertaVm = e.CurrentSelection.FirstOrDefault() as OfertaViewModel;
        ((CollectionView)sender).SelectedItem = null;

        if (ofertaVm == null)
            return;

        _selectedOfferIsMine = false;
        await MostrarDetalleOfertaAsync(ofertaVm);
    }

    private async Task MostrarDetalleOfertaAsync(OfertaViewModel ofertaVm)
    {
        try
        {
            _selectedOfferVm = ofertaVm;
            _selectedPayment = null;
            PaymentsCollection.SelectedItem = null;

            var motos = await _supabaseService.GetMotosAsync();
            var moto = motos.FirstOrDefault(m => m.IdMoto == ofertaVm.IdMoto);
            if (moto == null)
            {
                await DisplayAlert("Error", "No se encontró la moto de la oferta.", "OK");
                return;
            }

            DetailMotoLabel.Text = ofertaVm.MotoNombre;
            DetailMotoImage.Source = moto.FotoMostrada;
            DetailDescripcionLabel.Text = moto.Descripcion ?? string.Empty;
            DetailInteresLabel.Text = ofertaVm.Interes.HasValue
                ? $"Interés: {ofertaVm.Interes.Value:N2}%"
                : "Interés: -";

            DetailMontosLabel.Text =
                $"Total ofertado: {(ofertaVm.PrecioOfertado * 1000f):C}  -  " +
                $"Pendiente: {(ofertaVm.DeudaPendiente * 1000f):C}  -  " +
                $"Pagado: {(ofertaVm.TotalPagado * 1000f):C}";

            DetailEstadoLabel.Text = ofertaVm.EstadoTexto;
            DetailEstadoLabel.TextColor = ofertaVm.EstadoColor;

            var cuotas = await _supabaseService.GetCuotasByOfertaAsync(ofertaVm.IdOferta);
            PaymentsCollection.ItemsSource = cuotas;

            var esAceptada = ofertaVm.IdEstado == 5;

            ConfirmPaymentButton.IsVisible = !_selectedOfferIsMine && esAceptada;
            RegisterPaymentPanel.IsVisible = _selectedOfferIsMine && esAceptada;

            EditPanel.IsVisible = false;
            DetailsFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnCloseDetailsClicked(object sender, EventArgs e)
    {
        OcultarDetalle();
    }

    private async void OnApproveInlineClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button button || button.BindingContext is not OfertaViewModel vm)
                return;

            if (vm.IdEstado != 4)
            {
                await DisplayAlert("Info", "Solo se pueden aceptar ofertas pendientes.", "OK");
                return;
            }

            var confirmar = await DisplayAlert("Aceptar oferta", "¿Confirmar aceptación de la oferta?", "Sí", "No");
            if (!confirmar)
                return;

            var ok = await _supabaseService.ActualizarEstadoOfertaAsync(vm.IdOferta, 5);

            if (!ok)
            {
                await DisplayAlert("Error", "No se pudo aceptar la oferta.", "OK");
                return;
            }

            try
            {
                var oferta = await _supabaseService.GetOfertaByIdAsync(vm.IdOferta);
                if (oferta != null)
                {
                    var proponente = await _supabaseService.GetUsuarioByIdAsync(oferta.IdUsuario);
                    if (proponente != null)
                    {
                        await ServiceHelper.GetService<INotificationService>()
                            .EnviarNotificacionOfertaAceptadaAsync(oferta, proponente);
                    }
                }
            }
            catch { }

            await DisplayAlert("Resultado", "Oferta aceptada.", "OK");
            await CargarOfertasAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnRejectInlineClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button button || button.BindingContext is not OfertaViewModel vm)
                return;

            if (vm.IdEstado != 4)
            {
                await DisplayAlert("Info", "Solo se pueden rechazar ofertas pendientes.", "OK");
                return;
            }

            var confirmar = await DisplayAlert("Rechazar oferta", "¿Confirmar rechazo de la oferta?", "Sí", "No");
            if (!confirmar)
                return;

            var ok = await _supabaseService.ActualizarEstadoOfertaAsync(vm.IdOferta, 6);

            if (!ok)
            {
                await DisplayAlert("Error", "No se pudo rechazar la oferta.", "OK");
                return;
            }

            await DisplayAlert("Resultado", "Oferta rechazada.", "OK");
            await CargarOfertasAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnEditOfferInlineClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button button || button.BindingContext is not OfertaViewModel vm)
                return;

            _selectedOfferIsMine = true;
            await MostrarDetalleOfertaAsync(vm);
            OnEditOfferClicked(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnDeleteOfferInlineClicked(object sender, EventArgs e)
    {
        try
        {
            if (sender is not Button button || button.BindingContext is not OfertaViewModel vm)
                return;

            var confirmar = await DisplayAlert("Eliminar", "¿Eliminar esta oferta?", "Sí", "No");
            if (!confirmar)
                return;

            var ok = await _supabaseService.EliminarOfertaAsync(vm.IdMoto, vm.IdUsuario);

            await DisplayAlert(ok ? "Resultado" : "Error",
                ok ? "Oferta eliminada." : "No se pudo eliminar la oferta.",
                "OK");

            if (ok)
            {
                OcultarDetalle();
                await CargarOfertasAsync();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnPrecioEditChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedOfferVm == null)
            return;

        if (float.TryParse(PrecioEditEntry.Text, out var units))
            _selectedOfferVm.PrecioUnits = units;
    }

    private void OnSolicitaCuotasEditToggled(object sender, ToggledEventArgs e)
    {
        CantidadCuotasEditEntry.IsEnabled = e.Value;
    }

    private void OnPrimaEditChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedOfferVm == null)
            return;

        if (float.TryParse(PrimaEditEntry.Text, out var units))
            _selectedOfferVm.PrimaUnits = units;
    }

    private void OnEditOfferClicked(object sender, EventArgs e)
    {
        if (_selectedOfferVm == null)
            return;

        PrecioEditEntry.Text = (_selectedOfferVm.PrecioOfertado * 1000f).ToString();
        SolicitaCuotasEditSwitch.IsToggled = _selectedOfferVm.SolicitaCuotas;
        CantidadCuotasEditEntry.Text = _selectedOfferVm.CantidadCuotas?.ToString() ?? string.Empty;
        PrimaEditEntry.Text = _selectedOfferVm.Prima.HasValue
            ? (_selectedOfferVm.Prima.Value * 1000f).ToString()
            : string.Empty;
        InteresEditEntry.Text = _selectedOfferVm.Interes.HasValue
            ? _selectedOfferVm.Interes.Value.ToString("N2")
            : string.Empty;

        CantidadCuotasEditEntry.IsEnabled = SolicitaCuotasEditSwitch.IsToggled;
        EditPanel.IsVisible = true;
    }

    private void OnCancelEditClicked(object sender, EventArgs e)
    {
        EditPanel.IsVisible = false;
    }

    private async void OnSaveEditClicked(object sender, EventArgs e)
    {
        if (_selectedOfferVm == null)
            return;

        var cuotas = await _supabaseService.GetCuotasByOfertaAsync(_selectedOfferVm.IdOferta);
        if (_selectedOfferVm.IdEstado == 5 && cuotas.Any())
        {
            await DisplayAlert("Operación no permitida", "No se puede editar una oferta que ya tiene pagos registrados.", "OK");
            return;
        }

        if (!float.TryParse(PrecioEditEntry.Text, out var precioUnits) || precioUnits <= 0)
        {
            await DisplayAlert("Error", "Precio inválido.", "OK");
            return;
        }

        var oferta = await _supabaseService.GetOfertaByIdAsync(_selectedOfferVm.IdOferta);
        if (oferta == null)
        {
            await DisplayAlert("Error", "Oferta no encontrada.", "OK");
            return;
        }

        oferta.PrecioOfertado = precioUnits / 1000f;
        oferta.SolicitaCuotas = SolicitaCuotasEditSwitch.IsToggled;

        if (oferta.SolicitaCuotas)
        {
            if (!int.TryParse(CantidadCuotasEditEntry.Text, out var cuotasNum) || cuotasNum <= 0)
            {
                await DisplayAlert("Error", "Cantidad de cuotas inválida.", "OK");
                return;
            }

            oferta.CantidadCuotas = (short)cuotasNum;

            if (!string.IsNullOrWhiteSpace(PrimaEditEntry.Text) &&
                float.TryParse(PrimaEditEntry.Text, out var primaUnits))
            {
                oferta.Prima = primaUnits / 1000f;
            }

            if (!string.IsNullOrWhiteSpace(InteresEditEntry.Text) &&
                float.TryParse(InteresEditEntry.Text, out var interes))
            {
                oferta.Interes = interes;
            }
        }
        else
        {
            oferta.CantidadCuotas = null;
            oferta.Prima = null;
            oferta.Interes = null;
        }

        if (oferta.IdEstado == 5)
            oferta.IdEstado = 4;

        var ok = await _supabaseService.ActualizarOfertaCompletaAsync(oferta);

        await DisplayAlert(ok ? "Resultado" : "Error",
            ok ? "Oferta actualizada." : "No se pudo actualizar la oferta.",
            "OK");

        if (ok)
        {
            EditPanel.IsVisible = false;
            await CargarOfertasAsync();
        }
    }

    private async void OnRegisterPaymentClicked(object sender, EventArgs e)
    {
        if (_selectedOfferVm == null)
            return;

        if (_selectedOfferVm.IdEstado != 5)
        {
            await DisplayAlert("Error", "Solo puedes registrar pagos en ofertas aceptadas.", "OK");
            return;
        }

        if (!float.TryParse(PaymentAmountEntry.Text, out var montoUnits) || montoUnits <= 0)
        {
            await DisplayAlert("Error", "Monto inválido.", "OK");
            return;
        }

        var cuota = new Cuota
        {
            IdOferta = _selectedOfferVm.IdOferta,
            Date = DateTime.Today,
            Monto = montoUnits / 1000f,
            FechaVencimiento = DateTime.Today.AddMonths(1),
            Aceptada = null
        };

        var created = await _supabaseService.CrearCuotaAsync(cuota);

        await DisplayAlert(created ? "Resultado" : "Error",
            created ? "Abono registrado correctamente." : "No se pudo registrar el abono.",
            "OK");

        if (created)
        {
            PaymentAmountEntry.Text = string.Empty;
            PaymentsCollection.ItemsSource = await _supabaseService.GetCuotasByOfertaAsync(_selectedOfferVm.IdOferta);
            await CargarOfertasAsync();
        }
    }

    private void OnPaymentSelected(object sender, SelectionChangedEventArgs e)
    {
        _selectedPayment = e.CurrentSelection.FirstOrDefault() as Cuota;
    }

    private async void OnConfirmPaymentClicked(object sender, EventArgs e)
    {
        if (_selectedPayment == null)
        {
            await DisplayAlert("Info", "Selecciona primero un pago.", "OK");
            return;
        }

        var confirmar = await DisplayAlert("Confirmar pago", "¿Confirmar este pago como aceptado?", "Sí", "No");
        if (!confirmar)
            return;

        var ok = await _supabaseService.UpdateCuotaAceptadaAsync(_selectedPayment.IdCuota, true);

        if (!ok)
        {
            await DisplayAlert("Error", "No se pudo confirmar el pago.", "OK");
            return;
        }
        try
        {
            await EnviarEmailPagoAprobado(_selectedOfferVm.UsuarioEmail, _selectedOfferVm.MotoNombre, (decimal)_selectedPayment.Monto);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al enviar notificación: {ex.Message}");
        }
        await DisplayAlert("Resultado", "Pago confirmado.", "OK");


        if (_selectedOfferVm != null)
        {
            PaymentsCollection.ItemsSource = await _supabaseService.GetCuotasByOfertaAsync(_selectedOfferVm.IdOferta);
            await CargarOfertasAsync();
        }
    }
}