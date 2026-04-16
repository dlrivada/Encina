using Encina.Security.Audit;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Security.Audit;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaAudit_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IPiiMasker>().ShouldNotBeNull();
        provider.GetService<IAuditStore>().ShouldNotBeNull();
        provider.GetService<IAuditEntryFactory>().ShouldNotBeNull();
        provider.GetService<IOptions<AuditOptions>>().ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAudit_ShouldRegisterNullPiiMaskerByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();

        // Assert
        var masker = provider.GetRequiredService<IPiiMasker>();
        masker.ShouldBeOfType<NullPiiMasker>();
    }

    [Fact]
    public void AddEncinaAudit_ShouldRegisterInMemoryAuditStoreByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();

        // Assert
        var store = provider.GetRequiredService<IAuditStore>();
        store.ShouldBeOfType<InMemoryAuditStore>();
    }

    [Fact]
    public void AddEncinaAudit_ShouldRegisterDefaultAuditEntryFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetRequiredService<IAuditEntryFactory>();
        factory.ShouldBeOfType<DefaultAuditEntryFactory>();
    }

    [Fact]
    public void AddEncinaAudit_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit(options =>
        {
            options.AuditAllCommands = false;
            options.AuditAllQueries = true;
            options.IncludePayloadHash = false;
            options.RetentionDays = 365;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuditOptions>>().Value;

        // Assert
        options.AuditAllCommands.ShouldBeFalse();
        options.AuditAllQueries.ShouldBeTrue();
        options.IncludePayloadHash.ShouldBeFalse();
        options.RetentionDays.ShouldBe(365);
    }

    [Fact]
    public void AddEncinaAudit_WithoutConfiguration_ShouldUseDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuditOptions>>().Value;

        // Assert
        options.AuditAllCommands.ShouldBeTrue();
        options.AuditAllQueries.ShouldBeFalse();
        options.IncludePayloadHash.ShouldBeTrue();
        options.RetentionDays.ShouldBe(2555);
    }

    [Fact]
    public void AddEncinaAudit_ShouldAllowCustomPiiMaskerOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IPiiMasker, CustomPiiMasker>();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();

        // Assert - Custom masker should be used (TryAdd doesn't override)
        var masker = provider.GetRequiredService<IPiiMasker>();
        masker.ShouldBeOfType<CustomPiiMasker>();
    }

    [Fact]
    public void AddEncinaAudit_ShouldAllowCustomAuditStoreOverride()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuditStore, CustomAuditStore>();

        // Act
        services.AddEncinaAudit();
        var provider = services.BuildServiceProvider();

        // Assert - Custom store should be used (TryAdd doesn't override)
        var store = provider.GetRequiredService<IAuditStore>();
        store.ShouldBeOfType<CustomAuditStore>();
    }

    [Fact]
    public void AddEncinaAudit_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAudit();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddEncinaAudit_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaAudit();

        // Assert
        Should.Throw<ArgumentNullException>(act)
                .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaAudit_CalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var act = () =>
        {
            services.AddEncinaAudit();
            services.AddEncinaAudit();
        };

        // Assert
        Should.NotThrow(act);
    }

    [Fact]
    public void AddEncinaAudit_ShouldRegisterAuditEntryFactoryAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(IAuditEntryFactory));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaAudit_ShouldRegisterPiiMaskerAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(IPiiMasker));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAudit_ShouldRegisterAuditStoreAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit();

        // Assert
        var descriptor = services.First(d => d.ServiceType == typeof(IAuditStore));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaAudit_WithAutoPurgeEnabled_ShouldRegisterRetentionService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit(options =>
        {
            options.EnableAutoPurge = true;
            options.RetentionDays = 90;
            options.PurgeIntervalHours = 12;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            d.ImplementationType == typeof(AuditRetentionService));
        descriptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaAudit_WithAutoPurgeDisabled_ShouldNotRegisterRetentionService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit(options =>
        {
            options.EnableAutoPurge = false;
        });

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            d.ImplementationType == typeof(AuditRetentionService));
        descriptor.ShouldBeNull();
    }

    [Fact]
    public void AddEncinaAudit_WithDefaultOptions_ShouldNotRegisterRetentionService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddEncinaAudit(); // EnableAutoPurge defaults to false

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
            d.ImplementationType == typeof(AuditRetentionService));
        descriptor.ShouldBeNull();
    }

    #region Custom Test Implementations

    private sealed class CustomPiiMasker : IPiiMasker
    {
        public T MaskForAudit<T>(T request) where T : notnull => request;
        public object MaskForAudit(object request) => request;
    }

    private sealed class CustomAuditStore : IAuditStore
    {
        private static readonly IReadOnlyList<AuditEntry> EmptyEntries = System.Array.Empty<AuditEntry>();

        public ValueTask<Either<EncinaError, Unit>> RecordAsync(
            AuditEntry entry, CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, Unit>(Unit.Default));

        public ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByEntityAsync(
            string entityType, string? entityId, CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, IReadOnlyList<AuditEntry>>(EmptyEntries));

        public ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByUserAsync(
            string userId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, IReadOnlyList<AuditEntry>>(EmptyEntries));

        public ValueTask<Either<EncinaError, IReadOnlyList<AuditEntry>>> GetByCorrelationIdAsync(
            string correlationId, CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, IReadOnlyList<AuditEntry>>(EmptyEntries));

        public ValueTask<Either<EncinaError, PagedResult<AuditEntry>>> QueryAsync(
            AuditQuery query, CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, PagedResult<AuditEntry>>(PagedResult<AuditEntry>.Empty()));

        public ValueTask<Either<EncinaError, int>> PurgeEntriesAsync(
            DateTime olderThanUtc, CancellationToken cancellationToken = default)
            => new(Prelude.Right<EncinaError, int>(0));
    }

    #endregion
}
