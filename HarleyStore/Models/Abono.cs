using System;

namespace HarleyStore.Models
{
    /// <summary>
    /// Representa un abono/pago aplicado a una compra financiada.
    /// </summary>
    public class Abono
    {
        // Identificador único del abono
        public Guid AbonoId { get; set; } = Guid.NewGuid();

        // Fecha del abono
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        // Monto pagado en esta transacción
        public decimal Monto { get; set; }

        // Comentario opcional o referencia de la transacción
        public string? Referencia { get; set; }
    }
}
