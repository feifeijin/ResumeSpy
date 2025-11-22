using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Core.Models.Email;
using ResumeSpy.Infrastructure.Configuration;

namespace ResumeSpy.Infrastructure.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> emailOptions, ILogger<SmtpEmailSender> logger)
        {
            _settings = emailOptions.Value;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message.To))
            {
                throw new ArgumentException("Recipient address is required", nameof(message));
            }

            using var smtpClient = new SmtpClient(_settings.Smtp.Host, _settings.Smtp.Port)
            {
                EnableSsl = _settings.Smtp.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_settings.Smtp.Username) && !string.IsNullOrWhiteSpace(_settings.Smtp.Password))
            {
                smtpClient.Credentials = new NetworkCredential(_settings.Smtp.Username, _settings.Smtp.Password);
            }

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.From.Address, _settings.From.Name),
                Subject = message.Subject,
                Body = message.HtmlBody ?? message.TextBody ?? string.Empty,
                IsBodyHtml = !string.IsNullOrEmpty(message.HtmlBody)
            };

            mailMessage.To.Add(message.To);

            foreach (var header in message.Headers)
            {
                mailMessage.Headers[header.Key] = header.Value;
            }

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", message.To);
                throw;
            }
        }
    }
}
