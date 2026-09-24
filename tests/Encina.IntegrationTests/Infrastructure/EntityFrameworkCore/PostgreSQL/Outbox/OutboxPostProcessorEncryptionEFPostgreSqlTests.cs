using System.Collections.Concurrent;
using System.Security.Cryptography;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Outbox;
using Encina.Messaging.Encryption;
using Encina.Messaging.Outbox;
using Encina.Security.Encryption;
using Encina.Security.Encryption.Abstractions;
using Encina.Security.Encryption.Algorithms;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;
using EfOutboxProcessor = Encina.EntityFrameworkCore.Outbox.OutboxProcessor;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.Outbox;

/// <summary>
/// End-to-end check of the path the #1168 fix changed, on EF Core over a real PostgreSQL:
/// a request implementing <see cref="IHasNotifications"/> is sent through <see cref="IEncina"/>
/// with <c>AddEncinaMessageEncryption</c> registered; <see cref="OutboxPostProcessor{TRequest, TResponse}"/>
/// writes the notification encrypted (AES-256-GCM, real key provider) into the raw
/// <c>Content</c> column, and the EF Core outbox processor publishes the decrypted notification.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
[Collection("EFCore-PostgreSQL")]
public sealed class OutboxPostProcessorEncryptionEFPostgreSqlTests : IAsyncLifetime
{
    private const string Diagnosis = "F41.1 generalized anxiety disorder";

    private readonly EFCorePostgreSqlFixture _fixture;

    public OutboxPostProcessorEncryptionEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ClearAllDataAsync();

    public async ValueTask DisposeAsync() => await _fixture.ClearAllDataAsync();

    [Fact]
    public async Task Send_RequestWithNotifications_StoresCiphertextAndProcessorPublishesPlaintext()
    {
        // Arrange
        var received = new ConcurrentQueue<DiagnosisRecordedNotification>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(received);
        services.AddDbContext<TestPostgreSqlDbContext>(o => o.UseNpgsql(_fixture.ConnectionString));
        services.AddEncina();
        services.AddTransient<IRequestHandler<RecordDiagnosisCommand, Unit>, RecordDiagnosisHandler>();
        services.AddTransient<INotificationHandler<DiagnosisRecordedNotification>, DiagnosisRecordedHandler>();
        services.AddEncinaEntityFrameworkCore<TestPostgreSqlDbContext>(config =>
        {
            config.UseOutbox = true;
            config.OutboxOptions.ProcessingInterval = TimeSpan.FromMilliseconds(100);
        });

        var keyProvider = new InMemoryKeyProvider();
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        keyProvider.AddKey("integration-key", key);
        keyProvider.SetCurrentKey("integration-key");
        services.AddSingleton<IKeyProvider>(keyProvider);
        services.AddSingleton<IFieldEncryptor>(new AesGcmFieldEncryptor(keyProvider));
        services.AddEncinaMessageEncryption(o => o.EncryptAllMessages = true);

        await using var provider = services.BuildServiceProvider();

        // Act 1: send the request; the post-processor stores its notification in the outbox
        await using (var scope = provider.CreateAsyncScope())
        {
            var encina = scope.ServiceProvider.GetRequiredService<IEncina>();
            var result = await encina.Send(new RecordDiagnosisCommand(Diagnosis));
            result.IsRight.ShouldBeTrue();
        }

        // Assert 1: the raw column holds the encrypted envelope, never the diagnosis
        await using (var verify = _fixture.CreateDbContext<TestPostgreSqlDbContext>())
        {
            var stored = await verify.Set<OutboxMessage>()
                .SingleAsync(m => m.NotificationType.Contains(nameof(DiagnosisRecordedNotification)));
            stored.Content.ShouldStartWith("ENC:v1:");
            stored.Content.ShouldNotContain("F41.1");
        }

        // Act 2: run the registered EF Core outbox processor until it delivers the message
        var processor = provider.GetServices<IHostedService>().OfType<EfOutboxProcessor>().Single();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await processor.StartAsync(cts.Token);
        try
        {
            while (received.IsEmpty)
            {
                await Task.Delay(50, cts.Token);
            }
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        // Assert 2: the handler received the decrypted notification
        received.ShouldHaveSingleItem().Diagnosis.ShouldBe(Diagnosis);
    }

    public sealed record DiagnosisRecordedNotification(string Diagnosis) : INotification;

    public sealed record RecordDiagnosisCommand(string Diagnosis) : IRequest<Unit>, IHasNotifications
    {
        public IEnumerable<INotification> GetNotifications() => [new DiagnosisRecordedNotification(Diagnosis)];
    }

    public sealed class RecordDiagnosisHandler : IRequestHandler<RecordDiagnosisCommand, Unit>
    {
        public Task<Either<EncinaError, Unit>> Handle(RecordDiagnosisCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(Right<EncinaError, Unit>(unit));
    }

    public sealed class DiagnosisRecordedHandler(ConcurrentQueue<DiagnosisRecordedNotification> received)
        : INotificationHandler<DiagnosisRecordedNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(DiagnosisRecordedNotification notification, CancellationToken cancellationToken)
        {
            received.Enqueue(notification);
            return Task.FromResult(Right<EncinaError, Unit>(unit));
        }
    }
}
