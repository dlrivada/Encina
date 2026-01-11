using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Encina.Testing.Pact.Tests;

public sealed class EncinaPactFixtureTests : IAsyncLifetime, IDisposable
{
    private readonly string _testPactDir;
    private readonly EncinaPactFixture _sut;

    public EncinaPactFixtureTests()
    {
        _testPactDir = Path.Combine(Path.GetTempPath(), $"pact-fixture-tests-{Guid.NewGuid():N}");
        _sut = new EncinaPactFixture { PactDirectory = _testPactDir };
    }

    public async Task InitializeAsync()
    {
        await _sut.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _sut.DisposeAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPactDir))
        {
            Directory.Delete(_testPactDir, true);
        }
    }

    [Fact]
    public void PactDirectory_CanBeSet()
    {
        // Assert
        _sut.PactDirectory.ShouldBe(_testPactDir);
    }

    [Fact]
    public async Task InitializeAsync_CreatesDirectory()
    {
        // Assert - directory should exist after InitializeAsync
        Directory.Exists(_testPactDir).ShouldBeTrue();
    }

    [Fact]
    public void CreateConsumer_ReturnsBuilder()
    {
        // Act
        var consumer = _sut.CreateConsumer("Consumer", "Provider");

        // Assert
        consumer.ShouldNotBeNull();
        consumer.ConsumerName.ShouldBe("Consumer");
        consumer.ProviderName.ShouldBe("Provider");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConsumer_InvalidConsumerName_Throws(string? invalidName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.CreateConsumer(invalidName!, "Provider"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConsumer_InvalidProviderName_Throws(string? invalidName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.CreateConsumer("Consumer", invalidName!));
    }

    [Fact]
    public void CreateVerifier_WithoutEncina_Throws()
    {
        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => _sut.CreateVerifier("Provider"));
        ex.Message.ShouldContain("Encina is not configured");
    }

    [Fact]
    public void CreateVerifier_WithEncina_ReturnsVerifier()
    {
        // Arrange
        var mockEncina = Substitute.For<IEncina>();
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();
        _sut.WithEncina(mockEncina, serviceProvider);

        // Act
        var verifier = _sut.CreateVerifier("Provider");

        // Assert
        verifier.ShouldNotBeNull();
        verifier.ProviderName.ShouldBe("Provider");
    }

    [Fact]
    public void WithEncina_NullEncina_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithEncina(null!, serviceProvider));
    }

    [Fact]
    public void WithEncina_NullServiceProvider_Throws()
    {
        // Arrange
        var mockEncina = Substitute.For<IEncina>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithEncina(mockEncina, null!));
    }

    [Fact]
    public void WithEncina_ReturnsFixture()
    {
        // Arrange
        var mockEncina = Substitute.For<IEncina>();
        var services = new ServiceCollection();
        using var serviceProvider = services.BuildServiceProvider();

        // Act
        var result = _sut.WithEncina(mockEncina, serviceProvider);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithServices_NullAction_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithServices(null!));
    }

    [Fact]
    public void WithServices_ConfiguresServices()
    {
        // Act
        var result = _sut.WithServices(services =>
        {
            services.AddSingleton<TestService>();
        });

        // Assert - fluent API returns same instance
        result.ShouldBeSameAs(_sut);

        // Assert - service is actually registered and resolvable
        _sut.ServiceProvider.ShouldNotBeNull();
        var resolvedService = _sut.ServiceProvider!.GetService<TestService>();
        resolvedService.ShouldNotBeNull();
    }

    [Fact]
    public async Task VerifyAsync_WithConsumerAndAsyncAction_ExecutesAction()
    {
        // Arrange
        var consumer = _sut.CreateConsumer("Consumer", "Provider");
        var actionExecuted = false;

        // Act
        await _sut.VerifyAsync(consumer, async (Uri uri) =>
        {
            actionExecuted = true;
            await Task.CompletedTask;
        });

        // Assert
        actionExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_WithConsumerAndSyncAction_ExecutesAction()
    {
        // Arrange
        var consumer = _sut.CreateConsumer("Consumer", "Provider");
        var actionExecuted = false;

        // Act
        await _sut.VerifyAsync(consumer, (Uri uri) =>
        {
            actionExecuted = true;
        });

        // Assert
        actionExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_NullConsumer_Throws()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.VerifyAsync(null!, async (Uri _) => await Task.CompletedTask));
    }

    [Fact]
    public async Task VerifyAsync_NullAsyncAction_Throws()
    {
        // Arrange
        var consumer = _sut.CreateConsumer("Consumer", "Provider");

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _sut.VerifyAsync(consumer, (Func<Uri, Task>)null!));
    }

    [Fact]
    public void GetPactFiles_EmptyDirectory_ReturnsEmpty()
    {
        // Act
        var files = _sut.GetPactFiles();

        // Assert
        files.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPactFiles_WithFiles_ReturnsFiles()
    {
        // Arrange
        var pactFile = Path.Combine(_testPactDir, "test-pact.json");
        await File.WriteAllTextAsync(pactFile, "{}");

        // Act
        var files = _sut.GetPactFiles();

        // Assert
        files.Count.ShouldBe(1);
        files[0].ShouldEndWith("test-pact.json");
    }

    [Fact]
    public async Task ClearPactFiles_RemovesFiles()
    {
        // Arrange
        var pactFile = Path.Combine(_testPactDir, "test-pact.json");
        await File.WriteAllTextAsync(pactFile, "{}");

        // Act
        _sut.ClearPactFiles();

        // Assert
        Directory.GetFiles(_testPactDir, "*.json").ShouldBeEmpty();
    }

    [Fact]
    public void Reset_DisposesConsumers()
    {
        // Arrange
        var consumer = _sut.CreateConsumer("Consumer", "Provider");

        // Start the mock server first
        consumer.Verify(_ => { });

        // Act
        _sut.Reset();

        // Assert - consumer should be disposed
        Should.Throw<ObjectDisposedException>(() => consumer.GetMockServerUri());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            _sut.Dispose();
            _sut.Dispose();
        });
        Assert.Null(exception);
    }

    private sealed class TestService { }
}
