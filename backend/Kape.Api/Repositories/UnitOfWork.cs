using System.Data;
using Kape.Api.Data;
using Kape.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kape.Api.Repositories;

public sealed class UnitOfWork(KapeDbContext dbContext) : IUnitOfWork
{
    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default) =>
        dbContext.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
