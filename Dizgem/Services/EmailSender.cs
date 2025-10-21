using System.Net;
using System.Net.Mail;

namespace Dizgem.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ISettingsService _settingsService;

        public EmailSender(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var settings = _settingsService.Current;

            if (string.IsNullOrWhiteSpace(settings.SmtpHost) || string.IsNullOrWhiteSpace(settings.SmtpUser))
            {
                // SMTP ayarları yapılmamışsa e-posta gönderme.
                // Burada bir loglama yapılabilir.
                return;
            }

            using (var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort))
            {
                client.EnableSsl = true; // Genellikle SSL gereklidir.
                client.Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(settings.SmtpUser, settings.SiteTitle),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
