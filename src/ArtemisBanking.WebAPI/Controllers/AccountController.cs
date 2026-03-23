using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Shared.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
 
namespace ArtemisBanking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IJwtService jwtService,
        IEmailService emailService,
        IMapper mapper)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _emailService = emailService;
        _mapper = mapper;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "UserName y Password son requeridos" });

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            return Unauthorized(new { message = "Usuario o contraseña incorrectos" });

        if (!user.IsActive)
            return Unauthorized(new { message = "Usuario inactivo. Por favor confirme su correo." });

        var token = _jwtService.GenerateToken(user);
        var userDto = _mapper.Map<UserDto>(user);

        return Ok(new LoginResponseDto
        {
            Jwt = token,
            User = userDto
        });
    }

    [HttpPost("confirm")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmAccount([FromBody] ConfirmAccountDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Token requerido" });

        var users = await _userManager.GetUsersForClaimAsync(
            new System.Security.Claims.Claim("EmailConfirmationToken", request.Token));

        if (users == null || users.Count == 0)
        {
            var user = await _userManager.FindByEmailAsync(request.Token);
            if (user == null)
                return BadRequest(new { message = "Token invalido" });

            user.EmailConfirmed = true;
            user.IsActive = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(new { message = "Error al confirmar la cuenta", errors = result.Errors });

            return NoContent();
        }

        var confirmedUser = users.FirstOrDefault();
        if (confirmedUser == null)
            return BadRequest(new { message = "Token invalido" });

        confirmedUser.EmailConfirmed = true;
        confirmedUser.IsActive = true;
        var updateResult = await _userManager.UpdateAsync(confirmedUser);

        if (!updateResult.Succeeded)
            return BadRequest(new { message = "Error al confirmar la cuenta", errors = updateResult.Errors });

        return NoContent();
    }

    [HttpPost("get-reset-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetResetToken([FromBody] GetResetTokenDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new { message = "UserName requerido" });

        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return BadRequest(new { message = "Usuario no existe" });

        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

        try
        {
            var emailBody = $"<h2>Solicitud de Reset de Contrasena</h2><p>Tu token de reset:</p><code>{resetToken}</code><p>Valido por 24 horas.</p>";

            await _emailService.SendAsync(new EmailRequestDto
            {
                To = user.Email!,
                Subject = "Reset de Contrasena",
                Body = emailBody,
                IsHtml = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo: {ex.Message}");
        }

        return NoContent();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword))
            return BadRequest(new { message = "Todos los campos son requeridos" });

        if (request.Password != request.ConfirmPassword)
            return BadRequest(new { message = "Las contrasenas no coinciden" });

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return BadRequest(new { message = "Usuario no encontrado" });

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
            return BadRequest(new
            {
                message = "Error al resetear la contrasena",
                errors = result.Errors.Select(e => e.Description)
            });

        user.IsActive = true;
        user.EmailConfirmed = true;
        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
            return BadRequest(new { message = "Error al activar la cuenta", errors = updateResult.Errors });

        return NoContent();
    }
}
