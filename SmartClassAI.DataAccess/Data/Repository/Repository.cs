// ============================================================
// Repository.cs
// FIX: Added async implementations for GetAllAsync, GetAsync,
//      CountAsync, AnyAsync — removes thread blocking under load
// ============================================================

using Microsoft.EntityFrameworkCore;
using SmartClassAI.DataAccess.Data;
using SmartClassAI.DataAccess.Repository.IRepository;
using System.Linq.Expressions;

namespace SmartClassAI.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
            dbSet = _db.Set<T>();
        }

        // =============================================
        // PRIVATE HELPER: Apply includes to query
        // =============================================
        private IQueryable<T> ApplyIncludes(
            IQueryable<T> query,
            string? includeProperties)
        {
            if (string.IsNullOrWhiteSpace(includeProperties))
                return query;

            var includes = includeProperties
                .Split(',', StringSplitOptions.RemoveEmptyEntries
                          | StringSplitOptions.TrimEntries);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return query;
        }

        // =============================================
        // SYNCHRONOUS (legacy support)
        // =============================================
        public IEnumerable<T> GetAll(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            query = ApplyIncludes(query, includeProperties);
            return query.ToList();
        }

        public T? Get(
            Expression<Func<T, bool>> filter,
            string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            query = query.Where(filter);
            query = ApplyIncludes(query, includeProperties);
            return query.FirstOrDefault();
        }

        // =============================================
        // ASYNCHRONOUS (use these in all new code)
        // =============================================
        public async Task<IEnumerable<T>> GetAllAsync(
            Expression<Func<T, bool>>? filter = null,
            string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            query = ApplyIncludes(query, includeProperties);
            return await query.ToListAsync();
        }

        public async Task<T?> GetAsync(
            Expression<Func<T, bool>> filter,
            string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;
            query = query.Where(filter);
            query = ApplyIncludes(query, includeProperties);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<int> CountAsync(
            Expression<Func<T, bool>>? filter = null)
        {
            IQueryable<T> query = dbSet;
            if (filter != null) query = query.Where(filter);
            return await query.CountAsync();
        }

        public async Task<bool> AnyAsync(
            Expression<Func<T, bool>> filter)
        {
            return await dbSet.AnyAsync(filter);
        }

        // =============================================
        // WRITE OPERATIONS
        // =============================================
        public void Add(T entity) => dbSet.Add(entity);

        public void Remove(T entity) => dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities)
            => dbSet.RemoveRange(entities);
    }
}