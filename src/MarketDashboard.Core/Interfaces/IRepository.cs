namespace MarketDashboard.Core.Interfaces;

using MarketDashboard.Core.Entities;

/// <summary>
/// Generic repository abstraction for data access.
/// Satisfies PAW Architecture — Repository Pattern requirement.
/// </summary>
/// <typeparam name="T">Entity type inheriting BaseEntity</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    // Queries
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<IEnumerable<T>> GetAllAsync(
        CancellationToken ct = default);

    Task<IEnumerable<T>> FindAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    Task<int> CountAsync(
        System.Linq.Expressions.Expression<Func<T, bool>> predicate,
        CancellationToken ct = default);

    // Commands
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
