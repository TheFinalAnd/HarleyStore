using System;

namespace HarleyStore.Models
{
    public class Notificacion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Newtonsoft.Json.JsonProperty("id_usuario")]
        public long? IdUsuario { get; set; }

        [Newtonsoft.Json.JsonProperty("para")]
        public string Para { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public bool Enviada { get; set; }
    }
}
