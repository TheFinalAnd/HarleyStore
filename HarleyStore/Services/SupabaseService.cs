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
                BaseAddress = new Uri($"{AppConfig.SupabaseUrl}/rest/v1/")
            };

            _httpClient.DefaultRequestHeaders.Add("apikey", AppConfig.SupabaseAnonKey);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AppConfig.SupabaseAnonKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private async Task<HttpResponseMessage> SendAsyncWithLogging(HttpRequestMessage request)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Supabase] Request: {request.Method} {request.RequestUri}");

                if (request.Content != null)
                {
                    var reqBody = await request.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"[Supabase] Request Body: {reqBody}");
                }

                var response = await _httpClient.SendAsync(request);

                var respBody = response.Content != null
                    ? await response.Content.ReadAsStringAsync()
                    : string.Empty;

                System.Diagnostics.Debug.WriteLine(
                    $"[Supabase] Response: {(int)response.StatusCode} {response.ReasonPhrase}\n{respBody}");

                return response;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Supabase] SendAsyncWithLogging Exception: {ex.Message}");
                throw;
            }
        }

        private const string PendingOffersKey = "harleystore_pending_offers";

        public async Task SyncPendingOffersAsync()
        {
            try
            {
                var json = Microsoft.Maui.Storage.Preferences.Get(PendingOffersKey, string.Empty);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var pendientes = JsonConvert.DeserializeObject<List<Oferta>>(json) ?? new List<Oferta>();
                var enviados = new List<Oferta>();

                foreach (var o in pendientes)
                {
                    try
                    {
                        var ok = await CrearOfertaAsync(o);
                        if (ok)
                            enviados.Add(o);
                    }
                    catch { }
                }

                if (enviados.Any())
                {
                    var restantes = pendientes.Except(enviados).ToList();
                    var remJson = JsonConvert.SerializeObject(restantes);
                    Microsoft.Maui.Storage.Preferences.Set(PendingOffersKey, remJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Supabase] SyncPendingOffersAsync error: {ex.Message}");
            }
        }

        private async Task SavePendingOfferAsync(Oferta oferta)
        {
            try
            {
                var json = Microsoft.Maui.Storage.Preferences.Get(PendingOffersKey, string.Empty);
                var list = string.IsNullOrWhiteSpace(json)
                    ? new List<Oferta>()
                    : JsonConvert.DeserializeObject<List<Oferta>>(json) ?? new List<Oferta>();

                list.Add(oferta);
                var newJson = JsonConvert.SerializeObject(list);
                Microsoft.Maui.Storage.Preferences.Set(PendingOffersKey, newJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SavePendingOfferAsync error: {ex.Message}");
            }
        }

        public async Task<List<Marca>> GetMarcasAsync()
        {
            var json = await _httpClient.GetStringAsync("Marca?select=*&order=nombre_marca.asc");
            return JsonConvert.DeserializeObject<List<Marca>>(json) ?? new List<Marca>();
        }

        public async Task<List<Modelo>> GetModelosAsync()
        {
            var json = await _httpClient.GetStringAsync("Modelo?select=*&order=nombre_modelo.asc");
            return JsonConvert.DeserializeObject<List<Modelo>>(json) ?? new List<Modelo>();
        }

        public async Task<List<Usuario>> GetUsuariosAsync(string filtro = "")
        {
            var endpoint = "Usuario";
            if (!string.IsNullOrWhiteSpace(filtro))
                endpoint += filtro;

            var json = await _httpClient.GetStringAsync(endpoint);
            return JsonConvert.DeserializeObject<List<Usuario>>(json) ?? new List<Usuario>();
        }

        public async Task<Usuario?> GetUsuarioByIdAsync(long idUsuario)
        {
            var usuarios = await GetUsuariosAsync($"?id_usuario=eq.{idUsuario}&select=*");
            return usuarios.FirstOrDefault();
        }

        public async Task<Usuario?> LoginAsync(string correo, string contrasenaHash)
        {
            var result = await GetUsuariosAsync(
                $"?correo=eq.{Uri.EscapeDataString(correo)}&contrasena=eq.{Uri.EscapeDataString(contrasenaHash)}&select=*");
            return result.FirstOrDefault();
        }

        public async Task<bool> ExisteCorreoAsync(string correo)
        {
            var result = await GetUsuariosAsync(
                $"?correo=eq.{Uri.EscapeDataString(correo)}&select=id_usuario");
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
                    new AuthenticationHeaderValue("Bearer", AppConfig.SupabaseAnonKey);

                var content = new ByteArrayContent(bytes);
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                var response = await client.PostAsync(endpoint, content);
                var respuestaTexto = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Error subiendo foto",
                        $"Status: {(int)response.StatusCode}\n{respuestaTexto}",
                        "OK");
                    return null;
                }

                return $"{AppConfig.SupabaseUrl}/storage/v1/object/public/{AppConfig.BucketMotos}/{nombreArchivo}";
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
                return null;
            }
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
                    new AuthenticationHeaderValue("Bearer", AppConfig.SupabaseAnonKey);

                var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"{AppConfig.SupabaseUrl}/storage/v1/object/{AppConfig.BucketMotos}/{fileName}");

                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Moto>> GetMotosAsync()
        {
            var endpoint =
                "Moto?select=id_moto,id_usuario,id_modelo,precio_publicado,descripcion,millas,id_estado,fecha_publicacion,foto_url,min_prima,min_interes,Modelo:Modelo(id_modelo,nombre_modelo,anio,Marca:Marca(id_marca,nombre_marca),Motor:Motor(id_motor,tipo_motor,cc,hp,torque,combustible))";

            var json = await _httpClient.GetStringAsync(endpoint);
            return JsonConvert.DeserializeObject<List<Moto>>(json) ?? new List<Moto>();
        }

        public async Task<List<Moto>> GetMotosByUsuarioAsync(long idUsuario)
        {
            var endpoint =
                $"Moto?id_usuario=eq.{idUsuario}&select=id_moto,id_usuario,id_modelo,precio_publicado,descripcion,millas,id_estado,fecha_publicacion,foto_url,min_prima,min_interes,Modelo:Modelo(id_modelo,nombre_modelo,anio,Marca:Marca(id_marca,nombre_marca),Motor:Motor(id_motor,tipo_motor,cc,hp,torque,combustible))&order=fecha_publicacion.desc";

            var json = await _httpClient.GetStringAsync(endpoint);
            return JsonConvert.DeserializeObject<List<Moto>>(json) ?? new List<Moto>();
        }

        public async Task<List<Moto>> GetMotosDisponiblesAsync()
        {
            var motos = await GetMotosAsync();
            return motos
                .Where(m => m.IdEstado == 1)
                .OrderByDescending(m => m.FechaPublicacion)
                .ToList();
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
                foto_url = moto.FotoUrl,
                min_prima = moto.MinPrima,
                min_interes = moto.MinInteres
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

        public async Task<bool> ActualizarMotoAsync(Moto moto)
        {
            var payload = new
            {
                id_modelo = moto.IdModelo,
                precio_publicado = moto.PrecioPublicado,
                descripcion = moto.Descripcion,
                millas = moto.Millas,
                min_prima = moto.MinPrima,
                min_interes = moto.MinInteres,
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

        public async Task<long?> GetEstadoMotoDisponibleIdAsync()
        {
            var json = await _httpClient.GetStringAsync("Estado?tipo=eq.moto&nombre_estado=eq.Disponible&select=*");
            var estados = JsonConvert.DeserializeObject<List<Estado>>(json) ?? new List<Estado>();
            return estados.FirstOrDefault()?.IdEstado;
        }

        public async Task<bool> CrearOfertaAsync(Oferta oferta)
        {
            try
            {
                var payload = new
                {
                    id_moto = oferta.IdMoto,
                    id_usuario = oferta.IdUsuario,
                    precio_ofertado = oferta.PrecioOfertado,
                    solicita_cuotas = oferta.SolicitaCuotas,
                    cantidad_cuotas = oferta.CantidadCuotas,
                    prima = oferta.Prima,
                    interes = oferta.Interes,
                    it_estado = oferta.IdEstado,
                    fecha = oferta.Fecha.ToString("yyyy-MM-dd")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "Oferta")
                {
                    Content = content
                };
                request.Headers.Add("Prefer", "return=minimal");

                var response = await SendAsyncWithLogging(request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"CrearOfertaAsync error: {(int)response.StatusCode} - {body}");

                    var arrayJson = JsonConvert.SerializeObject(new[] { payload });
                    var arrayContent = new StringContent(arrayJson, Encoding.UTF8, "application/json");
                    var resp2Request = new HttpRequestMessage(HttpMethod.Post, "Oferta")
                    {
                        Content = arrayContent
                    };

                    var resp2 = await SendAsyncWithLogging(resp2Request);

                    if (!resp2.IsSuccessStatusCode)
                    {
                        var body2 = await resp2.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"CrearOfertaAsync retry error: {(int)resp2.StatusCode} - {body2}");

                        try
                        {
                            await Application.Current!.MainPage!.DisplayAlert(
                                "Error creando oferta",
                                $"Status: {(int)resp2.StatusCode}\n{body2}",
                                "OK");
                        }
                        catch { }

                        await SavePendingOfferAsync(oferta);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    await Application.Current!.MainPage!.DisplayAlert("Error", ex.Message, "OK");
                }
                catch { }

                return false;
            }
        }

        public async Task<Oferta?> GetOfertaByMotoAndUsuarioAsync(long idMoto, long idUsuario)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(
                    $"Oferta?id_moto=eq.{idMoto}&id_usuario=eq.{idUsuario}&select=*");

                var list = JsonConvert.DeserializeObject<List<Oferta>>(json) ?? new List<Oferta>();
                return list.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public async Task<Oferta?> GetOfertaByIdAsync(long idOferta)
        {
            try
            {
                var json = await _httpClient.GetStringAsync($"Oferta?id_oferta=eq.{idOferta}&select=*");
                var list = JsonConvert.DeserializeObject<List<Oferta>>(json) ?? new List<Oferta>();
                return list.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Oferta>> GetOfertasByMotoAsync(long idMoto)
        {
            var json = await _httpClient.GetStringAsync($"Oferta?id_moto=eq.{idMoto}&select=*&order=fecha.desc");
            return JsonConvert.DeserializeObject<List<Oferta>>(json) ?? new List<Oferta>();
        }

        public async Task<List<Oferta>> GetOfertasByUsuarioAsync(long idUsuario)
        {
            var json = await _httpClient.GetStringAsync($"Oferta?id_usuario=eq.{idUsuario}&select=*&order=fecha.desc");
            return JsonConvert.DeserializeObject<List<Oferta>>(json) ?? new List<Oferta>();
        }

        public async Task<List<Oferta>> GetOfertasRelacionadasConUsuarioAsync(long idUsuario)
        {
            var resultado = new List<Oferta>();

            var hechas = await GetOfertasByUsuarioAsync(idUsuario);
            resultado.AddRange(hechas);

            var misMotos = await GetMotosByUsuarioAsync(idUsuario);
            foreach (var moto in misMotos)
            {
                var ofertasMoto = await GetOfertasByMotoAsync(moto.IdMoto);
                resultado.AddRange(ofertasMoto);
            }

            return resultado
                .GroupBy(o => o.IdOferta)
                .Select(g => g.First())
                .OrderByDescending(o => o.Fecha)
                .ToList();
        }

        public async Task<List<Oferta>> GetOfertasAceptadasByUsuarioAsync(long idUsuario)
        {
            var ofertas = await GetOfertasByUsuarioAsync(idUsuario);
            return ofertas
                .Where(o => o.IdEstado == 5)
                .OrderByDescending(o => o.Fecha)
                .ToList();
        }

        public async Task<bool> ActualizarOfertaAsync(long idMoto, long idUsuario, float nuevoPrecio)
        {
            var existente = await GetOfertaByMotoAndUsuarioAsync(idMoto, idUsuario);
            if (existente != null && existente.IdEstado == 5)
            {
                try
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Operación no permitida",
                        "No se puede editar una oferta que ya fue aceptada.",
                        "OK");
                }
                catch { }

                return false;
            }

            var payload = new
            {
                precio_ofertado = nuevoPrecio
            };

            var json = JsonConvert.SerializeObject(payload);
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Oferta?id_moto=eq.{idMoto}&id_usuario=eq.{idUsuario}")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Prefer", "return=minimal");
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ActualizarOfertaCompletaAsync(Oferta oferta)
        {
            try
            {
                var payload = new
                {
                    precio_ofertado = oferta.PrecioOfertado,
                    solicita_cuotas = oferta.SolicitaCuotas,
                    cantidad_cuotas = oferta.CantidadCuotas,
                    prima = oferta.Prima,
                    interes = oferta.Interes,
                    it_estado = oferta.IdEstado
                };

                var json = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Oferta?it_oferta=eq.{oferta.IdOferta}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Prefer", "return=minimal");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarEstadoOfertaAsync(long idOferta, long nuevoEstado)
        {
            try
            {
                var payload = new
                {
                    it_estado = nuevoEstado
                };

                var json = JsonConvert.SerializeObject(payload);

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Oferta?id_oferta=eq.{idOferta}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Prefer", "return=representation");

                var response = await SendAsyncWithLogging(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EliminarOfertaAsync(long idMoto, long idUsuario)
        {
            var existente = await GetOfertaByMotoAndUsuarioAsync(idMoto, idUsuario);
            if (existente != null && existente.IdEstado == 5)
            {
                try
                {
                    await Application.Current!.MainPage!.DisplayAlert(
                        "Operación no permitida",
                        "No se puede eliminar una oferta que ya fue aceptada.",
                        "OK");
                }
                catch { }

                return false;
            }

            var response = await _httpClient.DeleteAsync($"Oferta?id_moto=eq.{idMoto}&id_usuario=eq.{idUsuario}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CrearCuotaAsync(Cuota cuota)
        {
            try
            {
                var oferta = await GetOfertaByIdAsync(cuota.IdOferta);
                if (oferta == null || oferta.IdEstado != 5)
                {
                    try
                    {
                        await Application.Current!.MainPage!.DisplayAlert(
                            "Operación no permitida",
                            "Solo se pueden registrar cuotas para ofertas aceptadas.",
                            "OK");
                    }
                    catch { }

                    return false;
                }

                var payload = new
                {
                    id_oferta = cuota.IdOferta,
                    date = cuota.Date.ToString("yyyy-MM-dd"),
                    monto = cuota.Monto,
                    fecha_vencimiento = cuota.FechaVencimiento.ToString("yyyy-MM-dd"),
                    pago_confirmado = cuota.Aceptada
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "Cuotas")
                {
                    Content = content
                };
                request.Headers.Add("Prefer", "return=minimal");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"CrearCuotaAsync error: {(int)response.StatusCode} - {body}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrearCuotaAsync exception: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Cuota>> GetCuotasByOfertaAsync(long idOferta)
        {
            var json = await _httpClient.GetStringAsync($"Cuotas?id_oferta=eq.{idOferta}&select=*&order=date.desc");
            return JsonConvert.DeserializeObject<List<Cuota>>(json) ?? new List<Cuota>();
        }

        public async Task<bool> UpdateCuotaAceptadaAsync(long idCuota, bool aceptada)
        {
            try
            {
                var payload = new
                {
                    pago_confirmado = aceptada
                };

                var json = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"Cuotas?id_cuota=eq.{idCuota}")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };

                request.Headers.Add("Prefer", "return=minimal");
                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
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

        public async Task<bool> CrearNotificacionAsync(Notificacion notificacion)
        {
            try
            {
                var payload = new
                {
                    id_usuario = notificacion.IdUsuario,
                    para = notificacion.Para,
                    asunto = notificacion.Asunto,
                    mensaje = notificacion.Mensaje,
                    fecha = notificacion.Fecha.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "Notificacion")
                {
                    Content = content
                };
                request.Headers.Add("Prefer", "return=minimal");

                var response = await SendAsyncWithLogging(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CrearNotificacionAsync exception: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Notificacion>> GetNotificacionesByUsuarioAsync(long idUsuario)
        {
            var json = await _httpClient.GetStringAsync($"Notificacion?id_usuario=eq.{idUsuario}&select=*&order=fecha.desc");
            return JsonConvert.DeserializeObject<List<Notificacion>>(json) ?? new List<Notificacion>();
        }
    }
}