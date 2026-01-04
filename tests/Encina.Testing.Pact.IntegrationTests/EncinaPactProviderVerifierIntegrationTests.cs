using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Encina.Testing.Pact.IntegrationTests;

/// <summary>
/// Integration tests for EncinaPactProviderVerifier that require file I/O operations.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EncinaPactProviderVerifierIntegrationTests : IDisposable
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _testPactDir;
    private readonly IEncina _mockEncina;
    private readonly IServiceProvider _serviceProvider;
    private readonly EncinaPactProviderVerifier _sut;

    public EncinaPactProviderVerifierIntegrationTests()
    {
        _testPactDir = Path.Combine(Path.GetTempPath(), $"pact-verifier-integration-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPactDir);

        _mockEncina = Substitute.For<IEncina>();
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        _sut = new EncinaPactProviderVerifier(_mockEncina, _serviceProvider);
    }

    public void Dispose()
    {
        _sut.Dispose();

        // Cleanup test directory - suppress exceptions to avoid masking test failures
        try
        {
            if (Directory.Exists(_testPactDir))
            {
                Directory.Delete(_testPactDir, true);
            }
        }
        catch (IOException)
        {
            // Directory may be locked by another process; ignore cleanup failure
        }
        catch (UnauthorizedAccessException)
        {
            // Insufficient permissions; ignore cleanup failure
        }
    }

    [Fact]
    public async Task VerifyAsync_NonExistentFile_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testPactDir, "nonexistent.json");

        // Act
        var result = await _sut.VerifyAsync(nonExistentPath);

        // Assert
        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.Contains("not found"));
    }

    [Fact]
    public async Task VerifyAsync_InvalidJsonFile_ReturnsFailure()
    {
        // Arrange
        var invalidJsonPath = Path.Combine(_testPactDir, "invalid.json");
        await File.WriteAllTextAsync(invalidJsonPath, "not valid json");

        // Act
        var result = await _sut.VerifyAsync(invalidJsonPath);

        // Assert
        result.Success.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_EmptyPactFile_ReturnsSuccess()
    {
        // Arrange
        var emptyPactPath = Path.Combine(_testPactDir, "empty.json");
        var emptyPact = new { Consumer = new { Name = "Consumer" }, Provider = new { Name = "Provider" }, Interactions = Array.Empty<object>() };
        await File.WriteAllTextAsync(emptyPactPath, JsonSerializer.Serialize(emptyPact));

        // Act
        var result = await _sut.VerifyAsync(emptyPactPath);

        // Assert
        result.Success.ShouldBeTrue();
        result.InteractionResults.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_WithInteraction_VerifiesSuccessfully()
    {
        // Arrange
        var pactPath = Path.Combine(_testPactDir, "test-pact.json");
        var pact = new
        {
            Consumer = new { Name = "Consumer" },
            Provider = new { Name = "Provider" },
            Interactions = new[]
            {
                new
                {
                    Description = "Test interaction",
                    Request = new { Method = "POST", Path = "/api/commands/TestCommand" },
                    Response = new { Status = 200, Body = new { IsSuccess = true } }
                }
            }
        };
        await File.WriteAllTextAsync(pactPath, JsonSerializer.Serialize(pact, CamelCaseOptions));

        // Act
        var result = await _sut.VerifyAsync(pactPath);

        // Assert
        result.Success.ShouldBeTrue();
        result.InteractionResults.Count.ShouldBe(1);
    }

    [Fact]
    public async Task VerifyAsync_WithProviderState_ExecutesStateHandler()
    {
        // Arrange
        var stateExecuted = false;
        _sut.WithProviderState("test state", () =>
        {
            stateExecuted = true;
            return Task.CompletedTask;
        });

        var pactPath = Path.Combine(_testPactDir, "state-pact.json");
        var pact = new
        {
            Consumer = new { Name = "Consumer" },
            Provider = new { Name = "Provider" },
            Interactions = new[]
            {
                new
                {
                    Description = "Test interaction",
                    ProviderState = "test state",
                    Request = new { Method = "POST", Path = "/api/commands/TestCommand" },
                    Response = new { Status = 200 }
                }
            }
        };
        await File.WriteAllTextAsync(pactPath, JsonSerializer.Serialize(pact, CamelCaseOptions));

        // Act
        await _sut.VerifyAsync(pactPath);

        // Assert
        stateExecuted.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyAsync_AfterDispose_Throws()
    {
        // Arrange
        _sut.Dispose();
        var pactPath = Path.Combine(_testPactDir, "test.json");
        await File.WriteAllTextAsync(pactPath, "{}");

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(() => _sut.VerifyAsync(pactPath));
    }
}
