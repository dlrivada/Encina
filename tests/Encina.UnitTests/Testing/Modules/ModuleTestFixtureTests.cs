using Encina.Testing;
using System.Reflection;
using Encina.Modules;
using Encina.Testing.Modules;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Testing.Modules;

public sealed class ModuleTestFixtureTests
{
    #region Test Infrastructure

    public sealed class TestModule : IModule
    {
        public string Name => "Test";

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ITestRepository, TestRepository>();
        }
    }

    public interface ITestRepository
    {
        string GetData();
    }

    public sealed class TestRepository : ITestRepository
    {
        public string GetData() => "real-data";
    }

    public sealed record TestCommand(string Value) : IRequest<string>;

    public sealed class TestCommandHandler : IRequestHandler<TestCommand, string>
    {
        private readonly ITestRepository _repository;

        public TestCommandHandler(ITestRepository repository)
        {
            _repository = repository;
        }

        public Task<Either<EncinaError, string>> Handle(
            TestCommand request,
            CancellationToken cancellationToken)
        {
            var data = _repository.GetData();
            return Task.FromResult(Right<EncinaError, string>($"{request.Value}:{data}"));
        }
    }

    public sealed record TestNotification(string Message) : INotification;

    public interface IDependentModuleApi
    {
        Task<Either<EncinaError, string>> GetExternalDataAsync(string id);
    }

    public sealed class FakeDependentModuleApi : IDependentModuleApi
    {
        public Task<Either<EncinaError, string>> GetExternalDataAsync(string id)
        {
            return Task.FromResult(Right<EncinaError, string>($"fake-{id}"));
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesModuleInstance()
    {
        // Act
        using var fixture = new ModuleTestFixture<TestModule>();

        // Assert
        fixture.Module.ShouldNotBeNull();
        fixture.Module.Name.ShouldBe("Test");
    }

    #endregion

    #region Module Mocking Tests

    [Fact]
    public void WithMockedModule_NullImplementation_ThrowsArgumentNullException()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            fixture.WithMockedModule<IDependentModuleApi>((IDependentModuleApi)null!));
    }

    [Fact]
    public void WithMockedModule_WithImplementation_RegistersService()
    {
        // Arrange
        var fake = new FakeDependentModuleApi();

        using var fixture = new ModuleTestFixture<TestModule>()
            .WithMockedModule<IDependentModuleApi>(fake);

        // Act
        fixture.Build();
        var resolved = fixture.GetService<IDependentModuleApi>();

        // Assert
        resolved.ShouldBe(fake);
    }

    [Fact]
    public void WithFakeModule_RegistersService()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithFakeModule<IDependentModuleApi, FakeDependentModuleApi>();

        // Act
        fixture.Build();
        var resolved = fixture.GetService<IDependentModuleApi>();

        // Assert
        resolved.ShouldNotBeNull();
        resolved.ShouldBeOfType<FakeDependentModuleApi>();
    }

    [Fact]
    public void WithFakeModule_WithInstance_RegistersInstance()
    {
        // Arrange
        var instance = new FakeDependentModuleApi();

        using var fixture = new ModuleTestFixture<TestModule>()
            .WithFakeModule<IDependentModuleApi, FakeDependentModuleApi>(instance);

        // Act
        fixture.Build();
        var resolved = fixture.GetService<IDependentModuleApi>();

        // Assert
        resolved.ShouldBe(instance);
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void WithService_RegistersService()
    {
        // Arrange
        var testService = new TestService();

        using var fixture = new ModuleTestFixture<TestModule>()
            .WithService<ITestService>(testService);

        // Act
        fixture.Build();
        var resolved = fixture.GetService<ITestService>();

        // Assert
        resolved.ShouldBe(testService);
    }

    [Fact]
    public void ConfigureServices_AppliesConfiguration()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITestService, TestService>();
            });

        // Act
        fixture.Build();
        var resolved = fixture.GetService<ITestService>();

        // Assert
        resolved.ShouldNotBeNull();
    }

    public interface ITestService
    {
        string GetValue();
    }

    public sealed class TestService : ITestService
    {
        public string GetValue() => "test-value";
    }

    #endregion

    #region Messaging Store Tests

    [Fact]
    public void WithMockedOutbox_ConfiguresOutboxStore()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithMockedOutbox();

        // Act
        fixture.Build();

        // Assert
        fixture.Outbox.ShouldNotBeNull();
    }

    [Fact]
    public void Outbox_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fixture.Outbox);
    }

    [Fact]
    public void WithMockedInbox_ConfiguresInboxStore()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithMockedInbox();

        // Act
        fixture.Build();

        // Assert
        fixture.Inbox.ShouldNotBeNull();
    }

    [Fact]
    public void WithMockedSaga_ConfiguresSagaStore()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithMockedSaga();

        // Act
        fixture.Build();

        // Assert
        fixture.SagaStore.ShouldNotBeNull();
    }

    [Fact]
    public void WithMockedScheduling_ConfiguresScheduledMessageStore()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithMockedScheduling();

        // Act
        fixture.Build();

        // Assert
        fixture.ScheduledMessageStore.ShouldNotBeNull();
    }

    [Fact]
    public void WithMockedDeadLetter_ConfiguresDeadLetterStore()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithMockedDeadLetter();

        // Act
        fixture.Build();

        // Assert
        fixture.DeadLetterStore.ShouldNotBeNull();
    }

    [Fact]
    public void WithAllMockedStores_ConfiguresAllStores()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithAllMockedStores();

        // Act
        fixture.Build();

        // Assert
        fixture.Outbox.ShouldNotBeNull();
        fixture.Inbox.ShouldNotBeNull();
        fixture.SagaStore.ShouldNotBeNull();
        fixture.ScheduledMessageStore.ShouldNotBeNull();
        fixture.DeadLetterStore.ShouldNotBeNull();
    }

    #endregion

    #region Time Provider Tests

    [Fact]
    public void WithFakeTimeProvider_ConfiguresTimeProvider()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithFakeTimeProvider();

        // Act
        fixture.Build();

        // Assert
        fixture.TimeProvider.ShouldNotBeNull();
    }

    [Fact]
    public void WithFakeTimeProvider_WithStartTime_SetsInitialTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

        using var fixture = new ModuleTestFixture<TestModule>()
            .WithFakeTimeProvider(startTime);

        // Act
        fixture.Build();

        // Assert
        fixture.TimeProvider.GetUtcNow().ShouldBe(startTime);
    }

    [Fact]
    public void AdvanceTimeBy_AdvancesTime()
    {
        // Arrange
        var startTime = new DateTimeOffset(2025, 1, 1, 12, 0, 0, TimeSpan.Zero);

        using var fixture = new ModuleTestFixture<TestModule>()
            .WithFakeTimeProvider(startTime);

        fixture.Build();

        // Act
        fixture.AdvanceTimeBy(TimeSpan.FromHours(2));

        // Assert
        fixture.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(2));
    }

    [Fact]
    public void TimeProvider_NotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fixture.TimeProvider);
    }

    #endregion

    #region Integration Events Tests

    [Fact]
    public void IntegrationEvents_InitiallyEmpty()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();

        // Act & Assert
        fixture.IntegrationEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PublishAsync_CapturesIntegrationEvent()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();
        fixture.Build();

        // Act
        await fixture.PublishAsync(new TestNotification("hello"));

        // Assert
        fixture.IntegrationEvents.Count.ShouldBe(1);
        fixture.IntegrationEvents.Contains<TestNotification>().ShouldBeTrue();
    }

    [Fact]
    public void ClearStores_ClearsIntegrationEvents()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>()
            .WithAllMockedStores();

        fixture.Build();
        fixture.IntegrationEvents.Add(new TestNotification("test"));

        // Act
        fixture.ClearStores();

        // Assert
        fixture.IntegrationEvents.Count.ShouldBe(0);
    }

    #endregion

    #region ServiceProvider Tests

    [Fact]
    public void ServiceProvider_NotBuilt_ThrowsInvalidOperationException()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => fixture.ServiceProvider);
    }

    [Fact]
    public void GetRequiredService_ReturnsService()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();
        fixture.Build();

        // Act
        var repository = fixture.GetRequiredService<ITestRepository>();

        // Assert
        repository.ShouldNotBeNull();
        repository.GetData().ShouldBe("real-data");
    }

    [Fact]
    public void GetService_NotRegistered_ReturnsNull()
    {
        // Arrange
        using var fixture = new ModuleTestFixture<TestModule>();
        fixture.Build();

        // Act
        var service = fixture.GetService<ITestService>();

        // Assert
        service.ShouldBeNull();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesServiceProvider()
    {
        // Arrange
        var fixture = new ModuleTestFixture<TestModule>();
        fixture.Build();

        // Act
        fixture.Dispose();

        // Assert - accessing ServiceProvider after dispose should throw an exception
        var ex = Should.Throw<Exception>(() => fixture.ServiceProvider);
        // Accept either ObjectDisposedException or the public InvalidOperationException the API exposes
        (ex is ObjectDisposedException || ex is InvalidOperationException).ShouldBeTrue();

        // Dispose is idempotent - calling again should not throw
        Should.NotThrow(() => fixture.Dispose());
    }

    [Fact]
    public async Task DisposeAsync_DisposesServiceProvider()
    {
        // Arrange
        var fixture = new ModuleTestFixture<TestModule>();
        fixture.Build();

        // Act
        await fixture.DisposeAsync();

        // Assert - accessing disposed fixture throws
        Should.Throw<InvalidOperationException>(() => fixture.ServiceProvider);
    }

    #endregion
}
