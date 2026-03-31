using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Oferta
    {
        [JsonProperty("id_moto")]
        public long IdMoto { get; set; }

        [JsonProperty("id_usuario")]
        public long IdUsuario { get; set; }

        [JsonProperty("precio_ofertado")]
        public float PrecioOfertado { get; set; }

        [JsonProperty("solicita_cuotas")]
        public bool SolicitaCuotas { get; set; }

        [JsonProperty("cantidad_cuotas")]
        public short? CantidadCuotas { get; set; }

        [JsonProperty("prima")]
        public short? Prima { get; set; }

        [JsonProperty("interes")]
        public short? Interes { get; set; }

        [JsonProperty("it_estado")]
        public long ItEstado { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }
    }
}
