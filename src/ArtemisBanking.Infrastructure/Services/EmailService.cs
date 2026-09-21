using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ArtemisBanking.Infrastructure.Services;
 
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var mail = new MimeMessage();
        var from = _configuration["MailSettings:SenderEmail"];
        var fromName = _configuration["MailSettings:SenderName"] ?? "Artemis Banking";
        var host = _configuration["MailSettings:Host"];
        var user = _configuration["MailSettings:UserName"];
        var password = _configuration["MailSettings:Password"];

        if (string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "La configuración de correo está incompleta. Configure MailSettings:Host, " +
                "SenderEmail, UserName y Password.");
        }

        if (!int.TryParse(_configuration["MailSettings:Port"], out var port) ||
            port is < 1 or > 65535)
        {
            throw new InvalidOperationException("MailSettings:Port debe ser un puerto válido.");
        }

        mail.From.Add(new MailboxAddress(fromName, from));
        mail.To.Add(MailboxAddress.Parse(request.To));
        mail.Subject = request.Subject;

        var builder = new BodyBuilder();
        if (request.IsHtml) builder.HtmlBody = request.Body;
        else builder.TextBody = request.Body;
        mail.Body = builder.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            // Gmail muestra las contraseñas de aplicación agrupadas con espacios.
            await smtp.AuthenticateAsync(user, password.Replace(" ", string.Empty));
            await smtp.SendAsync(mail);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el correo a {Recipient}.", request.To);
            throw new InvalidOperationException(
                "No se pudo enviar el correo mediante el servidor SMTP configurado.", ex);
        }
    }
}
