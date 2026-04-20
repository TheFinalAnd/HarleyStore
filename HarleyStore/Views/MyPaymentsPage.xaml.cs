using HarleyStore.Models;
using HarleyStore.Services;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace HarleyStore.Views;

public partial class MyPaymentsPage : ContentPage
{
    private readonly SupabaseService _supabaseService;
    private readonly SessionService _sessionService;

    private List<OfertaViewModel> _ofertasAceptadas = new();
    private OfertaViewModel? _ofertaSeleccionadaPago;

    public MyPaymentsPage()
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

        await CargarPagosAsync();
        await CargarOfertasAceptadasAsync();
    }

    private async Task CargarOfertasAceptadasAsync()
    {
        try
        {
            var usuarioId = _sessionService.UsuarioActual!.IdUsuario;
            var ofertas = await _supabaseService.GetOfertasAceptadasByUsuarioAsync(usuarioId);
            var motos = await _supabaseService.GetMotosAsync();

            _ofertasAceptadas = ofertas.Select(o =>
            {
                var moto = motos.FirstOrDefault(m => m.IdMoto == o.IdMoto);
                return new OfertaViewModel
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
                    MotoPreview = moto?.FotoMostrada,
                    MotoNombre = moto != null
                        ? $"{moto.Modelo?.Marca?.NombreMarca} {moto.Modelo?.NombreModelo}"
                        : $"Oferta #{o.IdOferta}"
                };
            }).ToList();

            OfertaPagoPicker.ItemsSource = _ofertasAceptadas;
            OfertaPagoPicker.SelectedItem = null;
            _ofertaSeleccionadaPago = null;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnOfertaPagoChanged(object sender, EventArgs e)
    {
        _ofertaSeleccionadaPago = OfertaPagoPicker.SelectedItem as OfertaViewModel;
    }

    private async Task CargarPagosAsync()
    {
        try
        {
            var usuarioId = _sessionService.UsuarioActual.IdUsuario;
            var todasOfertas = await _supabaseService.GetOfertasRelacionadasConUsuarioAsync(usuarioId);
            var motos = await _supabaseService.GetMotosAsync();

            var pagosQueHice = new List<AbonoViewModel>();
            var pagosRecibidos = new List<AbonoViewModel>();

            foreach (var o in todasOfertas)
            {
                var cuotas = await _supabaseService.GetCuotasByOfertaAsync(o.IdOferta);
                var moto = motos.FirstOrDefault(m => m.IdMoto == o.IdMoto);

                foreach (var c in cuotas)
                {
                    var vm = new AbonoViewModel
                    {
                        IdCuota = c.IdCuota,
                        IdOferta = c.IdOferta,
                        Monto = c.Monto,
                        Date = c.Date,
                        MotoPreview = moto?.FotoMostrada,
                        MotoNombre = moto != null
                            ? $"{moto.Modelo?.Marca?.NombreMarca} {moto.Modelo?.NombreModelo}"
                            : "-"
                    };

                    if (c.Aceptada.HasValue && c.Aceptada.Value)
                    {
                        vm.EstadoTexto = "Aprobado";
                        vm.EstadoColor = Colors.Green;
                    }
                    else
                    {
                        vm.EstadoTexto = "Pendiente";
                        vm.EstadoColor = Colors.Orange;
                    }

                    if (o.IdUsuario == usuarioId)
                        pagosQueHice.Add(vm);

                    if (moto != null && moto.IdUsuario == usuarioId)
                        pagosRecibidos.Add(vm);
                }
            }

            PagosQueHiceCollection.ItemsSource = pagosQueHice
                .OrderByDescending(x => x.Date)
                .ToList();

            PagosRecibidosCollection.ItemsSource = pagosRecibidos
                .OrderByDescending(x => x.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // Añade esto en MyPaymentsPage.xaml.cs
    private async Task EnviarAvisoPagoRegistrado(decimal monto, string motoNombre, string emailDueno)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "re_XAPo228r_Q1zrEjydqxM6SuSNkNA3Xarn");

            var emailData = new
            {
                from = "andres@arcetest.online",
                to = new[] { emailDueno }, // Usamos la variable recibida
                subject = $"Nuevo pago registrado: {motoNombre}",
                html = $@"
                <h1>Nuevo pago recibido</h1>
                <p>Se ha registrado un pago de <strong>${monto:N2}</strong> para la moto <strong>{motoNombre}</strong>.</p>
                <p>El sistema ha procesado la transacción correctamente.</p>"
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

    private async void OnAgregarClicked(object sender, EventArgs e)
    {
        try
        {
            if (_ofertaSeleccionadaPago == null)
            {
                await DisplayAlert("Validación", "Selecciona una oferta.", "OK");
                return;
            }

            if (!float.TryParse(MontoPagoEntry.Text, out var montoUnits) || montoUnits <= 0)
            {
                await DisplayAlert("Error", "Monto inválido.", "OK");
                return;
            }

            var cuota = new Cuota
            {
                IdOferta = _ofertaSeleccionadaPago.IdOferta,
                Date = DateTime.Today,
                Monto = montoUnits / 1000f,
                FechaVencimiento = DateTime.Today.AddMonths(1),
                Aceptada = null
            };

            var created = await _supabaseService.CrearCuotaAsync(cuota);

            if (created)
            {
                // --- AQUÍ ESTABA EL ERROR: Eliminé el bloque de INotificationService ---

                // Enviamos el correo directamente
                try
                {
                    var moto = (await _supabaseService.GetMotosAsync()).FirstOrDefault(m => m.IdMoto == _ofertaSeleccionadaPago.IdMoto);
                    if (moto != null)
                    {
                        var duenio = await _supabaseService.GetUsuarioByIdAsync(moto.IdUsuario);
                        if (duenio != null)
                        {
                            // Asegúrate de usar la propiedad correcta (duenio.Email o duenio.Correo)
                            await EnviarAvisoPagoRegistrado((decimal)cuota.Monto, _ofertaSeleccionadaPago.MotoNombre, duenio.Correo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al enviar email: {ex.Message}");
                }

                await DisplayAlert("Resultado", "Pago registrado correctamente.", "OK");

                MontoPagoEntry.Text = string.Empty;
                OfertaPagoPicker.SelectedItem = null;
                _ofertaSeleccionadaPago = null;
                await CargarPagosAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo registrar el pago.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void OnPagoSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        ((CollectionView)sender).SelectedItem = null;
    }

    private void OnPagoRecibidoSeleccionado(object sender, SelectionChangedEventArgs e)
    {
        ((CollectionView)sender).SelectedItem = null;
    }
}