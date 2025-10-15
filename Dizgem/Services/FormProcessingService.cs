using Dizgem.Data;
using Dizgem.Models;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace Dizgem.Services
{
    public class FormProcessingService : IFormProcessingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ISettingsService _settingsService; // SMTP ayarları için

        public FormProcessingService(ApplicationDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<bool> ProcessFormAsync(FormHandler handler, IFormCollection formData)
        {
            switch (handler.ActionType)
            {
                case FormActionType.SendEmail:
                    return await SendEmailAsync(handler, formData);

                case FormActionType.SaveToDatabase:
                    return await SaveToDatabaseAsync(handler, formData);

                default:
                    return false;
            }
        }

        private async Task<bool> SaveToDatabaseAsync(FormHandler handler, IFormCollection formData)
        {
            var submission = new FormSubmission
            {
                FormHandlerId = handler.Id,
                DataJson = JsonSerializer.Serialize(formData.ToDictionary(k => k.Key, v => v.Value.ToString()))
            };
            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> SendEmailAsync(FormHandler handler, IFormCollection formData)
        {
            var settings = _settingsService.Current;

            if (string.IsNullOrWhiteSpace(settings.SmtpHost) ||
                string.IsNullOrWhiteSpace(settings.SmtpUser) ||
                string.IsNullOrWhiteSpace(settings.SmtpPassword))
            {
                // SMTP ayarları yapılmamışsa başarısız ol.
                return false;
            }

            try
            {
                var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword),
                    EnableSsl = settings.SmtpUseSsl
                };

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(settings.SmtpUser, settings.SiteTitle);
                mailMessage.To.Add(handler.ActionTarget);
                mailMessage.Subject = $"Yeni Form Mesajı: {handler.Name}";

                var body = new StringBuilder();
                body.AppendLine($"<strong>{handler.Name}</strong> formundan yeni bir mesaj aldınız:<br/><hr/>");
                foreach (var item in formData)
                {
                    if (item.Key.StartsWith("__") || item.Key.ToLower() == "data-dizgem-handler-id") continue;
                    body.AppendLine($"<strong>{item.Key}:</strong> {item.Value}<br/>");
                }
                mailMessage.Body = body.ToString();
                mailMessage.IsBodyHtml = true;

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                // E-posta gönderme hatası
                return false;
            }
        }
    }
}
