using Newtonsoft.Json;
using System;

namespace HarleyStore.Models
{
    public class Cuota
    {
        [JsonProperty("id_cuota")]
        public long IdCuota { get; set; }

        [JsonProperty("id_oferta")]
        public long IdOferta { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("monto")]
        public float Monto { get; set; }

        [JsonProperty("fecha_vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [JsonProperty("pago_confirmado")]
        public bool? Aceptada { get; set; }
    }
}