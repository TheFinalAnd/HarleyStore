using System;
using HarleyStore.Models;
using HarleyStore.Services;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers; // Importante para el error del StringContent

namespace HarleyStore.Views;

public partial class OfferPage : ContentPage, IQueryAttributable
{
    private readonly SupabaseService _supabaseService;
    private readonly SessionService _sessionService;

    private long _motoId;
    private Moto? _moto;
    private List<Moto> _motosDisponibles = new();

    public OfferPage()
    {
        InitializeComponent();
        _supabaseService = ServiceHelper.GetService<SupabaseService>();
        _sessionService = ServiceHelper.GetService<SessionService>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("MotoId", out var idObj) && idObj is long id)
        {
            _motoId = id;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarMotosDisponiblesAsync();
    }

    private async Task CargarMotosDisponiblesAsync()
    {
        try
        {
            _motosDisponibles = await _supabaseService.GetMotosDisponiblesAsync();
            MotoPicker.ItemsSource = _motosDisponibles;

            if (_motoId > 0)
            {
                _moto = _motosDisponibles.FirstOrDefault(m => m.IdMoto == _motoId);
                if (_moto != null)
                {
                    MotoPicker.SelectedItem = _moto;
                    MotoPicker.IsEnabled = false;
                    await CheckExistingOfferAsync();
                    CargarMotoEnPantalla();
                }
            }
            else
            {
                MotoPicker.IsEnabled = true;
                MotoSeleccionadaLabel.Text = "Selecciona una moto";
                MinPrimaLabel.Text = "Prima mínima: -";
                InteresEntry.Text = "0";
                PrecioPorCuotaLabel.Text = "Precio por cuota: -";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private async void OnMotoPickerChanged(object sender, EventArgs e)
    {
        try
        {
            _moto = MotoPicker.SelectedItem as Moto;
            _motoId = _moto?.IdMoto ?? 0;

            CargarMotoEnPantalla();

            if (_motoId > 0)
                await CheckExistingOfferAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void CargarMotoEnPantalla()
    {
        if (_moto == null)
        {
            MotoSeleccionadaLabel.Text = "Selecciona una moto";
            MinPrimaLabel.Text = "Prima mínima: -";
            InteresEntry.Text = "0";
            PrecioPorCuotaLabel.Text = "Precio por cuota: -";
            return;
        }

        MotoSeleccionadaLabel.Text = $"{_moto.Modelo?.Marca?.NombreMarca} {_moto.Modelo?.NombreModelo}";

        if (_moto.MinPrima.HasValue)
            MinPrimaLabel.Text = $"Prima mínima: {_moto.MinPrima.Value * 1000f:N0} unidades";
        else
            MinPrimaLabel.Text = "Prima mínima: -";

        if (_moto.MinInteres.HasValue)
            InteresEntry.Text = _moto.MinInteres.Value.ToString("N2");
        else
            InteresEntry.Text = "0";

        UpdatePrecioPorCuota();
    }

    public void OnInputChanged(object sender, EventArgs e)
    {
        UpdatePrecioPorCuota();
    }

    private void OnSolicitaCuotasToggled(object sender, ToggledEventArgs e)
    {
        CantidadCuotasEntry.IsEnabled = e.Value;
        CuotasInfoLayout.IsVisible = e.Value;
        UpdatePrecioPorCuota();
    }

    private void UpdatePrecioPorCuota()
    {
        var precioOk = float.TryParse(PrecioEntry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var precioUnits);
        var primaOk = float.TryParse(PrimaEntry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var primaUnits);
        var cuotasOk = int.TryParse(CantidadCuotasEntry.Text, out var cuotas);

        if (!precioOk)
        {
            PrecioPorCuotaLabel.Text = "Precio por cuota: -";
            return;
        }

        var restante = precioUnits - (primaOk ? primaUnits : 0f);
        if (restante < 0) restante = 0f;

        if (SolicitaCuotasSwitch.IsToggled && cuotasOk && cuotas > 0)
        {
            var interesVal = _moto?.MinInteres ?? 0f;
            var precioPorCuota = Services.FinanceHelper.CalculateInstallment(restante, interesVal, cuotas);
            PrecioPorCuotaLabel.Text = $"Precio por cuota: {precioPorCuota:C}";
        }
        else
        {
            PrecioPorCuotaLabel.Text = "Precio por cuota: -";
        }
    }

    private async Task CheckExistingOfferAsync()
    {
        try
        {
            if (_sessionService.UsuarioActual == null || _motoId <= 0)
                return;

            var existing = await _supabaseService.GetOfertaByMotoAndUsuarioAsync(_motoId, _sessionService.UsuarioActual.IdUsuario);
            if (existing != null)
            {
                await DisplayAlert("Oferta existente", "Ya tienes una oferta para esta moto. No puedes crear otra.", "OK");
                await Shell.Current.GoToAsync("..", true);
            }
        }
        catch { }
    }

    private async Task EnviarEmailNotificacion(string emailDestino, string asunto, Oferta oferta, Usuario ofertante)
    {
        try
        {
            // Creamos una estructura HTML clara con CSS inline (para que se vea bien en todos los correos)
            string htmlBody = $@"
        <html>
            <body style='font-family: Arial, sans-serif; line-height: 1.6;'>
                <h2 style='color: #2c3e50;'>Nueva Oferta Recibida</h2>
                
                <h3 style='border-bottom: 2px solid #3498db; color: #3498db;'>Detalles del Ofertante</h3>
                <p><strong>Nombre:</strong> {ofertante.Nombre}</p>
                <p><strong>Teléfono:</strong> {ofertante.Telefono}</p>
                <p><strong>Email:</strong> {ofertante.Correo}</p>

                <h3 style='border-bottom: 2px solid #e67e22; color: #e67e22;'>Detalles de la Oferta</h3>
                <table style='width: 100%; border-collapse: collapse;'>
                    <tr style='background-color: #f2f2f2;'>
                        <th style='text-align: left; padding: 8px;'>Concepto</th>
                        <th style='text-align: left; padding: 8px;'>Valor</th>
                    </tr>
                    <tr><td style='padding: 8px;'>Precio Ofertado</td><td style='padding: 8px;'>${(oferta.PrecioOfertado*1000):N2}</td></tr>
                    <tr><td style='padding: 8px;'>¿Solicita Cuotas?</td><td style='padding: 8px;'>{(oferta.SolicitaCuotas ? "Sí" : "No")}</td></tr>
                    <tr><td style='padding: 8px;'>Cantidad Cuotas</td><td style='padding: 8px;'>{(oferta.CantidadCuotas.HasValue ? oferta.CantidadCuotas.ToString() : "N/A")}</td></tr>
                    <tr><td style='padding: 8px;'>Prima</td><td style='padding: 8px;'>${(oferta.Prima.HasValue ? oferta.Prima.Value.ToString("N2") : "0")}</td></tr>
                </table>
                
                <p style='margin-top: 20px; font-size: 0.9em; color: #7f8c8d;'>Este correo fue generado automáticamente por HarleyStore.</p>
            </body>
        </html>";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "re_XAPo228r_Q1zrEjydqxM6SuSNkNA3Xarn");

            var emailData = new
            {
                from = "andres@arcetest.online",
                to = new[] { emailDestino },
                subject = asunto,
                html = htmlBody
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Error Resend: {error}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error al enviar email: {ex.Message}");
        }
    }

    private async void OnEnviarClicked(object sender, EventArgs e)
    {
        if (_sessionService.UsuarioActual == null)
        {
            await DisplayAlert("Sesión", "Debes iniciar sesión.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        if (_motoId <= 0 || _moto == null)
        {
            await DisplayAlert("Validación", "Selecciona una moto.", "OK");
            return;
        }

        var precioOk = float.TryParse(PrecioEntry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var precioUnits);
        if (!precioOk || precioUnits <= 0)
        {
            await DisplayAlert("Error", "Precio inválido.", "OK");
            return;
        }

        var oferta = new Oferta
        {
            IdMoto = _motoId,
            IdUsuario = _sessionService.UsuarioActual.IdUsuario,
            PrecioOfertado = precioUnits / 1000f,
            SolicitaCuotas = SolicitaCuotasSwitch.IsToggled,
            CantidadCuotas = null,
            Prima = null,
            Interes = null,
            IdEstado = 4,
            Fecha = DateTime.Today
        };

        if (SolicitaCuotasSwitch.IsToggled)
        {
            if (!int.TryParse(CantidadCuotasEntry.Text, out var cuotas) || cuotas <= 0)
            {
                await DisplayAlert("Error", "Cantidad de cuotas inválida.", "OK");
                return;
            }

            oferta.CantidadCuotas = (short)cuotas;

            if (float.TryParse(PrimaEntry.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var primaUnits))
                oferta.Prima = primaUnits / 1000f;

            if (_moto?.MinInteres.HasValue == true)
                oferta.Interes = _moto.MinInteres.Value;
        }

        var ok = await _supabaseService.CrearOfertaAsync(oferta);
        if (ok)
        {
            // 1. Obtener datos del dueño usando '_moto' (la variable de clase)
            var duenio = await _supabaseService.GetUsuarioByIdAsync(_moto.IdUsuario);

            // 2. Enviar email usando 'oferta' (la variable local que creamos arriba)
            if (duenio != null)
            {
                var ofertante = await _supabaseService.GetUsuarioByIdAsync(oferta.IdUsuario);
                await EnviarEmailNotificacion(duenio.Correo, "¡Tienes una nueva oferta!", oferta, ofertante);
            }

            await DisplayAlert("Éxito", "Oferta enviada", "OK");
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await DisplayAlert("Oferta", "No se pudo enviar la oferta.", "OK");
        }
    }
}