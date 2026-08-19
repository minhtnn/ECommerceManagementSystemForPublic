using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;

public interface IUnitOfWork : IGenericRepositoryFactory, IDisposable
{
    int Commit();
    Task<int> CommitAsync();
    Task<DatabaseTransactionResult> BeginTransactionAsync();
    Task<DatabaseTransactionResult> CommitTransactionAsync();
    Task<DatabaseTransactionResult> RollbackTransactionAsync();
}

public interface IUnitOfWork<TContext> : IUnitOfWork where TContext : DbContext
{
    TContext Context { get; }
}