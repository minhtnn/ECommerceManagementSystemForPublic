using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ECommerceManagementSystem.Coffee.Infrastructure.Filter;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate;
using ECommerceManagementSystem.Coffee.Infrastructure.Paginate.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly DbContext _dbContext;
    protected readonly DbSet<T> _dbSet;
    protected readonly IMapper _mapper;

    public GenericRepository(DbContext context, IMapper mapper)
    {
        _dbContext = context;
        _mapper = mapper;
        _dbSet = context.Set<T>();
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region Get

    public virtual async Task<T> SingleOrDefaultAsync(
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) return await orderBy(query).AsNoTracking().FirstOrDefaultAsync();

        return await query.AsSplitQuery().AsNoTracking().FirstOrDefaultAsync();
    }

    public virtual async Task<TResult> SingleOrDefaultAsync<TResult>(
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) query = orderBy(query);

        var projectedQuery = query
            .AsNoTracking()
            .ProjectTo<TResult>(_mapper.ConfigurationProvider);

        return await projectedQuery.FirstOrDefaultAsync();
    }

    public virtual async Task<TResult> SingleOrDefaultAsync<TResult>(Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>> predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;
        if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) return await orderBy(query).AsNoTracking().Select(selector).FirstOrDefaultAsync();

        return await query.AsNoTracking().Select(selector).FirstOrDefaultAsync();
    }

    public virtual async Task<ICollection<T>> GetListAsync(
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            var filterExpression = filter.ToExpression();
            query = query.Where(filterExpression);
        }

        if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) query = orderBy(query);
        var projectedQuery = query
            .AsNoTracking();
        return await projectedQuery.ToListAsync();
    }

    public virtual async Task<ICollection<TResult>> GetListAsync<TResult>(
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            var filterExpression = filter.ToExpression();
            query = query.Where(filterExpression);
        }
        // if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) query = orderBy(query);

        var projectedQuery = query
            .AsNoTracking()
            .ProjectTo<TResult>(_mapper.ConfigurationProvider);

        return await projectedQuery.ToListAsync();
    }

    public virtual async Task<ICollection<TResult>> GetListAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            var filterExpression = filter.ToExpression();
            query = query.Where(filterExpression);
        }

        if (include != null) query = include(query);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null) return await orderBy(query).AsNoTracking().Select(selector).ToListAsync();

        return await query.Select(selector).ToListAsync();
    }

    public async Task<IPaginate<T>> GetPagingListAsync(
        Expression<Func<T, bool>> predicate = null,
        IFilter<T> filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
        int page = 1,
        int size = 10,
        string sortBy = null,
        bool isAsc = true)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            var filterExpression = filter.ToExpression();
            query = query.Where(filterExpression);
        }

        if (predicate != null) query = query.Where(predicate);
        if (include != null) query = include(query);
        if (!string.IsNullOrEmpty(sortBy))
        {
            query = ApplySort(query, sortBy, isAsc);
        }
        else if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.AsNoTracking().ToPaginateAsync(page, size, 1);
    }

    public async Task<IPaginate<TResult>> GetPagingListAsync<TResult>(
        IFilter<T> filter,
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
        int page = 1,
        int size = 10,
        string sortBy = null,
        bool isAsc = true)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            var filterExpression = filter.ToExpression();
            query = query.Where(filterExpression);
        }

        if (predicate != null) query = query.Where(predicate);
        if (include != null) query = include(query);
        if (!string.IsNullOrEmpty(sortBy))
        {
            query = ApplySort(query, sortBy, isAsc);
        }
        else if (orderBy != null)
        {
            query = orderBy(query);
        }

        var projectedQuery = query
            .AsNoTracking()
            .ProjectTo<TResult>(_mapper.ConfigurationProvider);
        return await projectedQuery.ToPaginateAsync(page, size, 1);
    }

    public async Task<IPaginate<TResult>> GetPagingListAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        IFilter<T> filter,
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null,
        int page = 1,
        int size = 10,
        string sortBy = null,
        bool isAsc = true)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            var filterExpression = filter.ToExpression();
            query = query.Where(filterExpression);
        }

        if (predicate != null) query = query.Where(predicate);
        if (include != null) query = include(query);
        if (!string.IsNullOrEmpty(sortBy))
        {
            query = ApplySort(query, sortBy, isAsc);
        }
        else if (orderBy != null)
        {
            query = orderBy(query);
        }

        return await query.AsNoTracking().Select(selector).ToPaginateAsync(page, size, 1);
    }

    private IQueryable<T> ApplySort(IQueryable<T> query, string sortBy, bool isAsc)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property =
            typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            throw new ArgumentException($"Property '{sortBy}' not found on type {typeof(T).Name}");
        }

        var propertyAccess = Expression.Property(parameter, property);
        var lambda = Expression.Lambda(propertyAccess, parameter);

        string methodName = isAsc ? "OrderBy" : "OrderByDescending";

        var resultExpression = Expression.Call(typeof(Queryable), methodName,
            new Type[] { typeof(T), propertyAccess.Type },
            query.Expression, Expression.Quote(lambda));
        return query.Provider.CreateQuery<T>(resultExpression);
    }

    #endregion

    #region Insert

    public async Task InsertAsync(T entity)
    {
        if (entity == null) return;
        await _dbSet.AddAsync(entity);
    }

    public async Task InsertRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    #endregion

    #region Update

    public void UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
    }

    public void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    #endregion

    #region Delete

    public void DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void DeleteRangeAsync(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    #endregion

    #region Other

    /// <summary>
    /// Determines whether any elements exist that satisfy the specified condition
    /// </summary>
    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate = null)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.AsNoTracking().AnyAsync();
    }

    #endregion
}