using Encina.Testing.Pact;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Encina.UnitTests.Testing.Pact;

public sealed class EncinaPactProviderVerifierTests : IDisposable
{
    private readonly string _testPactDir;
    private readonly IEncina _mockEncina;
    private readonly IServiceProvider _serviceProvider;
    private readonly EncinaPactProviderVerifier _sut;

    public EncinaPactProviderVerifierTests()
    {
        _testPactDir = Path.Combine(Path.GetTempPath(), $"pact-verifier-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPactDir);

        _mockEncina = Substitute.For<IEncina>();
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        _sut = new EncinaPactProviderVerifier(_mockEncina, _serviceProvider);
    }

    public void Dispose()
    {
        _sut.Dispose();
        if (Directory.Exists(_testPactDir))
        {
            Directory.Delete(_testPactDir, true);
        }
    }

    [Fact]
    public void Constructor_WithNullEncina_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new EncinaPactProviderVerifier(null!, _serviceProvider));
    }

    [Fact]
    public void Constructor_EncinaOnly_DoesNotThrow()
    {
        // Act
        using var verifier = new EncinaPactProviderVerifier(_mockEncina);

        // Assert
        verifier.ShouldNotBeNull();
    }

    [Fact]
    public void WithProviderName_SetsProviderName()
    {
        // Act
        var result = _sut.WithProviderName("TestProvider");

        // Assert
        result.ShouldBeSameAs(_sut);
        _sut.ProviderName.ShouldBe("TestProvider");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithProviderName_InvalidName_Throws(string? invalidName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => _sut.WithProviderName(invalidName!));
    }

    [Fact]
    public void WithProviderState_AsyncAction_ReturnsBuilder()
    {
        // Act
        var result = _sut.WithProviderState("state name", () => Task.CompletedTask);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithProviderState_InvalidStateName_Throws(string? invalidName)
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            _sut.WithProviderState(invalidName!, () => Task.CompletedTask));
    }

    [Fact]
    public void WithProviderState_NullAsyncAction_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithProviderState("state", (Func<Task>)null!));
    }

    [Fact]
    public void WithProviderState_WithParams_ReturnsBuilder()
    {
        // Act
        var result = _sut.WithProviderState("state with params", (IDictionary<string, object> _) => Task.CompletedTask);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithProviderState_SyncAction_ReturnsBuilder()
    {
        // Act
        var result = _sut.WithProviderState("sync state", () => { });

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithProviderState_NullSyncAction_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithProviderState("state", (Action)null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyAsync_InvalidPath_Throws(string? invalidPath)
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _sut.VerifyAsync(invalidPath!));
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

    [Fact]
    public void FluentChaining_Works()
    {
        // Act
        var result = _sut
            .WithProviderName("TestProvider")
            .WithProviderState("state1", () => Task.CompletedTask)
            .WithProviderState("state2", () => { })
            .WithProviderState("state3", (IDictionary<string, object> _) => Task.CompletedTask);

        // Assert
        result.ShouldBeSameAs(_sut);
        result.ProviderName.ShouldBe("TestProvider");
    }
}
