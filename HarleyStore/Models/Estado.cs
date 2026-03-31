using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Estado
    {
        [JsonProperty("id_estado")]
        public long IdEstado { get; set; }

        [JsonProperty("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [JsonProperty("nombre_estado")]
        public string NombreEstado { get; set; } = string.Empty;
    }
}
