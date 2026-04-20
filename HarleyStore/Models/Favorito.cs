using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Favorito
    {
        /// <summary>
        /// Relación simple entre usuario y moto que indica interés (favorito).
        /// </summary>
        [JsonProperty("id_usuario")]
        public long IdUsuario { get; set; }

        [JsonProperty("id_moto")]
        public long IdMoto { get; set; }

        [JsonProperty("fecha_agregado")]
        public DateTime FechaAgregado { get; set; }
    }
}
