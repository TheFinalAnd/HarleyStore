using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Oferta
    {
        [JsonProperty("id_oferta")]
        public long IdOferta { get; set; }

        /// <summary>
        /// Representa una oferta realizada por un usuario sobre una moto.
        /// </summary>
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
        public float? Prima { get; set; }

        [JsonProperty("interes")]
        public float? Interes { get; set; }

        [JsonProperty("it_estado")]
        public long IdEstado { get; set; }

        [JsonProperty("fecha")]
        public DateTime Fecha { get; set; }
    }
}
