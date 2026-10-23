using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Outbox;

/// <summary>The transactional outbox scenarios of #718 on EF Core over PostgreSQL.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class OutboxAtomicityTests(EFCorePostgreSqlFixture fixture) : OutboxAtomicityEFTestsBase<TestPostgreSqlDbContext>
{
    /// <inheritdoc />
    protected override IEFCoreFixture Fixture => fixture;

    /// <inheritdoc />
    protected override void UseDatabase(DbContextOptionsBuilder options, bool retryOnFailure) =>
        options.UseNpgsql(fixture.ConnectionString, npgsql =>
        {
            if (retryOnFailure)
            {
                npgsql.EnableRetryOnFailure();
            }
        });
}
