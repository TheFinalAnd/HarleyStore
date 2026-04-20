using System.Threading.Tasks;
using HarleyStore.Models;

namespace HarleyStore.Services
{
    /// <summary>
    /// Interfaz que define operaciones de notificación (envío de correo).
    /// </summary>
    public interface INotificationService
    {
        Task<bool> EnviarNotificacionRegistroCompraAsync(Compra compra, Usuario usuario);
        Task<bool> EnviarNotificacionAbonoAsync(Compra compra, Abono abono, Usuario usuario);
        Task<IEnumerable<Notificacion>> GetNotificacionesAsync();
        Task<bool> EnviarNotificacionTextoAsync(string to, string subject, string message);

        // Notificaciones relacionadas con ofertas y cuotas
        Task<bool> EnviarNotificacionOfertaEnviadaAsync(Oferta oferta, Usuario propietario, Usuario proponente);
        Task<bool> EnviarNotificacionOfertaAceptadaAsync(Oferta oferta, Usuario proponente);
        Task<bool> EnviarNotificacionCuotaAsync(Oferta oferta, Cuota cuota, Usuario destinatario);

        // Nuevo: confirmación de pago
        Task<bool> EnviarNotificacionPagoConfirmadoAsync(Oferta oferta, Cuota cuota, Usuario pagador);
    }
}