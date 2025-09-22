using Serilog;

namespace Dizgem.Helpers
{
    public static class LoggerHelper
    {
        /// <summary>
        /// Tüm exception'ları Serilog kullanarak loga kaydeder.
        /// </summary>
        /// <param name="ex">Loglanacak exception nesnesi.</param>
        public static void LogException(Exception ex)
        {
            // Serilog'un .Error metodu, exception nesnesini otomatik olarak işler
            // ve detaylı bir şekilde loga kaydeder.
            Log.Error(ex, "Uygulamada beklenmedik bir hata oluştu.");
        }
    }
}
