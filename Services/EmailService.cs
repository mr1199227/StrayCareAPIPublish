using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using StrayCareAPI.Settings;

namespace StrayCareAPI.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;
        private readonly string _templatePath;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _templatePath = Path.Combine(AppContext.BaseDirectory, "Templates");       
     }

        public async Task SendOtpAsync(string email, string otpCode)
        {
            try
            {
                var templateContent = await GetTemplateAsync("OtpEmail.html");
                var emailBody = templateContent
                                    .Replace("{{OtpCode}}", otpCode)
                                    .Replace("{{Company}}", "StrayCare")
                                    .Replace("{{Year}}", DateTime.Now.Year.ToString());

                await SendEmailAsync(email, "Your Verification Code", emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to {Email}", email);
                throw; 
            }
        }

        public async Task SendStatusUpdateAsync(string email, string subject, string statusMessage, string userName)
        {
             try
            {
                var templateContent = await GetTemplateAsync("StatusUpdateEmail.html");
                var emailBody = templateContent
                                    .Replace("{{UserName}}", userName)
                                    .Replace("{{StatusMessage}}", statusMessage)
                                    .Replace("{{Company}}", "StrayCare")
                                    .Replace("{{Year}}", DateTime.Now.Year.ToString());

                await SendEmailAsync(email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send status update email to {Email}", email);
            }
        }

        private async Task<string> GetTemplateAsync(string templateName)
        {
            var filePath = Path.Combine(_templatePath, templateName);

                if (!File.Exists(filePath))
                {
                     _logger.LogError("Email template not found at {Path}", filePath);
                     throw new FileNotFoundException($"Email template not found: {filePath}");
                }

                return await File.ReadAllTextAsync(filePath);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            using var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlMessage };

            using var smtp = new SmtpClient();
            try 
            {
                // Accept all SSL certificates (dangerous in production, but often needed for dev)
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                if (_emailSettings.UseSsl)
                {
                    await smtp.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                }
                else
                {
                    await smtp.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.None);
                }

                await smtp.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                await smtp.SendAsync(email);
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }
}
