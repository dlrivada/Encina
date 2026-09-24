using System.Data;
using Encina.Dapper.MySQL;
using Encina.Messaging;
using Encina.Security.Audit;
using DbReadAuditStore = Encina.Dapper.MySQL.Auditing.ReadAuditStoreDapper;

namespace Encina.UnitTests.Dapper.MySQL;

/// <summary>
/// Verifies that the database-backed <see cref="IReadAuditStore"/> wins over
/// <see cref="InMemoryReadAuditStore"/> regardless of the order in which
/// <c>AddEncinaReadAuditing</c> and <c>AddEncinaDapper</c> run (#1269).
/// </summary>
public sealed class ReadAuditStoreRegistrationTests
{
    [Fact]
    public void AddEncinaReadAuditing_ThenAddEncinaDapper_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaReadAuditing();
        services.AddEncinaDapper(config => config.UseReadAuditStore = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReadAuditStore>()
            .ShouldBeOfType<DbReadAuditStore>();
    }

    [Fact]
    public void AddEncinaDapper_ThenAddEncinaReadAuditing_ResolvesDatabaseStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IDbConnection>());

        // Act
        services.AddEncinaDapper(config => config.UseReadAuditStore = true);
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
        services.AddEncinaDapper(config => config.UseReadAuditStore = true);

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IReadAuditStore>().ShouldBeSameAs(customStore);
    }
}
