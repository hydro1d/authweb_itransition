using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthWeb.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(string email, string name, string confirmationLink)
        {
            // note: Fetch SMTP credentials from environment variables or appsettings
            var host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? _configuration["Smtp:Host"];
            var portStr = Environment.GetEnvironmentVariable("SMTP_PORT") ?? _configuration["Smtp:Port"];
            var user = Environment.GetEnvironmentVariable("SMTP_USER") ?? _configuration["Smtp:User"];
            var pass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? _configuration["Smtp:Password"];
            var from = Environment.GetEnvironmentVariable("SMTP_FROM") ?? _configuration["Smtp:From"] ?? "no-reply@authweb.com";

            int.TryParse(portStr, out int port);
            if (port <= 0) port = 587;

            // Log confirmation link for convenience during local development & video recording
            _logger.LogInformation("==================================================");
            _logger.LogInformation("CONFIRMATION EMAIL DISPATCHED TO: {Email}", email);
            _logger.LogInformation("CONFIRMATION LINK: {Link}", confirmationLink);
            _logger.LogInformation("==================================================");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                _logger.LogWarning("SMTP environment variables not configured. Email logged to console instead of SMTP dispatch.");
                return;
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(from, "AuthWeb Management"),
                    Subject = "Confirm your AuthWeb Account Email",
                    Body = $@"
                        <h2>Welcome to AuthWeb, {WebUtility.HtmlEncode(name)}!</h2>
                        <p>Please confirm your account email address by clicking the link below:</p>
                        <p><a href='{confirmationLink}'>Confirm Email Address</a></p>
                        <br/>
                        <p>Or copy and paste this URL into your browser:</p>
                        <p>{confirmationLink}</p>",
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Confirmation email successfully sent via SMTP to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMTP email to {Email}", email);
            }
        }
    }
}
