using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Outbox;

/// <summary>The transactional outbox scenarios of #718 on EF Core over SQL Server.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class OutboxAtomicityTests(EFCoreSqlServerFixture fixture) : OutboxAtomicityEFTestsBase<TestEFDbContext>
{
    /// <inheritdoc />
    protected override IEFCoreFixture Fixture => fixture;

    /// <inheritdoc />
    protected override void UseDatabase(DbContextOptionsBuilder options, bool retryOnFailure) =>
        options.UseSqlServer(fixture.ConnectionString, sql =>
        {
            if (retryOnFailure)
            {
                sql.EnableRetryOnFailure();
            }
        });
}
