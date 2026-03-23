using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.Email;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using ArtemisBanking.Shared.Helpers;
using ArtemisBanking.Shared.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ArtemisBanking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGenericRepository<SavingsAccount, int> _savingsRepository;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        IGenericRepository<SavingsAccount, int> savingsRepository,
        IEmailService emailService,
        IMapper mapper)
    {
        _userManager = userManager;
        _savingsRepository = savingsRepository;
        _emailService = emailService;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] UserRole? rol = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _userManager.Users.AsQueryable();

        if (rol.HasValue)
            query = query.Where(u => u.Role == rol.Value);
        else
            query = query.Where(u => u.Role != UserRole.Comercio);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = _mapper.Map<List<UserDto>>(users);

        return Ok(new PaginatedResponse<UserDto>
        {
            Data = userDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        });
    }

    [HttpGet("commerce")]
    [ProducesResponseType(typeof(PaginatedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<UserDto>>> GetCommerceUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _userManager.Users
            .Where(u => u.Role == UserRole.Comercio)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = _mapper.Map<List<UserDto>>(users);

        return Ok(new PaginatedResponse<UserDto>
        {
            Data = userDtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(userDto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.IdentityCard))
            return BadRequest(new { message = "Todos los campos son requeridos" });

        var existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
            return Conflict(new { message = "El usuario ya existe" });

        var existingEmail = await _userManager.FindByEmailAsync(request.Email);
        if (existingEmail != null)
            return Conflict(new { message = "El correo ya esta registrado" });

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            IdentityCard = request.IdentityCard,
            Role = request.Role,
            IsActive = false,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = "Error al crear el usuario", errors = result.Errors });

        if (request.Role == UserRole.Cliente || request.Role == UserRole.Comercio)
        {
            var accountNumber = AccountNumberGenerator.Generate9Digits();
            var savingsAccount = new SavingsAccount
            {
                AccountNumber = accountNumber,
                AccountType = AccountType.Main,
                ClientId = user.Id,
                IsActive = true,
                Balance = request.InitialAmount,
                CreatedAt = DateTime.UtcNow
            };

            await _savingsRepository.AddAsync(savingsAccount);
            await _savingsRepository.SaveChangesAsync();
        }

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        try
        {
            var emailBody = $"<h2>Bienvenido a Artemis Banking</h2><p>Tu cuenta ha sido creada.</p><p>Token de confirmacion:</p><code>{confirmToken}</code>";

            await _emailService.SendAsync(new EmailRequestDto
            {
                To = user.Email!,
                Subject = "Bienvenido a Artemis Banking",
                Body = emailBody,
                IsHtml = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo: {ex.Message}");
        }

        var userDto = _mapper.Map<UserDto>(user);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userDto);
    }

    [HttpPost("commerce")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> CreateCommerceUser([FromBody] CreateUserDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Todos los campos son requeridos" });

        var existingUser = await _userManager.FindByNameAsync(request.UserName);
        if (existingUser != null)
            return Conflict(new { message = "El usuario ya existe" });

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            Email = request.Email,
            IdentityCard = request.IdentityCard,
            Role = UserRole.Comercio,
            IsActive = false,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = "Error al crear el usuario", errors = result.Errors });

        var accountNumber = AccountNumberGenerator.Generate9Digits();
        var savingsAccount = new SavingsAccount
        {
            AccountNumber = accountNumber,
            AccountType = AccountType.Main,
            ClientId = user.Id,
            IsActive = true,
            Balance = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _savingsRepository.AddAsync(savingsAccount);
        await _savingsRepository.SaveChangesAsync();

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        try
        {
            var emailBody = $"<h2>Bienvenido a Artemis Banking</h2><p>Tu cuenta de comercio ha sido creada.</p><p>Token de confirmacion:</p><code>{confirmToken}</code>";

            await _emailService.SendAsync(new EmailRequestDto
            {
                To = user.Email!,
                Subject = "Bienvenido a Artemis Banking",
                Body = emailBody,
                IsHtml = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo: {ex.Message}");
        }

        var userDto = _mapper.Map<UserDto>(user);
        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, userDto);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Todos los campos son requeridos" });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

        if (user.UserName != request.UserName)
        {
            var existingUser = await _userManager.FindByNameAsync(request.UserName);
            if (existingUser != null)
                return Conflict(new { message = "El usuario ya existe" });
        }

        if (user.Email != request.Email)
        {
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
                return Conflict(new { message = "El correo ya esta registrado" });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IdentityCard = request.IdentityCard;
        user.UserName = request.UserName;
        user.Email = request.Email;
        user.NormalizedUserName = request.UserName.ToUpper();
        user.NormalizedEmail = request.Email.ToUpper();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { message = "Error al actualizar el usuario", errors = result.Errors });

        if (request.AdditionalAmount > 0 && user.Role == UserRole.Cliente)
        {
            var account = await _savingsRepository.Query()
                .Where(a => a.ClientId == user.Id && a.AccountType == AccountType.Main)
                .FirstOrDefaultAsync();

            if (account != null)
            {
                account.Balance += request.AdditionalAmount;
                _savingsRepository.Update(account);
                await _savingsRepository.SaveChangesAsync();
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
            if (!passwordResult.Succeeded)
                return BadRequest(new { message = "Error al cambiar la contrasena", errors = passwordResult.Errors });
        }

        return NoContent();
    }

    [HttpPatch("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangeUserStatus(string id, [FromBody] ChangeUserStatusDto request)
    {

        var authenticatedUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (id == authenticatedUserId)
            return Forbid();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

        user.IsActive = request.Status;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return BadRequest(new { message = "Error al cambiar el estado", errors = result.Errors });

        return NoContent();
    }
}
