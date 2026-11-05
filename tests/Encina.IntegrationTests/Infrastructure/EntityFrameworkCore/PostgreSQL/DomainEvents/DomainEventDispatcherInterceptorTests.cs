using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.DomainEvents;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.DomainEvents;

/// <summary>The domain event dispatch scenarios of #2863 on EF Core over PostgreSQL.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class DomainEventDispatcherInterceptorTests(EFCorePostgreSqlFixture fixture)
    : DomainEventDispatcherInterceptorEFTestsBase
{
    /// <inheritdoc />
    protected override IEFCoreFixture Fixture => fixture;

    /// <inheritdoc />
    protected override void UseDatabase(DbContextOptionsBuilder options) => options.UseNpgsql(fixture.ConnectionString);
}
