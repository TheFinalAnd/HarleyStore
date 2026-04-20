using Microsoft.Maui.Graphics;

namespace HarleyStore.Models
{
    public class OfertaViewModel
    {
        public long IdOferta { get; set; }
        public long IdMoto { get; set; }
        public long IdUsuario { get; set; }
        public string UsuarioEmail { get; set; }

        public float PrecioOfertado { get; set; }
        public bool SolicitaCuotas { get; set; }

        public short? CantidadCuotas { get; set; }
        public float? Prima { get; set; }
        public float? Interes { get; set; }

        public short IdEstado { get; set; }
        public DateTime Fecha { get; set; }

        public float TotalPagado { get; set; }
        public float DeudaPendiente { get; set; }
        public float PrecioPorCuota { get; set; }

        public string? MotoPreview { get; set; }
        public string MotoNombre { get; set; } = "-";

        public float PrecioUnits { get; set; }
        public float PrimaUnits { get; set; }

        public string EstadoTexto { get; set; } = "Pendiente";
        public Color EstadoColor { get; set; } = Colors.Orange;
    }
}