using System.ComponentModel.DataAnnotations;
using AutoMapper;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Repositories;

public class UnitOfWork<TContext> : IUnitOfWork<TContext> where TContext : DbContext
{
    public TContext Context { get; }
    private Dictionary<Type, object> _repositories;
    private readonly IMapper _mapper;
    private IDbContextTransaction _transaction;

    public UnitOfWork(TContext context, IMapper mapper)
    {
        Context = context;
        _mapper = mapper;
    }

    public IGenericRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        _repositories ??= new Dictionary<Type, object>();
        if (_repositories.TryGetValue(typeof(TEntity), out object repository))
        {
            return (IGenericRepository<TEntity>)repository;
        }

        repository = new GenericRepository<TEntity>(Context, _mapper);
        _repositories.Add(typeof(TEntity), repository);
        return (IGenericRepository<TEntity>)repository;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        Context?.Dispose();
    }

    public int Commit()
    {
        TrackChanges();
        return Context.SaveChanges();
    }

    public async Task<int> CommitAsync()
    {
        TrackChanges();
        return await Context.SaveChangesAsync();
    }

    public async Task<DatabaseTransactionResult> BeginTransactionAsync()
    {
        try
        {
            if (_transaction != null)
            {
                return new DatabaseTransactionResult
                {
                    IsSuccess = false,
                    Message = "Transaction already exists"
                };
            }

            _transaction = await Context.Database.BeginTransactionAsync();
            return new DatabaseTransactionResult
            {
                IsSuccess = true,
                Message = "Transaction started successfully"
            };
        }
        catch (Exception ex)
        {
            return new DatabaseTransactionResult
            {
                IsSuccess = false,
                Message = "Failed to start transaction",
                Exception = ex
            };
        }
    }

    public async Task<DatabaseTransactionResult> CommitTransactionAsync()
    {
        try
        {
            if (_transaction == null)
            {
                return new DatabaseTransactionResult
                {
                    IsSuccess = false,
                    Message = "No active transaction"
                };
            }

            // Validate before commit
            var validationErrors = ValidateChanges();
            if (validationErrors.Any())
            {
                await RollbackTransactionAsync();
                return new DatabaseTransactionResult
                {
                    IsSuccess = false,
                    Message = "Validation failed",
                    ValidationErrors = validationErrors
                };
            }

            // Save changes
            var rowsAffected = await Context.SaveChangesAsync();
            await _transaction.CommitAsync();

            return new DatabaseTransactionResult
            {
                IsSuccess = true,
                RowsAffected = rowsAffected,
                Message = $"Transaction committed successfully. {rowsAffected} rows affected"
            };
        }
        catch (DbUpdateException dbEx)
        {
            await RollbackTransactionAsync();
            return new DatabaseTransactionResult
            {
                IsSuccess = false,
                Message = "Database update failed",
                Exception = dbEx
            };
        }
        catch (Exception ex)
        {
            await RollbackTransactionAsync();
            return new DatabaseTransactionResult
            {
                IsSuccess = false,
                Message = "Transaction commit failed",
                Exception = ex
            };
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task<DatabaseTransactionResult> RollbackTransactionAsync()
    {
        try
        {
            if (_transaction == null)
            {
                return new DatabaseTransactionResult
                {
                    IsSuccess = false,
                    Message = "No active transaction to rollback"
                };
            }

            await _transaction.RollbackAsync();
            return new DatabaseTransactionResult
            {
                IsSuccess = true,
                Message = "Transaction rolled back successfully"
            };
        }
        catch (Exception ex)
        {
            return new DatabaseTransactionResult
            {
                IsSuccess = false,
                Message = "Rollback failed",
                Exception = ex
            };
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    // Validation Methods
    private void TrackChanges()
    {
        var validationErrors = Context.ChangeTracker.Entries<IValidatableObject>()
            .SelectMany(e => e.Entity.Validate(null))
            .Where(e => e != ValidationResult.Success)
            .ToArray();

        if (validationErrors.Any())
        {
            var exceptionMessage = string.Join(Environment.NewLine,
                validationErrors.Select(error =>
                    $"Properties {string.Join(", ", error.MemberNames)} Error: {error.ErrorMessage}"));
            throw new Exception(exceptionMessage);
        }
    }

    private List<ValidationError> ValidateChanges()
    {
        return Context.ChangeTracker.Entries<IValidatableObject>()
            .SelectMany(e => e.Entity.Validate(null))
            .Where(e => e != ValidationResult.Success)
            .Select(e => new ValidationError
            {
                MemberNames = e.MemberNames.ToArray(),
                ErrorMessage = e.ErrorMessage
            })
            .ToList();
    }

    // private void TrackChanges()
    // {
    //     var validationErrors = Context.ChangeTracker.Entries<IValidatableObject>()
    //         .SelectMany(e => e.Entity.Validate(null))
    //         .Where(e => e != ValidationResult.Success)
    //         .ToArray();
    //     if (validationErrors.Any())
    //     {
    //         var exceptionMessage = string.Join(Environment.NewLine,
    //             validationErrors.Select(error => $"Properties {error.MemberNames} Error: {error.ErrorMessage}"));
    //         throw new Exception(exceptionMessage);
    //     }
    // }
}