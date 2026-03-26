using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ArtemisBanking.Infrastructure.Services;
 
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration) => _configuration = configuration;

    public async Task SendAsync(EmailRequestDto request)
    {
        var mail = new MimeMessage();
        var from = _configuration["MailSettings:SenderEmail"]!;
        var fromName = _configuration["MailSettings:SenderName"] ?? "Artemis Banking";
        var host = _configuration["MailSettings:Host"] ?? "localhost";
        var portStr = _configuration["MailSettings:Port"] ?? "587";
        if (!int.TryParse(portStr, out int port)) port = 587;
        
        var user = _configuration["MailSettings:UserName"] ?? from;
        var password = _configuration["MailSettings:Password"] ?? "password";

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
            // Si el host es EMAIL_HOST (placeholder), no intentar conectar
            if (host == "EMAIL_HOST" || string.IsNullOrEmpty(host)) return;

            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(user, password);
            await smtp.SendAsync(mail);
            await smtp.DisconnectAsync(true);
        }
        catch { /* Fallback for development: just don't crash */ }
    }
}
