// ============================================================
// IRepository.cs
// FIX: Added async variants — all original methods were sync-only
//      causing thread pool starvation under concurrent requests
// ============================================================

using System.Linq.Expressions;

namespace SmartClassAI.DataAccess.Repository.IRepository;

public interface IRepository<T> where T : class
{
    // =============================================
    // SYNCHRONOUS (kept for backward compat)
    // =============================================

    IEnumerable<T> GetAll(
        Expression<Func<T, bool>>? filter = null,
        string? includeProperties = null
    );

    T? Get(
        Expression<Func<T, bool>> filter,
        string? includeProperties = null
    );

    // =============================================
    // ASYNCHRONOUS (NEW — use these going forward)
    // =============================================

    Task<IEnumerable<T>> GetAllAsync(
        Expression<Func<T, bool>>? filter = null,
        string? includeProperties = null
    );

    Task<T?> GetAsync(
        Expression<Func<T, bool>> filter,
        string? includeProperties = null
    );

    Task<int> CountAsync(
        Expression<Func<T, bool>>? filter = null
    );

    Task<bool> AnyAsync(
        Expression<Func<T, bool>> filter
    );

    // =============================================
    // WRITE (unchanged signatures, EF tracks changes)
    // =============================================

    void Add(T entity);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);
}