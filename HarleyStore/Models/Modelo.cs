using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Modelo
    {
        [JsonProperty("id_modelo")]
        public long IdModelo { get; set; }

        [JsonProperty("nombre_modelo")]
        public string NombreModelo { get; set; } = string.Empty;

        [JsonProperty("anio")]
        public int Anio { get; set; }

        [JsonProperty("id_marca")]
        public long IdMarca { get; set; }

        [JsonProperty("id_motor")]
        public long IdMotor { get; set; }
    }
}
