using System.Data;
using Encina.ADO.PostgreSQL;
using Encina.Messaging;
using Encina.Security.Audit;
using DbReadAuditStore = Encina.ADO.PostgreSQL.Auditing.ReadAuditStoreADO;

namespace Encina.UnitTests.ADO.PostgreSQL;

/// <summary>
/// Verifies that the database-backed <see cref="IReadAuditStore"/> wins over
/// <see cref="InMemoryReadAuditStore"/> regardless of the order in which
/// <c>AddEncinaReadAuditing</c> and <c>AddEncinaADO</c> run (#1269).
/// </summary>
public sealed class ReadAuditStoreRegistrationTests
{
    [Fact]
    public void AddEncinaReadAuditing_ThenAddEncinaADO_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaReadAuditing();
        services.AddEncinaADO(config => config.UseReadAuditStore = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReadAuditStore>()
            .ShouldBeOfType<DbReadAuditStore>();
    }

    [Fact]
    public void AddEncinaADO_ThenAddEncinaReadAuditing_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaADO(config => config.UseReadAuditStore = true);
        services.AddEncinaReadAuditing();

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReadAuditStore>()
            .ShouldBeOfType<DbReadAuditStore>();
    }

    [Fact]
    public void CustomReadAuditStore_RegisteredBeforeProvider_StaysResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());
        var customStore = Substitute.For<IReadAuditStore>();
        services.AddSingleton(customStore);

        // Act
        services.AddEncinaADO(config => config.UseReadAuditStore = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReadAuditStore>().ShouldBeSameAs(customStore);
    }
}
