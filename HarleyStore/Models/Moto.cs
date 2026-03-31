using Newtonsoft.Json;

namespace HarleyStore.Models
{
    public class Moto
    {
        [JsonProperty("id_moto")]
        public long IdMoto { get; set; }

        [JsonProperty("id_usuario")]
        public long IdUsuario { get; set; }

        [JsonProperty("id_modelo")]
        public long IdModelo { get; set; }

        [JsonProperty("precio_publicado")]
        public float PrecioPublicado { get; set; }

        [JsonProperty("descripcion")]
        public string Descripcion { get; set; } = string.Empty;

        [JsonProperty("millas")]
        public float Millas { get; set; }

        [JsonProperty("id_estado")]
        public long IdEstado { get; set; }

        [JsonProperty("fecha_publicacion")]
        public DateTime FechaPublicacion { get; set; }

        [JsonProperty("foto_url")]
        public string? FotoUrl { get; set; }

        [JsonProperty("Modelo")]
        public ModeloExpandido? Modelo { get; set; }

        [JsonIgnore]
        public string FotoMostrada
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FotoUrl))
                    return "welcome.jpg";

                return FotoUrl;
            }
        }
    }

    public class ModeloExpandido
    {
        [JsonProperty("id_modelo")]
        public long IdModelo { get; set; }

        [JsonProperty("nombre_modelo")]
        public string NombreModelo { get; set; } = string.Empty;

        [JsonProperty("anio")]
        public int Anio { get; set; }

        [JsonProperty("Marca")]
        public Marca? Marca { get; set; }

        [JsonProperty("Motor")]
        public Motor? Motor { get; set; }
    }
}