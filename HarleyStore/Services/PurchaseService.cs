using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarleyStore.Models;

namespace HarleyStore.Services
{
    /// <summary>
    /// Servicio simple que mantiene compras en memoria y dispara notificaciones.
    /// </summary>
    public class PurchaseService : IPurchaseService
    {
        private readonly List<Compra> _compras = new();
        private readonly INotificationService _notificationService;

        public PurchaseService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public Task<IEnumerable<Compra>> GetComprasAsync()
        {
            return Task.FromResult(_compras.AsEnumerable());
        }

        public async Task<Compra> AddCompraAsync(Compra compra, Usuario usuario)
        {
            // Validaciones básicas: prima >= 15% del precio
            var minimoPrima = compra.PrecioTotal * 0.15m;
            if (compra.PrimaInicial < minimoPrima)
                throw new InvalidOperationException($"La prima inicial debe ser al menos {minimoPrima:C}.");

            if (compra.PlazoMeses <= 0 || compra.PlazoMeses > 60)
                throw new InvalidOperationException("El plazo debe estar entre 1 y 60 meses.");

            _compras.Add(compra);

            // Intentar enviar notificación; no bloquear el flujo si falla.
            try
            {
                await _notificationService.EnviarNotificacionRegistroCompraAsync(compra, usuario);
            }
            catch
            {
                // Loggear en implementación real.
            }

            return compra;
        }

        public async Task<bool> AddAbonoAsync(Guid compraId, Abono abono, Usuario usuario)
        {
            var compra = _compras.FirstOrDefault(c => c.CompraId == compraId);
            if (compra == null)
                throw new KeyNotFoundException("Compra no encontrada.");

            compra.Abonos.Add(abono);

            try
            {
                await _notificationService.EnviarNotificacionAbonoAsync(compra, abono, usuario);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
