namespace ArtemisBanking.Application.Interfaces.Services;

public interface IGenericService<TDto, TCreateDto, TKey>
    where TDto       : class
    where TCreateDto : class
{
    Task<TDto?> GetByIdAsync(TKey id);
    Task<IEnumerable<TDto>> GetAllAsync();
    Task<TDto> CreateAsync(TCreateDto createDto);
    Task UpdateAsync(TKey id, TCreateDto updateDto);
    Task DeleteAsync(TKey id);
    Task<(IEnumerable<TDto> Items, int TotalCount)> GetPagedAsync(int page, int pageSize);
}