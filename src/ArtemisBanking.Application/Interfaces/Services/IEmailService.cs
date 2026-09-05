using ArtemisBanking.Application.DTOs.Email;

namespace ArtemisBanking.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(EmailRequestDto request);
}
