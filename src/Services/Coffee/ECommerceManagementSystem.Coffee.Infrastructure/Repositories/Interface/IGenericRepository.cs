using System.Linq.Expressions;
using ECommerceManagementSystem.Coffee.Infrastructure.Filter;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate.Interface;
using Microsoft.EntityFrameworkCore.Query;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;

public interface IGenericRepository<T> : IDisposable where T : class
{
    #region Get

    Task<T> SingleOrDefaultAsync(
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);

    Task<TResult> SingleOrDefaultAsync<TResult>(
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);

    Task<TResult> SingleOrDefaultAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);

    Task<ICollection<T>> GetListAsync(
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);

    Task<ICollection<TResult>> GetListAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);

    Task<ICollection<TResult>> GetListAsync<TResult>(
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null);

    Task<IPaginate<T>> GetPagingListAsync(
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
        int page = 1,
        int size = 10,
        string sortBy = null,
        bool isAsc = true);

    Task<IPaginate<TResult>> GetPagingListAsync<TResult>(
        IFilter<T> filter = null,
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
        int page = 1,
        int size = 10,
        string sortBy = null,
        bool isAsc = true
    );

    Task<IPaginate<TResult>> GetPagingListAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        IFilter<T> filter = null,
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
        int page = 1,
        int size = 10,
        string sortBy = null,
        bool isAsc = true
    );


    #endregion

    #region Insert

    Task InsertAsync(T entity);

    Task InsertRangeAsync(IEnumerable<T> entities);

    #endregion

    #region Update

    void UpdateAsync(T entity);

    void UpdateRange(IEnumerable<T> entities);

    #endregion

    #region Delete

    void DeleteAsync(T entity);
    void DeleteRangeAsync(IEnumerable<T> entities);

    #endregion

    #region Other

    /// <summary>
    /// Determines whether any elements exist that satisfy the specified condition
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition</param>
    /// <returns>true if any elements in the source sequence pass the test; otherwise, false</returns>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null);

    #endregion
}