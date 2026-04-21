using System;
using System.Collections.Generic;
using System.Text;

namespace HarleyStore.Services
{
    public static class AppConfig
    {
        /// <summary>
        /// Valores de configuración de la integración con Supabase.
        /// </summary>
        /// <remarks>
        /// Ahora leemos la URL y la clave de variables de entorno para evitar
        /// incluir secretos en el código fuente. Exportar en el entorno de
        /// desarrollo las variables: SUPABASE_URL y SUPABASE_ANON_KEY.
        /// </remarks>
        public static string SupabaseUrl =>
            Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "https://hrkgntfvmixmqasqdkkd.supabase.co";

        public static string SupabaseAnonKey =>
            Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imhya2dudGZ2bWl4bXFhc3Fka2tkIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzI0OTI0MzksImV4cCI6MjA4ODA2ODQzOX0.m0jsyJQvTD5ngwdvqc4A2d2MQ4lUDxgQSWVcP_PxJio";

        public const string BucketMotos = "motos";

        // SMTP configuration for sending notification emails. Values are read from
        // environment variables to avoid committing secrets into source control.
        public static string SmtpHost => Environment.GetEnvironmentVariable("SMTP_HOST") ?? string.Empty;
        public static int SmtpPort => int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 25;
        public static string SmtpUser => Environment.GetEnvironmentVariable("SMTP_USER") ?? string.Empty;
        public static string SmtpPassword => Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? string.Empty;
        public static string SmtpFrom => Environment.GetEnvironmentVariable("SMTP_FROM") ?? string.Empty;

        
        public static string ResendApiKey => Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? "re_25Sj7Ga5_F4pmyzWwPG9Eqf9U4MMmhpu4";

        
        public static string TestEmailOverride => Environment.GetEnvironmentVariable("TEST_EMAIL_OVERRIDE") ?? "andresarceviquez@gmail.com";

        public static class ApiConfig
        {
            public const string ResendApiKey = "re_XAPo228r_Q1zrEjydqxM6SuSNkNA3Xarn";
        }
    }
}
