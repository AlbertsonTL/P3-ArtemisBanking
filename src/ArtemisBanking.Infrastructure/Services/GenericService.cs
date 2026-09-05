using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Application.Interfaces.Services;
using ArtemisBanking.Domain.Common;
using AutoMapper;

namespace ArtemisBanking.Infrastructure.Services;

public class GenericService<TEntity, TDto, TCreateDto, TKey> : IGenericService<TDto, TCreateDto, TKey>
    where TEntity    : BaseEntity<TKey>
    where TDto       : class
    where TCreateDto : class
{
    protected readonly IGenericRepository<TEntity, TKey> _repository;
    protected readonly IMapper _mapper;

    public GenericService(IGenericRepository<TEntity, TKey> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper     = mapper;
    }

    public async Task<TDto?> GetByIdAsync(TKey id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : _mapper.Map<TDto>(entity);
    }

    public async Task<IEnumerable<TDto>> GetAllAsync() => _mapper.Map<IEnumerable<TDto>>(await _repository.GetAllAsync());

    public async Task<TDto> CreateAsync(TCreateDto createDto)
    {
        var entity = _mapper.Map<TEntity>(createDto);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<TDto>(entity);
    }

    public async Task UpdateAsync(TKey id, TCreateDto updateDto)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Entidad {id} no encontrada.");
        _mapper.Map(updateDto, entity);
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task DeleteAsync(TKey id)
    {
        var entity = await _repository.GetByIdAsync(id) ?? throw new KeyNotFoundException($"Entidad {id} no encontrada.");
        _repository.Remove(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task<(IEnumerable<TDto> Items, int TotalCount)> GetPagedAsync(int page, int pageSize)
    {
        var (items, total) = await _repository.GetPagedAsync(page, pageSize);
        return (_mapper.Map<IEnumerable<TDto>>(items), total);
    }
}
