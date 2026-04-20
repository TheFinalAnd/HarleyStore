using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Motor
    {
        /// <summary>
        /// Datos técnicos del motor asociados a un modelo (puede ser nulo si no se expande).
        /// </summary>
        [JsonProperty("id_motor")]
        public long IdMotor { get; set; }

        [JsonProperty("tipo_motor")]
        public string TipoMotor { get; set; } = string.Empty;

        [JsonProperty("cc")]
        public int? Cc { get; set; }

        [JsonProperty("hp")]
        public int? Hp { get; set; }

        [JsonProperty("torque")]
        public float? Torque { get; set; }

        [JsonProperty("combustible")]
        public string Combustible { get; set; } = string.Empty;
    }
}
