namespace PersonalFinance.Core.Interfaces;

/// <summary>
/// Explicit unit of work. Call SaveChanges once at the end of a business operation.
/// Use ExecuteInTransactionAsync for multi-entity money operations (balance + row).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs the operation inside a DB transaction, then SaveChanges + Commit.
    /// Rolls back on any exception.
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}