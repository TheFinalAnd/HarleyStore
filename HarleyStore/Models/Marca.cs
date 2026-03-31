using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Marca
    {
        [JsonProperty("id_marca")]
        public long IdMarca { get; set; }

        [JsonProperty("nombre_marca")]
        public string NombreMarca { get; set; } = string.Empty;
    }
}
