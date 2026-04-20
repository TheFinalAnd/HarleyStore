using System;

namespace HarleyStore.Services
{
    public static class FinanceHelper
    {
        /// <summary>
        /// Calcula la cuota periódica usando la fórmula de anualidad (amortización)
        /// P = principal, r = interés por periodo en porcentaje, n = número de periodos
        /// Si r == 0 devuelve P / n
        /// </summary>
        public static float CalculateInstallment(float principal, float interestPercentPerPeriod, int periods)
        {
            if (periods <= 0) return 0f;
            if (principal <= 0f) return 0f;

            var r = interestPercentPerPeriod / 100.0;
            if (Math.Abs(r) < 1e-9)
            {
                return principal / periods;
            }

            // payment = P * r / (1 - (1+r)^-n)
            var denom = 1.0 - Math.Pow(1.0 + r, -periods);
            if (Math.Abs(denom) < 1e-12) return (float)(principal / periods);

            var payment = principal * r / denom;
            return (float)payment;
        }
    }
}
