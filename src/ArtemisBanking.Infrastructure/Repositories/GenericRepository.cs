using System.Linq.Expressions;
using ArtemisBanking.Application.Interfaces.Repositories;
using ArtemisBanking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBanking.Infrastructure.Repositories;

public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(TKey id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<TEntity>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) => await _dbSet.Where(predicate).ToListAsync();

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) => await _dbSet.FirstOrDefaultAsync(predicate);

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate) => await _dbSet.AnyAsync(predicate);

    public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null) => predicate is null ? await _dbSet.CountAsync() : await _dbSet.CountAsync(predicate);

    public IQueryable<TEntity> Query() => _dbSet.AsQueryable();

    public async Task<(IEnumerable<TEntity> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null)
    {
        IQueryable<TEntity> query = _dbSet;
        if (filter != null) query = query.Where(filter);
        int total = await query.CountAsync();
        if (orderBy != null) query = orderBy(query);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task AddAsync(TEntity entity) => await _dbSet.AddAsync(entity);
    public async Task AddRangeAsync(IEnumerable<TEntity> entities) => await _dbSet.AddRangeAsync(entities);
    public void Update(TEntity entity) => _dbSet.Update(entity);
    public void Remove(TEntity entity) => _dbSet.Remove(entity);
    public void RemoveRange(IEnumerable<TEntity> entities) => _dbSet.RemoveRange(entities);
    public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
}