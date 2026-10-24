using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.Outbox;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Outbox;

/// <summary>
/// The transactional outbox scenarios of #718 on EF Core over MySQL. They are skipped, like the rest of the
/// EF Core MySQL suite, until Pomelo.EntityFrameworkCore.MySql supports EF Core 10 (#2086): enable them by
/// returning null from <see cref="SkipReason"/> and configuring <c>UseMySql</c> in <see cref="UseDatabase"/>.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class OutboxAtomicityTests(EFCoreMySqlFixture fixture) : OutboxAtomicityEFTestsBase<TestEFDbContext>
{
    /// <inheritdoc />
    protected override IEFCoreFixture Fixture => fixture;

    /// <inheritdoc />
    protected override string? SkipReason =>
        "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility (#2086)";

    /// <inheritdoc />
    protected override void UseDatabase(DbContextOptionsBuilder options, bool retryOnFailure)
    {
        // o.UseMySql(fixture.ConnectionString, ServerVersion.AutoDetect(...), my => my.EnableRetryOnFailure())
        // once the Pomelo provider supports EF Core 10 (#2086).
    }
}
