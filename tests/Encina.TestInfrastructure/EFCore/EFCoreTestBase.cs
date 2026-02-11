using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for all EF Core tests that provides common fixture access patterns.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
public abstract class EFCoreTestBase<TFixture> : IAsyncLifetime
    where TFixture : class, IEFCoreFixture
{
    /// <summary>
    /// Gets the EF Core fixture instance.
    /// </summary>
    protected abstract TFixture Fixture { get; }

    /// <summary>
    /// Creates a new DbContext instance for the test.
    /// </summary>
    /// <typeparam name="TContext">The type of DbContext to create.</typeparam>
    /// <returns>A new DbContext instance configured for the underlying provider.</returns>
    protected TContext CreateDbContext<TContext>() where TContext : DbContext
        => Fixture.CreateDbContext<TContext>();

    /// <summary>
    /// Gets the connection string for the underlying database.
    /// </summary>
    protected string ConnectionString => Fixture.ConnectionString;

    /// <summary>
    /// Gets the provider name (e.g., "SqlServer", "PostgreSQL").
    /// </summary>
    protected string ProviderName => Fixture.ProviderName;

    /// <inheritdoc />
    public virtual async ValueTask InitializeAsync()
    {
        await Fixture.ClearAllDataAsync();
    }

    /// <inheritdoc />
    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
