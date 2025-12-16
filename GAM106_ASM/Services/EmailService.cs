using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace GAM106_ASM.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var portStr = smtpSection["Port"];
            var user = smtpSection["User"];
            var password = smtpSection["Password"];
            var from = smtpSection["From"];
            var enableSsl = bool.TryParse(smtpSection["EnableSsl"], out var ssl) && ssl;

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException("SMTP settings are missing. Please configure Smtp:Host/User/Password/From in configuration.");
            }

            if (!int.TryParse(portStr, out var port))
            {
                port = 587;
            }

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(user, password)
            };

            var mail = new MailMessage(from, to, subject, body)
            {
                IsBodyHtml = false
            };

            await client.SendMailAsync(mail);
        }
    }
}
