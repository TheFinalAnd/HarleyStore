using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HarleyStore.Services
{
    public class CryptoService
    {
        /// <summary>
        /// Calcula el hash SHA-256 de una cadena y devuelve su representación hexadecimal en minúsculas.
        /// </summary>
        /// <remarks>
        /// - Uso previsto: almacenamiento/validación de contraseñas en la API externa.
        /// - Considerar usar PBKDF2/Argon2/scrypt con salt por seguridad en contraseñas
        ///   si la aplicación llegara a gestionar contraseñas en producción.
        /// </remarks>
        /// <param name="texto">Texto de entrada.</param>
        /// <returns>Hex string del hash SHA-256.</returns>
        public string ToSha256(string texto)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(texto);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder();

            foreach (var b in hash)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}
