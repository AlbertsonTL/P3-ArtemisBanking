using ArtemisBanking.Application.DTOs.Account;
using ArtemisBanking.Application.DTOs.Commerce;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Domain.Entities;
using ArtemisBanking.Domain.Enums;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.WebAPI.Controllers;

[ApiController]
[Route("api/commerce")]
[Authorize(Roles = "Admin")]
[Tags("Commerce")]
public class CommerceController : ControllerBase
{
    private readonly IGenericRepository<Commerce, int> _commerceRepo;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;

    public CommerceController(
        IGenericRepository<Commerce, int> commerceRepo,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _commerceRepo = commerceRepo;
        _userManager = userManager;
        _mapper = mapper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<CommerceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResponse<CommerceDto>>> GetCommerces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        // Si no se pasan parámetros (page=1, pageSize=20 defaults), retorna activos
        var query = _commerceRepo.Query()
            .Where(c => c.IsActive)
            .AsQueryable();

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<CommerceDto>>(items);

        return Ok(new PaginatedResponse<CommerceDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = page > 1,
            HasNextPage = page < totalPages
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CommerceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommerceDto>> GetCommerceById(int id)
    {
        var commerce = await _commerceRepo.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (commerce == null)
            return NotFound(new { message = "Comercio no encontrado" });

        return Ok(_mapper.Map<CommerceDto>(commerce));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CommerceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CommerceDto>> CreateCommerce([FromBody] CreateCommerceDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "El nombre es requerido" });

        var commerce = _mapper.Map<Commerce>(request);

        await _commerceRepo.AddAsync(commerce);
        await _commerceRepo.SaveChangesAsync();

        var dto = _mapper.Map<CommerceDto>(commerce);
        return CreatedAtAction(nameof(GetCommerceById), new { id = commerce.Id }, dto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCommerce(int id, [FromBody] UpdateCommerceDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "El nombre es requerido" });

        var commerce = await _commerceRepo.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (commerce == null)
            return NotFound(new { message = "Comercio no encontrado" });

        commerce.Name = request.Name;
        _commerceRepo.Update(commerce);
        await _commerceRepo.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ToggleCommerceStatus(int id, [FromBody] ToggleCommerceStatusDto request)
    {
        var commerce = await _commerceRepo.Query().FirstOrDefaultAsync(c => c.Id == id);
        if (commerce == null)
            return NotFound(new { message = "Comercio no encontrado" });

        commerce.IsActive = request.Status;
        _commerceRepo.Update(commerce);
        await _commerceRepo.SaveChangesAsync();

        // Al desactivar: desactivar todos los usuarios del comercio en cascada
        if (!request.Status)
        {
            var commerceUsers = _userManager.Users
                .Where(u => u.CommerceId == id && u.Role == UserRole.Comercio)
                .ToList();

            foreach (var user in commerceUsers)
            {
                user.IsActive = false;
                await _userManager.UpdateAsync(user);
            }
        }
        // Al reactivar: los usuarios permanecen inactivos (deben hacer reset de contraseña)
        // No se toma acción sobre los usuarios.

        return NoContent();
    }
}

// ── DTOs exclusivos de la API ─────────────────────────────────────────────────

public class UpdateCommerceDto
{
    public string Name { get; set; } = string.Empty;
}

public class ToggleCommerceStatusDto
{
    public bool Status { get; set; }
}
