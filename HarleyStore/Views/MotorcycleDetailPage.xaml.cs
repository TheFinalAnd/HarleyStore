using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HarleyStore.Models;
using HarleyStore.Services;

namespace HarleyStore.Views
{
    [QueryProperty(nameof(MotoSeleccionada), "MotoSeleccionada")]
    public partial class MotorcycleDetailPage : ContentPage, IQueryAttributable
    {
        private readonly SupabaseService _supabaseService;
        private readonly SessionService _sessionService;
        private readonly INotificationService _notificationService;
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

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            if (_moto == null) return;

            // Navega a la página de edición
            await Shell.Current.GoToAsync(nameof(EditMotoPage), true, new Dictionary<string, object>
            {
                ["MotoEditar"] = _moto
            });
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            if (_moto == null) return;

            bool confirmar = await DisplayAlertAsync("Eliminar", "¿Seguro que deseas eliminar esta moto? También se eliminará la foto.", "Sí", "No");
            if (!confirmar) return;

            // Validación previa para asegurar que no tenga ofertas aceptadas
            var puede = await PuedeEliminarMotoAsync();
            if (!puede)
            {
                await DisplayAlertAsync("Operación no permitida", "No se puede eliminar la moto porque tiene ofertas aceptadas o pagos registrados.", "OK");
                return;
            }

            var ok = await _supabaseService.EliminarMotoConFotoAsync(_moto);
            await DisplayAlertAsync("Resultado", ok ? "Moto eliminada correctamente." : "No se pudo eliminar la moto.", "OK");

            if (ok)
                await Shell.Current.GoToAsync("..");
        }

        public MotorcycleDetailPage()
        {
            InitializeComponent();
            _supabaseService = ServiceHelper.GetService<SupabaseService>();
            _sessionService = ServiceHelper.GetService<SessionService>();
            _notificationService = ServiceHelper.GetService<INotificationService>();
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
            // Mostrar condiciones mínimas si están definidas (convertir de unidades a miles para mostrar)
            if (_moto.MinPrima.HasValue)
                PrimaMinLabel.Text = $"Prima mínima: {_moto.MinPrima.Value * 1000f:N0} miles";
            if (_moto.MinInteres.HasValue)
                InteresMinLabel.Text = $"Interés mínimo: {_moto.MinInteres.Value:N2}% por cuota";
            EngineLabel.Text = $"Engine Type: {_moto.Modelo?.Motor?.TipoMotor}";
            PowerLabel.Text = $"Power: {_moto.Modelo?.Motor?.Hp} hp";
            MileageLabel.Text = $"Mileage: {_moto.Millas:N0} millas";
            PriceLabel.Text = $"Precio: ${_moto.PrecioPublicado * 1000f:N0}";

            var esDueno = _sessionService.UsuarioActual != null && _sessionService.UsuarioActual.IdUsuario == _moto.IdUsuario;

            // Mostrar/Ocultar botones
            EditButton.IsVisible = esDueno;
            DeleteButton.IsVisible = esDueno;

            ContactarButton.IsVisible = !esDueno;
            OfertarButton.IsVisible = !esDueno;

            _ = CargarOfertasAsync();
        }

        private async Task CargarOfertasAsync()
        {
            try
            {
                if (_moto == null) return;
                var ofertas = await _supabaseService.GetOfertasByMotoAsync(_moto.IdMoto);

                // Enriquecer con calculos y precio por cuota
                var lista = new List<OfertaViewModel>();
                foreach (var o in ofertas)
                {
                    var totalPagado = await GetTotalPagadoByOfertaAsync(o.IdOferta);
                    // convertir valores almacenados en 'miles' a unidades para mostrar/operar
                    var precioUnits = o.PrecioOfertado * 1000f;
                    var primaUnits = o.Prima.HasValue ? o.Prima.Value * 1000f : 0f;
                    var deudaBase = precioUnits - primaUnits;
                    var pendiente = deudaBase - totalPagado; // Calcula la deuda pendiente
                    if (pendiente < 0) pendiente = 0f;

                    float precioPorCuota = 0f;
                    if (o.SolicitaCuotas && o.CantidadCuotas.HasValue && o.CantidadCuotas.Value > 0)
                    {
                        // usar fórmula de amortización si hay interés definido
                        var interesVal = o.Interes.HasValue ? o.Interes.Value : 0f;
                        precioPorCuota = Services.FinanceHelper.CalculateInstallment(pendiente, interesVal, o.CantidadCuotas.Value);
                    }

                    lista.Add(new OfertaViewModel
                    {
                        IdOferta = o.IdOferta,
                        IdMoto = o.IdMoto,
                        IdUsuario = o.IdUsuario,
                        PrecioOfertado = precioUnits,
                        SolicitaCuotas = o.SolicitaCuotas,
                        CantidadCuotas = o.CantidadCuotas,
                        Prima = primaUnits,
                        Interes = o.Interes,
                        IdEstado = (short)o.IdEstado,
                        Fecha = o.Fecha,
                        TotalPagado = totalPagado,
                        DeudaPendiente = pendiente,
                        PrecioPorCuota = precioPorCuota
                    });
                }

                OfertasCollectionView.ItemsSource = lista;
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"No se pudieron cargar las ofertas: {ex.Message}", "OK");
            }
        }

        private async Task<float> GetTotalPagadoByOfertaAsync(long idOferta)
        {
            try
            {
                var cuotas = await _supabaseService.GetCuotasByOfertaAsync(idOferta);
                // Contar sólo cuotas que el vendedor haya aceptado
                return cuotas.Where(c => c.Aceptada.HasValue && c.Aceptada.Value).Sum(c => c.Monto);
            }
            catch
            {
                return 0f;
            }
        }

        private async Task<float> GetDeudaPendienteAsync(Oferta oferta)
        {
            // Calcula la deuda pendiente considerando prima y pagos realizados.
            // Deuda base = PrecioOfertado - Prima
            var prima = oferta.Prima.HasValue ? oferta.Prima.Value : 0f;
            var deudaBase = oferta.PrecioOfertado - prima;

            // Sumar pagos realizados
            var totalPagado = await GetTotalPagadoByOfertaAsync(oferta.IdOferta);

            var pendiente = deudaBase - totalPagado;
            return pendiente > 0 ? pendiente : 0f;
        }

        // Calcula el precio por cuota aplicando la tasa de interés por cuota
        private float CalcularPrecioPorCuota(Oferta oferta, float pendiente, int cantidadCuotas)
        {
            if (cantidadCuotas <= 0) return 0f;

            var interes = oferta.Interes.HasValue ? oferta.Interes.Value : 0f; // porcentaje por cuota

            // Precio base por cuota
            var basePorCuota = pendiente / cantidadCuotas;

            // Aplicar interés por cuota (porcentaje)
            var interesPorCuota = basePorCuota * (interes / 100f);

            return basePorCuota + interesPorCuota;
        }

        private async void OnOfertaSelected(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var ofertaVm = e.CurrentSelection.FirstOrDefault() as OfertaViewModel;
                if (ofertaVm == null) return;
                // crear objeto Oferta auxiliar para reutilizar las funciones existentes
                var oferta = new Oferta
                {
                    IdOferta = ofertaVm.IdOferta,
                    IdMoto = ofertaVm.IdMoto,
                    IdUsuario = ofertaVm.IdUsuario,
                    PrecioOfertado = ofertaVm.PrecioOfertado,
                    SolicitaCuotas = ofertaVm.SolicitaCuotas,
                    CantidadCuotas = ofertaVm.CantidadCuotas.HasValue ? (short?)ofertaVm.CantidadCuotas.Value : null,
                    Prima = ofertaVm.Prima,
                    Interes = ofertaVm.Interes,
                    IdEstado = ofertaVm.IdEstado,
                    Fecha = ofertaVm.Fecha
                };
                ((CollectionView)sender).SelectedItem = null;

                var usuario = _sessionService.UsuarioActual;
                if (usuario == null)
                {
                    await DisplayAlertAsync("Sesión", "Debes iniciar sesión para realizar acciones.", "OK");
                    await Shell.Current.GoToAsync("//login");
                    return;
                }

                // Si soy el dueño de la moto
                if (usuario.IdUsuario == _moto!.IdUsuario)
                {
                    var accion = await DisplayActionSheet("Oferta", "Cancelar", null, "Aceptar", "Rechazar");
                    if (accion == "Aceptar")
                    {
                        var ok = await _supabaseService.ActualizarEstadoOfertaAsync(oferta.IdOferta, 5);
                        if (ok)
                        {
                            var proponente = await _supabaseService.GetUsuarioByIdAsync(oferta.IdUsuario);
                            // enviar notificación formal
                            try { await _notificationService.EnviarNotificacionOfertaAceptadaAsync(oferta, proponente!); } catch { }
                            await DisplayAlertAsync("Resultado", "Oferta aceptada.", "OK");
                        }
                        else
                        {
                            await DisplayAlertAsync("Error", "No se pudo aceptar la oferta.", "OK");
                        }
                    }
                    else if (accion == "Rechazar")
                    {
                        var ok = await _supabaseService.ActualizarEstadoOfertaAsync(oferta.IdOferta, 6);
                        if (ok)
                        {
                            var proponente = await _supabaseService.GetUsuarioByIdAsync(oferta.IdUsuario);
                            var email = proponente?.Correo ?? oferta.IdUsuario.ToString();
                            await _notificationService.EnviarNotificacionTextoAsync(email, "Oferta rechazada", $"Tu oferta de {oferta.PrecioOfertado:C} fue rechazada.");
                            await DisplayAlertAsync("Resultado", "Oferta rechazada.", "OK");
                        }
                        else
                        {
                            await DisplayAlertAsync("Error", "No se pudo rechazar la oferta.", "OK");
                        }
                    }
                }
                // Si soy el autor de la oferta
                else if (usuario.IdUsuario == oferta.IdUsuario)
                {
                    var pendiente = await GetDeudaPendienteAsync(oferta);

                    // Construir opciones dependiendo del estado de la oferta: solo mostrar "Registrar abono" si está Aceptada (id_estado == 5)
                    var opciones = new List<string> { "Editar", "Eliminar" };
                    if (oferta.IdEstado == 5 && pendiente > 0)
                        opciones.Add("Registrar abono");

                    var accion = await DisplayActionSheet("Mi oferta", "Cancelar", null, opciones.ToArray());

                    if (accion == "Editar")
                    {
                        var input = await DisplayPromptAsync("Editar oferta", "Ingrese nuevo monto:", "Aceptar", "Cancelar", ofertaVm.PrecioOfertado.ToString(CultureInfo.InvariantCulture), -1, Keyboard.Numeric);
                        if (string.IsNullOrWhiteSpace(input)) return;

                        var normalized = input.Replace(',', '.');
                        if (!float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
                        {
                            await DisplayAlertAsync("Error", "Monto inválido.", "OK");
                            return;
                        }

                        oferta.PrecioOfertado = monto;
                        var updated = await _supabaseService.ActualizarOfertaAsync(oferta.IdMoto, oferta.IdUsuario, oferta.PrecioOfertado);
                        var propietario = await _supabaseService.GetUsuarioByIdAsync(_moto!.IdUsuario);
                        var propietarioEmail = propietario?.Correo ?? _moto!.IdUsuario.ToString();
                        await _notificationService.EnviarNotificacionTextoAsync(propietarioEmail, "Oferta editada", $"La oferta fue editada a {oferta.PrecioOfertado:C}.");
                        await DisplayAlertAsync("Resultado", updated ? "Oferta actualizada." : "No se pudo actualizar la oferta.", "OK");
                    }
                    else if (accion == "Eliminar")
                    {
                        bool confirmar = await DisplayAlertAsync("Eliminar", "¿Eliminar tu oferta?", "Sí", "No");
                        if (!confirmar) return;

                        var deleted = await _supabaseService.EliminarOfertaAsync(oferta.IdMoto, oferta.IdUsuario);
                        if (deleted)
                        {
                            await DisplayAlertAsync("Resultado", "Oferta eliminada.", "OK");
                        }
                        else
                        {
                            await DisplayAlertAsync("Error", "No se pudo eliminar la oferta.", "OK");
                        }
                    }
                    else if (accion == "Registrar abono")
                    {
                        // Asegurarse de que la oferta esté aceptada antes de permitir registrar abono
                        if (oferta.IdEstado != 5)
                        {
                            await DisplayAlertAsync("Operación no permitida", "Solo se pueden registrar abonos para ofertas aceptadas.", "OK");
                            return;
                        }

                        var pendienteActual = await GetDeudaPendienteAsync(new Oferta { IdOferta = ofertaVm.IdOferta, PrecioOfertado = ofertaVm.PrecioOfertado, Prima = ofertaVm.Prima });
                        if (pendienteActual <= 0)
                        {
                            await DisplayAlertAsync("Info", "No hay monto pendiente por pagar.", "OK");
                            return;
                        }

                        // Pedir monto con máximo igual a lo pendiente
                        var input = await DisplayPromptAsync("Registrar abono", $"Ingrese monto del abono (máx {pendienteActual:C}):", "Aceptar", "Cancelar", "", -1, Keyboard.Numeric);
                        if (string.IsNullOrWhiteSpace(input)) return;

                        var normalized = input.Replace(',', '.');
                        if (!float.TryParse(normalized, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var monto) || monto <= 0)
                        {
                            await DisplayAlertAsync("Error", "Monto inválido.", "OK");
                            return;
                        }

                        if (monto > pendienteActual)
                        {
                            await DisplayAlertAsync("Error", $"El monto no puede ser mayor a lo pendiente: {pendienteActual:C}", "OK");
                            return;
                        }

                        var cuota = new Cuota
                        {
                            IdOferta = ofertaVm.IdOferta,
                            Date = DateTime.Today,
                            Monto = monto / 1000f, // almacenar en 'miles'
                            FechaVencimiento = DateTime.Today.AddMonths(1)
                        };

                        var created = await _supabaseService.CrearCuotaAsync(cuota);
                        if (created)
                        {
                            var duenio = await _supabaseService.GetUsuarioByIdAsync(_moto!.IdUsuario);
                            try { await _notificationService.EnviarNotificacionCuotaAsync(oferta, cuota, duenio!); } catch { }
                            await DisplayAlertAsync("Resultado", "Abono registrado correctamente.", "OK");
                        }
                        else
                        {
                            await DisplayAlertAsync("Error", "No se pudo registrar el abono.", "OK");
                        }
                    }
                }

                await CargarOfertasAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", ex.Message, "OK");
            }
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

        private async void OnHacerOfertarClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(OfferPage), true, new Dictionary<string, object>
            {
                ["MotoId"] = _moto!.IdMoto
            });
        }

        private async void OnContactarClicked(object sender, EventArgs e)
        {
            // Buscamos al dueño de la moto para mostrar sus datos
            var usuario = await _supabaseService.GetUsuarioByIdAsync(_moto!.IdUsuario);
            if (usuario != null)
            {
                await DisplayAlert("Contacto",
                    $"Nombre: {usuario.Nombre}\nTeléfono: {usuario.Telefono}\nCorreo: {usuario.Correo}",
                    "OK");
            }
        }

        private async void OnAccionClicked(object sender, EventArgs e)
        {
            if (_moto == null) return;

            var usuario = _sessionService.UsuarioActual;

            // Si no hay sesión: mostrar opción para iniciar sesión o contactar.
            if (usuario == null)
            {
                await DisplayAlertAsync("Sesión", "Debes iniciar sesión para realizar acciones.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            var esDueno = usuario.IdUsuario == _moto.IdUsuario;

            if (esDueno)
            {
                // Mostrar menú para editar o eliminar
                var accion = await DisplayActionSheet("Acción", "Cancelar", null, "Editar", "Eliminar");
                if (accion == "Editar")
                {
                    await Shell.Current.GoToAsync(nameof(EditMotoPage), true, new Dictionary<string, object>
                    {
                        ["MotoEditar"] = _moto
                    });
                }
                else if (accion == "Eliminar")
                {
                        bool confirmar = await DisplayAlertAsync("Eliminar", "¿Seguro que deseas eliminar esta moto? También se eliminará la foto.", "Sí", "No");
                        if (!confirmar) return;

                        var puede = await PuedeEliminarMotoAsync();
                        if (!puede)
                        {
                            await DisplayAlertAsync("Operación no permitida", "No se puede eliminar la moto porque tiene ofertas aceptadas o pagos registrados.", "OK");
                            return;
                        }

                        var ok = await _supabaseService.EliminarMotoConFotoAsync(_moto);
                        await DisplayAlertAsync("Resultado", ok ? "Moto eliminada correctamente." : "No se pudo eliminar la moto.", "OK");

                        if (ok)
                            await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                // Para terceros: mostrar diálogo para ofertar o contactar
                var accion = await DisplayActionSheet("Acción", "Cancelar", null, "Contactar", "Hacer oferta");
                if (accion == "Contactar")
                {
                    await DisplayAlertAsync("Contacto", "Se abrirá el correo para contactar al vendedor.", "OK");
                    await Launcher.Default.OpenAsync($"mailto:soporte@harleystore.com?subject=Interés%20en%20moto%20{Uri.EscapeDataString(_moto.Modelo?.NombreModelo ?? "")}%20");
                }
                else if (accion == "Hacer oferta")
                {
                    // Navegar a la página de creación de oferta para completar todos los datos
                    await Shell.Current.GoToAsync(nameof(OfferPage), true, new Dictionary<string, object>
                    {
                        ["MotoId"] = _moto.IdMoto
                    });
                }
            }
        }

        // Helper para validar si se puede eliminar la moto: no debe haber ofertas aceptadas ni cuotas realizadas
        private async Task<bool> PuedeEliminarMotoAsync()
        {
            if (_moto == null) return false;

            var ofertas = await _supabaseService.GetOfertasByMotoAsync(_moto.IdMoto);
            // Si existe alguna oferta aceptada
            if (ofertas.Any(o => o.IdEstado == 5)) return false;

            // Verificar cuotas en todas las ofertas
            foreach (var o in ofertas)
            {
                var cuotas = await _supabaseService.GetCuotasByOfertaAsync(o.IdOferta);
                if (cuotas.Any()) return false;
            }

            return true;
        }
    }
}