using System;
using System.Collections.Generic;

namespace HarleyStore.Models
{
    /// <summary>
    /// Entidad que representa una compra financiada dentro de la app.
    /// </summary>
    public class Compra
    {
        // Identificador único local para la compra. Usamos Guid para facilidad
        // de generación en cliente y evitar colisiones en almacenamiento local.
        public Guid CompraId { get; set; } = Guid.NewGuid();

        // Tipo de bien (por ejemplo: "motocicleta", "vehículo", "electrodoméstico").
        public string TipoBien { get; set; } = string.Empty;

        // Marca o proveedor del bien.
        public string Marca { get; set; } = string.Empty;

        // Precio total del bien.
        public decimal PrecioTotal { get; set; }

        // Prima inicial (valor absoluto, no porcentaje) pagada por el cliente.
        public decimal PrimaInicial { get; set; }

        // Plazo en meses (requisito: hasta 60 meses).
        public int PlazoMeses { get; set; }

        // Tasa de interés anual en porcentaje (ej. 9.5 => 9.5%).
        public decimal TasaInteres { get; set; }

        // URL al logo del proveedor (opcional). Se puede usar para mostrar en UI.
        public string? LogoUrl { get; set; }

        // Fecha en la que se registró la compra en la app.
        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        // Lista de abonos/pagos aplicados a esta compra.
        public List<Abono> Abonos { get; set; } = new();
    }
}
