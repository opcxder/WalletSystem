
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using WalletSystem.Core.Interfaces.Services;
using WalletSystem.Infrastructure.Config;

namespace WalletSystem.Infrastructure.ExternalServices
{
    public class EmailService : IEmailService
    {

        private readonly SmtpSettings _smtp;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpOptions, ILogger<EmailService> logger)
        {
            _smtp = smtpOptions.Value;
            _logger = logger;
        }

        public async Task SendMailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtp.SenderName, _smtp.SenderEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtp.Server, _smtp.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtp.SenderUsername, _smtp.AppPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

            }
            catch(Exception e)
            {
                _logger.LogError( e , "Error in the Email Service");
                throw new InvalidOperationException("Email service failed while sending email", e);
            }
        }
    }
}
