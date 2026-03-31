using System.Net.Http.Headers;
using System.Text;
using HarleyStore.Models;
using Newtonsoft.Json;

namespace HarleyStore.Services
{
    public class SupabaseService
    {
        private readonly HttpClient _httpClient;

        public SupabaseService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://hrkgntfvmixmqasqdkkd.supabase.co/rest/v1/")
            };

            _httpClient.DefaultRequestHeaders.Add("apikey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imhya2dudGZ2bWl4bXFhc3Fka2tkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI0OTI0MzksImV4cCI6MjA4ODA2ODQzOX0.m0jsyJQvTD5ngwdvqc4A2d2MQ4lUDxgQSWVcP_PxJio");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imhya2dudGZ2bWl4bXFhc3Fka2tkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI0OTI0MzksImV4cCI6MjA4ODA2ODQzOX0.m0jsyJQvTD5ngwdvqc4A2d2MQ4lUDxgQSWVcP_PxJio");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<Marca>> GetMarcasAsync()
        {
            var json = await _httpClient.GetStringAsync("Marca?select=*&order=nombre_marca.asc");
            return JsonConvert.DeserializeObject<List<Marca>>(json) ?? new List<Marca>();
        }


        public async Task<List<Usuario>> GetUsuariosAsync(string filtro = "")
        {
            var endpoint = "Usuario";
            if (!string.IsNullOrWhiteSpace(filtro))
                endpoint += filtro;

            var json = await _httpClient.GetStringAsync(endpoint);
            return JsonConvert.DeserializeObject<List<Usuario>>(json) ?? new List<Usuario>();
        }

        public async Task<Usuario?> LoginAsync(string correo, string contrasenaHash)
        {
            var result = await GetUsuariosAsync($"?correo=eq.{Uri.EscapeDataString(correo)}&contrasena=eq.{Uri.EscapeDataString(contrasenaHash)}&select=*");
            return result.FirstOrDefault();
        }

        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            var result = await GetUsuariosAsync($"?correo=eq.{Uri.EscapeDataString(correo)}&select=id_usuario");
            return result.Any();
        }

        public async Task<bool> CrearUsuarioAsync(Usuario usuario)
        {
            var payload = new
            {
                nombre = usuario.Nombre,
                correo = usuario.Correo,
                contrasena = usuario.Contrasena,
                telefono = usuario.Telefono,
                es_admin = usuario.EsAdmin,
                fecha_registro = usuario.FechaRegistro.ToString("yyyy-MM-dd")
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "Usuario")
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<string?> SubirFotoMotoAsync(FileResult foto)
        {
            try
            {
                using var stream = await foto.OpenReadAsync();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                var bytes = memory.ToArray();
                var nombreArchivo = $"{Guid.NewGuid()}_{foto.FileName}";
                var endpoint = $"{AppConfig.SupabaseUrl}/storage/v1/object/{AppConfig.BucketMotos}/{nombreArchivo}";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", AppConfig.SupabaseAnonKey);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppConfig.SupabaseAnonKey);

                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                var response = await client.PostAsync(endpoint, content);
                var respuestaTexto = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await Application.Current!.MainPage!.DisplayAlertAsync(
                        "Error subiendo foto",
                        $"Status: {(int)response.StatusCode}\n{respuestaTexto}",
                        "OK");
                    return null;
                }

                return $"{AppConfig.SupabaseUrl}/storage/v1/object/public/{AppConfig.BucketMotos}/{nombreArchivo}";
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlertAsync("Error", ex.Message, "OK");
                return null;
            }
        }

        public async Task<List<Moto>> GetMotosByUsuarioAsync(long idUsuario)
        {
            var endpoint = $"Moto?id_usuario=eq.{idUsuario}&select=id_moto,id_usuario,id_modelo,precio_publicado,descripcion,millas,id_estado,fecha_publicacion,foto_url,Modelo:Modelo(id_modelo,nombre_modelo,anio,Marca:Marca(id_marca,nombre_marca),Motor:Motor(id_motor,tipo_motor,cc,hp,torque,combustible))&order=fecha_publicacion.desc";
            var json = await _httpClient.GetStringAsync(endpoint);
            return JsonConvert.DeserializeObject<List<Moto>>(json) ?? new List<Moto>();
        }

        public async Task<bool> ActualizarMotoAsync(Moto moto)
        {
            var payload = new
            {
                id_modelo = moto.IdModelo,
                precio_publicado = moto.PrecioPublicado,
                descripcion = moto.Descripcion,
                millas = moto.Millas,
                id_estado = moto.IdEstado,
                foto_url = moto.FotoUrl
            };

            var json = JsonConvert.SerializeObject(payload);

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Moto?id_moto=eq.{moto.IdMoto}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Prefer", "return=minimal");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EliminarFotoMotoAsync(string? fotoUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fotoUrl))
                    return true;

                var fileName = fotoUrl.Split('/').Last();

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", AppConfig.SupabaseAnonKey);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", AppConfig.SupabaseAnonKey);

                var request = new HttpRequestMessage(HttpMethod.Delete,
                    $"{AppConfig.SupabaseUrl}/storage/v1/object/{AppConfig.BucketMotos}/{fileName}");

                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarMotoAsync(long idMoto)
        {
            var response = await _httpClient.DeleteAsync($"Moto?id_moto=eq.{idMoto}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EliminarMotoConFotoAsync(Moto moto)
        {
            var fotoOk = await EliminarFotoMotoAsync(moto.FotoUrl);
            var motoOk = await EliminarMotoAsync(moto.IdMoto);

            return fotoOk && motoOk;
        }

        public async Task<List<Moto>> GetMotosAsync()
        {
            var endpoint = "Moto?select=id_moto,id_usuario,id_modelo,precio_publicado,descripcion,millas,id_estado,fecha_publicacion,foto_url,Modelo:Modelo(id_modelo,nombre_modelo,anio,Marca:Marca(id_marca,nombre_marca),Motor:Motor(id_motor,tipo_motor,cc,hp,torque,combustible))";
            var json = await _httpClient.GetStringAsync(endpoint);
            return JsonConvert.DeserializeObject<List<Moto>>(json) ?? new List<Moto>();
        }

        public async Task<List<Modelo>> GetModelosAsync()
        {
            var json = await _httpClient.GetStringAsync("Modelo?select=*&order=nombre_modelo.asc");
            return JsonConvert.DeserializeObject<List<Modelo>>(json) ?? new List<Modelo>();
        }

        public async Task<long?> GetEstadoMotoDisponibleIdAsync()
        {
            var json = await _httpClient.GetStringAsync("Estado?tipo=eq.moto&nombre_estado=eq.Disponible&select=*");
            var estados = JsonConvert.DeserializeObject<List<Estado>>(json) ?? new List<Estado>();
            return estados.FirstOrDefault()?.IdEstado;
        }

        public async Task<bool> CambiarContrasenaAsync(long idUsuario, string nuevaContrasenaHash)
        {
            var payload = new
            {
                contrasena = nuevaContrasenaHash
            };

            var json = JsonConvert.SerializeObject(payload);

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Usuario?id_usuario=eq.{idUsuario}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Prefer", "return=minimal");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PublicarMotoAsync(Moto moto)
        {
            var payload = new
            {
                id_usuario = moto.IdUsuario,
                id_modelo = moto.IdModelo,
                precio_publicado = moto.PrecioPublicado,
                descripcion = moto.Descripcion,
                millas = moto.Millas,
                id_estado = moto.IdEstado,
                fecha_publicacion = moto.FechaPublicacion.ToString("yyyy-MM-dd"),
                foto_url = moto.FotoUrl
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "Moto")
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Favorito>> GetFavoritosAsync(long idUsuario)
        {
            var json = await _httpClient.GetStringAsync($"Favorito?id_usuario=eq.{idUsuario}&select=*");
            return JsonConvert.DeserializeObject<List<Favorito>>(json) ?? new List<Favorito>();
        }

        public async Task<bool> EsFavoritoAsync(long idUsuario, long idMoto)
        {
            var json = await _httpClient.GetStringAsync($"Favorito?id_usuario=eq.{idUsuario}&id_moto=eq.{idMoto}&select=*");
            var favoritos = JsonConvert.DeserializeObject<List<Favorito>>(json) ?? new List<Favorito>();
            return favoritos.Any();
        }

        public async Task<List<Moto>> BuscarMotosAsync(string texto)
        {
            var motos = await GetMotosAsync();

            texto = (texto ?? string.Empty).Trim().ToLower();

            return motos.Where(m =>
                ((m.Descripcion ?? string.Empty).ToLower().Contains(texto)) ||
                ((m.Modelo?.NombreModelo ?? string.Empty).ToLower().Contains(texto)) ||
                ((m.Modelo?.Marca?.NombreMarca ?? string.Empty).ToLower().Contains(texto))
            ).ToList();
        }

        public async Task<bool> AgregarFavoritoAsync(Favorito favorito)
        {
            var payload = new
            {
                id_usuario = favorito.IdUsuario,
                id_moto = favorito.IdMoto,
                fecha_agregado = favorito.FechaAgregado.ToString("yyyy-MM-dd")
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "Favorito")
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=minimal");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> QuitarFavoritoAsync(long idUsuario, long idMoto)
        {
            var response = await _httpClient.DeleteAsync($"Favorito?id_usuario=eq.{idUsuario}&id_moto=eq.{idMoto}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarUsuarioAsync(Usuario usuario)
        {
            var payload = new
            {
                nombre = usuario.Nombre,
                correo = usuario.Correo,
                telefono = usuario.Telefono,
                es_admin = usuario.EsAdmin,
                fecha_registro = usuario.FechaRegistro.ToString("yyyy-MM-dd")
            };

            var json = JsonConvert.SerializeObject(payload);

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Usuario?id_usuario=eq.{usuario.IdUsuario}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Prefer", "return=minimal");

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
    }
}