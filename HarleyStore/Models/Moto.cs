using Newtonsoft.Json;

namespace HarleyStore.Models
{
    public class Moto
    {
        /// <summary>
        /// Representa una motocicleta publicada en la plataforma.
        /// </summary>
        /// <remarks>
        /// - Las propiedades están mapeadas a la representación JSON/REST que
        ///   expone la API (nombres en snake_case) mediante <see cref="JsonProperty"/>.
        /// - <see cref="Modelo"/> contiene datos expandidos (join) cargados por la API
        ///   para evitar llamadas adicionales desde la UI.
        /// - <see cref="FotoMostrada"/> expone una URL segura para mostrarse en la UI
        ///   devolviendo una imagen por defecto cuando no hay URL disponible.
        /// </remarks>
        [JsonProperty("id_moto")]
        public long IdMoto { get; set; }

        [JsonProperty("id_usuario")]
        public long IdUsuario { get; set; }

        [JsonProperty("id_modelo")]
        public long IdModelo { get; set; }

        [JsonProperty("precio_publicado")]
        public float PrecioPublicado { get; set; }

        public float PrecioMostrado => PrecioPublicado * 1000;

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

        [JsonProperty("min_prima")]
        public float? MinPrima { get; set; }

        [JsonProperty("min_interes")]
        public float? MinInteres { get; set; }

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