using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.DomainEvents;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.DomainEvents;

/// <summary>The domain event dispatch scenarios of #2863 on EF Core over SQL Server.</summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class DomainEventDispatcherInterceptorTests(EFCoreSqlServerFixture fixture)
    : DomainEventDispatcherInterceptorEFTestsBase
{
    /// <inheritdoc />
    protected override IEFCoreFixture Fixture => fixture;

    /// <inheritdoc />
    protected override void UseDatabase(DbContextOptionsBuilder options) => options.UseSqlServer(fixture.ConnectionString);
}
