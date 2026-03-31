using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace HarleyStore.Services
{
    public class CryptoService
    {
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
