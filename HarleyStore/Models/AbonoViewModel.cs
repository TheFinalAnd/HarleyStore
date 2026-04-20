using Microsoft.Maui.Graphics;

namespace HarleyStore.Models
{
    public class AbonoViewModel
    {
        public long IdCuota { get; set; }
        public long IdOferta { get; set; }
        public float Monto { get; set; }
        public DateTime Date { get; set; }

        public string? MotoPreview { get; set; }
        public string MotoNombre { get; set; } = "-";

        public string EstadoTexto { get; set; } = "Pendiente";
        public Color EstadoColor { get; set; } = Colors.Orange;
    }
}