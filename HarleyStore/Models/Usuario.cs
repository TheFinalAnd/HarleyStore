using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Models
{
    public class Usuario
    {
        /// <summary>
        /// Representa la entidad Usuario tal y como se recibe/manda a la API.
        /// </summary>
        /// <remarks>
        /// - Campos serializados con <see cref="JsonProperty"/> para mantener
        ///   compatibilidad con el esquema REST.
        /// - La contraseña se maneja como hash en la app cliente; no procesar
        ///   contraseñas en claro en memoria más tiempo del necesario.
        /// </remarks>
        [JsonProperty("id_usuario")]
        public long IdUsuario { get; set; }

        [JsonProperty("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [JsonProperty("correo")]
        public string Correo { get; set; } = string.Empty;

        [JsonProperty("contrasena")]
        public string Contrasena { get; set; } = string.Empty;

        [JsonProperty("telefono")]
        public string Telefono { get; set; } = string.Empty;

        [JsonProperty("es_admin")]
        public bool EsAdmin { get; set; }

        [JsonProperty("fecha_registro")]
        public DateTime FechaRegistro { get; set; }
    }
}
