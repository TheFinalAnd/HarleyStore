using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using HarleyStore.Models;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace HarleyStore.Services
{
    /// <summary>
    /// Servicio responsable del envío de notificaciones por correo.
    /// Implementa la interfaz INotificationService.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly List<Notificacion> _notificaciones = new();
        private const string NotifStorageKey = "harleystore_notificaciones";

        public NotificationService()
        {
            try
            {
                var json = Microsoft.Maui.Storage.Preferences.Get(NotifStorageKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Notificacion>>(json);
                    if (items != null)
                        _notificaciones.AddRange(items);
                }
            }
            catch
            {
            }
        }

        public async Task<bool> EnviarNotificacionRegistroCompraAsync(Compra compra, Usuario usuario)
        {
            try
            {
                var subject = "Nueva compra registrada";
                var body = $"Se ha registrado una nueva compra: {compra.TipoBien} - {compra.Marca} por {compra.PrecioTotal:C}.";

                var ok = true;
                try
                {
                    await SendEmailAsync(usuario.Correo, subject, body);
                }
                catch
                {
                    ok = false;
                }

                var noti = new Notificacion
                {
                    IdUsuario = usuario.IdUsuario,
                    Para = usuario.Correo,
                    Asunto = subject,
                    Mensaje = body,
                    Enviada = ok
                };

                _notificaciones.Insert(0, noti);
                PersistNotificaciones();

                try
                {
                    var supa = ServiceHelper.Services?.GetService(typeof(SupabaseService)) as SupabaseService;
                    if (supa != null)
                    {
                        await supa.CrearNotificacionAsync(noti);
                    }
                }
                catch { }

                return ok;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnviarNotificacionTextoAsync(string to, string subject, string message)
        {
            var ok = true;

            try
            {
                await SendEmailAsync(to, subject, message);
            }
            catch
            {
                ok = false;
            }

            var noti = new Notificacion
            {
                Para = to,
                Asunto = subject,
                Mensaje = message,
                Enviada = ok
            };

            _notificaciones.Insert(0, noti);
            PersistNotificaciones();

            return ok;
        }

        public async Task<bool> EnviarNotificacionAbonoAsync(Compra compra, Abono abono, Usuario usuario)
        {
            try
            {
                var subject = "Abono registrado";
                var body = $"Se ha registrado un abono de {abono.Monto:C} para la compra {compra.TipoBien} ({compra.CompraId}).";

                var ok = true;
                try
                {
                    await SendEmailAsync(usuario.Correo, subject, body);
                }
                catch
                {
                    ok = false;
                }

                var noti = new Notificacion
                {
                    IdUsuario = usuario.IdUsuario,
                    Para = usuario.Correo,
                    Asunto = subject,
                    Mensaje = body,
                    Enviada = ok
                };

                _notificaciones.Insert(0, noti);
                PersistNotificaciones();

                return ok;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnviarNotificacionOfertaEnviadaAsync(Oferta oferta, Usuario propietario, Usuario proponente)
        {
            try
            {
                var subjectOwner = "Nueva oferta recibida";
                var bodyOwner = $"Has recibido una nueva oferta de {proponente.Nombre} por {oferta.PrecioOfertado:C} en la moto {oferta.IdMoto}.";

                var okOwner = true;
                try
                {
                    await SendEmailAsync(propietario.Correo, subjectOwner, bodyOwner);
                }
                catch
                {
                    okOwner = false;
                }

                var subjectProponent = "Oferta enviada";
                var bodyProp = $"Tu oferta de {oferta.PrecioOfertado:C} fue enviada correctamente para la moto {oferta.IdMoto}.";

                var okProp = true;
                try
                {
                    await SendEmailAsync(proponente.Correo, subjectProponent, bodyProp);
                }
                catch
                {
                    okProp = false;
                }

                _notificaciones.Insert(0, new Notificacion
                {
                    IdUsuario = propietario.IdUsuario,
                    Para = propietario.Correo,
                    Asunto = subjectOwner,
                    Mensaje = bodyOwner,
                    Enviada = okOwner
                });

                _notificaciones.Insert(0, new Notificacion
                {
                    IdUsuario = proponente.IdUsuario,
                    Para = proponente.Correo,
                    Asunto = subjectProponent,
                    Mensaje = bodyProp,
                    Enviada = okProp
                });

                PersistNotificaciones();

                try
                {
                    var supa = ServiceHelper.Services?.GetService(typeof(SupabaseService)) as SupabaseService;
                    if (supa != null)
                    {
                        await supa.CrearNotificacionAsync(new Notificacion
                        {
                            IdUsuario = propietario.IdUsuario,
                            Para = propietario.Correo,
                            Asunto = subjectOwner,
                            Mensaje = bodyOwner,
                            Enviada = okOwner
                        });

                        await supa.CrearNotificacionAsync(new Notificacion
                        {
                            IdUsuario = proponente.IdUsuario,
                            Para = proponente.Correo,
                            Asunto = subjectProponent,
                            Mensaje = bodyProp,
                            Enviada = okProp
                        });
                    }
                }
                catch { }

                return okOwner || okProp;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnviarNotificacionOfertaAceptadaAsync(Oferta oferta, Usuario proponente)
        {
            try
            {
                var subject = "Oferta aceptada";
                var body = $"Tu oferta de {oferta.PrecioOfertado:C} para la moto {oferta.IdMoto} fue aceptada.";

                var ok = true;
                try
                {
                    await SendEmailAsync(proponente.Correo, subject, body);
                }
                catch
                {
                    ok = false;
                }

                var noti = new Notificacion
                {
                    IdUsuario = proponente.IdUsuario,
                    Para = proponente.Correo,
                    Asunto = subject,
                    Mensaje = body,
                    Enviada = ok
                };

                _notificaciones.Insert(0, noti);
                PersistNotificaciones();

                try
                {
                    var supa = ServiceHelper.Services?.GetService(typeof(SupabaseService)) as SupabaseService;
                    if (supa != null)
                    {
                        await supa.CrearNotificacionAsync(noti);
                    }
                }
                catch { }

                return ok;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnviarNotificacionCuotaAsync(Oferta oferta, Cuota cuota, Usuario destinatario)
        {
            try
            {
                var subject = "Nuevo abono registrado";
                var body = $"Se registró un abono de {cuota.Monto * 1000f:C} para la oferta {oferta.IdOferta} (moto {oferta.IdMoto}).";

                var ok = true;
                try
                {
                    await SendEmailAsync(destinatario.Correo, subject, body);
                }
                catch
                {
                    ok = false;
                }

                var noti = new Notificacion
                {
                    IdUsuario = destinatario.IdUsuario,
                    Para = destinatario.Correo,
                    Asunto = subject,
                    Mensaje = body,
                    Enviada = ok
                };

                _notificaciones.Insert(0, noti);
                PersistNotificaciones();

                try
                {
                    var supa = ServiceHelper.Services?.GetService(typeof(SupabaseService)) as SupabaseService;
                    if (supa != null)
                    {
                        await supa.CrearNotificacionAsync(noti);
                    }
                }
                catch { }

                return ok;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> EnviarNotificacionPagoConfirmadoAsync(Oferta oferta, Cuota cuota, Usuario pagador)
        {
            try
            {
                var subject = "Pago confirmado";
                var body = $"Tu pago de {cuota.Monto * 1000f:C} para la oferta {oferta.IdOferta} fue confirmado por el dueño de la moto.";

                var ok = true;
                try
                {
                    await SendEmailAsync(pagador.Correo, subject, body);
                }
                catch
                {
                    ok = false;
                }

                var noti = new Notificacion
                {
                    IdUsuario = pagador.IdUsuario,
                    Para = pagador.Correo,
                    Asunto = subject,
                    Mensaje = body,
                    Enviada = ok
                };

                _notificaciones.Insert(0, noti);
                PersistNotificaciones();

                try
                {
                    var supa = ServiceHelper.Services?.GetService(typeof(SupabaseService)) as SupabaseService;
                    if (supa != null)
                    {
                        await supa.CrearNotificacionAsync(noti);
                    }
                }
                catch { }

                return ok;
            }
            catch
            {
                return false;
            }
        }

        public Task<IEnumerable<Notificacion>> GetNotificacionesAsync()
        {
            return Task.FromResult(_notificaciones.AsEnumerable());
        }

        private void PersistNotificaciones()
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_notificaciones);
                Microsoft.Maui.Storage.Preferences.Set(NotifStorageKey, json);
            }
            catch
            {
            }
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            var recipient = AppConfig.TestEmailOverride ?? to;

            if (!string.IsNullOrWhiteSpace(AppConfig.SmtpHost) &&
                !string.IsNullOrWhiteSpace(AppConfig.SmtpFrom))
            {
                using var client = new SmtpClient(AppConfig.SmtpHost, AppConfig.SmtpPort)
                {
                    EnableSsl = AppConfig.SmtpPort == 465 || AppConfig.SmtpPort == 587,
                };

                if (!string.IsNullOrWhiteSpace(AppConfig.SmtpUser))
                {
                    client.Credentials = new NetworkCredential(AppConfig.SmtpUser, AppConfig.SmtpPassword);
                }

                var mail = new MailMessage(AppConfig.SmtpFrom, recipient, subject, body);
                await client.SendMailAsync(mail);
                return;
            }

            var apiKey = AppConfig.ResendApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("No SMTP and no Resend API key configured.");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            http.DefaultRequestHeaders.Add("Accept", "application/json");

            var payload = new
            {
                from = AppConfig.SmtpFrom ?? "no-reply@harleystore.test",
                to = new[] { recipient },
                subject,
                html = body
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await http.PostAsync("https://api.resend.com/emails", content);

            if (!resp.IsSuccessStatusCode)
            {
                var text = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Resend error: {(int)resp.StatusCode} {text}");
            }
        }
    }
}