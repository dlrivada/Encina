using Encina.Messaging;
using Encina.Messaging.DeadLetter;
using Encina.Messaging.Encryption;
using Encina.Messaging.Encryption.Serialization;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.Messaging.Serialization;
using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;
using Encina.UnitTests.Messaging.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Encina.UnitTests.Messaging.Serialization;

/// <summary>
/// Every provider family (ADO.NET, Dapper, EF Core, MongoDB) and every standalone registration
/// (dead letter queue, recoverability) must make <see cref="IMessageSerializer"/> resolvable,
/// because the outbox post-processor and the inbox, saga, scheduler, dead-letter and delayed-retry
/// components take it as a required dependency (#1259 review). With
/// <c>AddEncinaMessageEncryption</c>, registered before or after the provider, the resolved
/// serializer must be the encrypting decorator.
/// </summary>
public sealed class MessageSerializerRegistrationTests
{
    public enum Encryption
    {
        None,
        RegisteredBeforeProvider,
        RegisteredAfterProvider
    }

    public enum ProviderFamily
    {
        AdoPostgreSql,
        DapperPostgreSql,
        EntityFrameworkCore,
        MongoDB
    }

    public static TheoryData<ProviderFamily, Encryption> ProviderMatrix()
    {
        var data = new TheoryData<ProviderFamily, Encryption>();
        foreach (var family in Enum.GetValues<ProviderFamily>())
        {
            foreach (var encryption in Enum.GetValues<Encryption>())
            {
                data.Add(family, encryption);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ProviderMatrix))]
    public void Provider_WithAllMessagingPatterns_ResolvesEveryComponentThatNeedsTheSerializer(
        ProviderFamily family,
        Encryption encryption)
    {
        // Arrange
        var services = CreateBaseServices(encryption);
        RegisterProvider(services, family);

        // Gaps outside the serializer that ValidateOnBuild surfaces (reported as follow-ups of
        // the #1259 review): the low-ceremony SagaRunner that ADO.NET/Dapper register needs an
        // IRequestContext nobody registers, and AddEncinaMongoDB registers InboxOrchestrator
        // without InboxOptions. They are filled here so this test isolates the serializer.
        services.AddScoped(_ => Substitute.For<IRequestContext>());
        services.TryAddSingleton(new InboxOptions());

        if (encryption == Encryption.RegisteredAfterProvider)
        {
            services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);
        }

        // Act
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        // Assert
        AssertSerializer(sp, encryption);
        sp.GetServices<IRequestPostProcessor<ProbeRequest, string>>()
            .ShouldContain(p => p is OutboxPostProcessor<ProbeRequest, string>);
        sp.GetRequiredService<InboxOrchestrator>().ShouldNotBeNull();
        sp.GetRequiredService<SagaOrchestrator>().ShouldNotBeNull();
        sp.GetRequiredService<SchedulerOrchestrator>().ShouldNotBeNull();
    }

    [Theory]
    [InlineData(Encryption.None)]
    [InlineData(Encryption.RegisteredBeforeProvider)]
    [InlineData(Encryption.RegisteredAfterProvider)]
    public void DeadLetterQueue_Standalone_ResolvesOrchestratorAndManager(Encryption encryption)
    {
        // Arrange
        var services = CreateBaseServices(encryption);
        services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, ProbeDeadLetterMessageFactory>();
        if (encryption == Encryption.RegisteredAfterProvider)
        {
            services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);
        }

        // Act
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        // Assert
        AssertSerializer(scope.ServiceProvider, encryption);
        scope.ServiceProvider.GetRequiredService<DeadLetterOrchestrator>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<IDeadLetterManager>().ShouldBeOfType<DeadLetterManager>();
    }

    [Theory]
    [InlineData(Encryption.None)]
    [InlineData(Encryption.RegisteredBeforeProvider)]
    [InlineData(Encryption.RegisteredAfterProvider)]
    public void Recoverability_WithDelayedRetries_ResolvesScheduler(Encryption encryption)
    {
        // Arrange
        var services = CreateBaseServices(encryption);
        services.AddEncinaRecoverability();
        services.AddEncinaDelayedRetryStore<ProbeDelayedRetryStore, ProbeDelayedRetryMessageFactory>();
        if (encryption == Encryption.RegisteredAfterProvider)
        {
            services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);
        }

        // Act
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        // Assert
        AssertSerializer(scope.ServiceProvider, encryption);
        scope.ServiceProvider.GetRequiredService<IDelayedRetryScheduler>().ShouldBeOfType<DelayedRetryScheduler>();
    }

    [Fact]
    public void TryAddDefaultMessageSerializer_DoesNotReplaceAnExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var custom = new JsonMessageSerializer();
        services.AddSingleton<IMessageSerializer>(custom);

        // Act
        services.TryAddDefaultMessageSerializer();

        // Assert
        services.Count(d => d.ServiceType == typeof(IMessageSerializer)).ShouldBe(1);
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMessageSerializer>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void TryAddDefaultMessageSerializer_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() => MessagingServiceCollectionExtensions.TryAddDefaultMessageSerializer(null!));
    }

    private static ServiceCollection CreateBaseServices(Encryption encryption)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();

        if (encryption != Encryption.None)
        {
            services.AddKeyMaterial();
        }

        if (encryption == Encryption.RegisteredBeforeProvider)
        {
            services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);
        }

        return services;
    }

    private static void RegisterProvider(IServiceCollection services, ProviderFamily family)
    {
        static void AllPatterns(MessagingConfiguration config)
        {
            config.UseOutbox = true;
            config.UseInbox = true;
            config.UseSagas = true;
            config.UseScheduling = true;
        }

        switch (family)
        {
            case ProviderFamily.AdoPostgreSql:
                global::Encina.ADO.PostgreSQL.ServiceCollectionExtensions.AddEncinaADO(
                    services, "Host=localhost;Database=probe;Username=probe;Password=probe", AllPatterns);
                break;

            case ProviderFamily.DapperPostgreSql:
                global::Encina.Dapper.PostgreSQL.ServiceCollectionExtensions.AddEncinaDapper(
                    services, "Host=localhost;Database=probe;Username=probe;Password=probe", AllPatterns);
                break;

            case ProviderFamily.EntityFrameworkCore:
                services.AddDbContext<ProbeDbContext>(o => o.UseInMemoryDatabase($"serializer-registration-{Guid.NewGuid():N}"));
                global::Encina.EntityFrameworkCore.ServiceCollectionExtensions.AddEncinaEntityFrameworkCore<ProbeDbContext>(
                    services, AllPatterns);
                break;

            case ProviderFamily.MongoDB:
                global::Encina.MongoDB.ServiceCollectionExtensions.AddEncinaMongoDB(services, options =>
                {
                    options.ConnectionString = "mongodb://localhost:27017";
                    options.DatabaseName = "probe";
                    options.UseOutbox = true;
                    options.UseInbox = true;
                    options.UseSagas = true;
                    options.UseScheduling = true;
                    options.CreateIndexes = false;
                });
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(family), family, null);
        }
    }

    private static void AssertSerializer(IServiceProvider sp, Encryption encryption)
    {
        var serializer = sp.GetRequiredService<IMessageSerializer>();
        if (encryption == Encryption.None)
        {
            serializer.ShouldBeOfType<JsonMessageSerializer>();
        }
        else
        {
            serializer.ShouldBeOfType<EncryptingMessageSerializer>();
        }
    }

    public sealed record ProbeRequest(string Value) : IRequest<string>;

    public sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options);

    public sealed class ProbeDeadLetterMessageFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => new FakeDeadLetterMessage { Id = data.Id };
    }

    public sealed class ProbeDelayedRetryStore : IDelayedRetryStore
    {
        public Task AddAsync(IDelayedRetryMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IEnumerable<IDelayedRetryMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<IDelayedRetryMessage>>([]);

        public Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkAsFailedAsync(Guid id, string errorMessage, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> DeleteByContextIdAsync(Guid recoverabilityContextId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    public sealed class ProbeDelayedRetryMessageFactory : IDelayedRetryMessageFactory
    {
        public IDelayedRetryMessage Create(DelayedRetryMessageData data) => throw new NotSupportedException();
    }
}
