using Encina.Security.Audit;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        provider.GetService<IPiiMasker>().Should().NotBeNull();
        provider.GetService<IAuditStore>().Should().NotBeNull();
        provider.GetService<IAuditEntryFactory>().Should().NotBeNull();
        provider.GetService<IOptions<AuditOptions>>().Should().NotBeNull();
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
        masker.Should().BeOfType<NullPiiMasker>();
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
        store.Should().BeOfType<InMemoryAuditStore>();
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
        factory.Should().BeOfType<DefaultAuditEntryFactory>();
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
        options.AuditAllCommands.Should().BeFalse();
        options.AuditAllQueries.Should().BeTrue();
        options.IncludePayloadHash.Should().BeFalse();
        options.RetentionDays.Should().Be(365);
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
        options.AuditAllCommands.Should().BeTrue();
        options.AuditAllQueries.Should().BeFalse();
        options.IncludePayloadHash.Should().BeTrue();
        options.RetentionDays.Should().Be(2555);
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
        masker.Should().BeOfType<CustomPiiMasker>();
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
        store.Should().BeOfType<CustomAuditStore>();
    }

    [Fact]
    public void AddEncinaAudit_ShouldReturnServicesForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaAudit();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaAudit_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaAudit();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
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
        act.Should().NotThrow();
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
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
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
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
    }

    #endregion
}
