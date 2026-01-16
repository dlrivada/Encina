using System.Net.Http.Json;
using Encina.Testing.Pact;

namespace Encina.IntegrationTests.Testing.Pact;

/// <summary>
/// Integration tests for EncinaPactConsumerBuilder that require actual HTTP calls to Pact mock server.
/// </summary>
[Trait("Category", "Integration")]
public sealed class EncinaPactConsumerBuilderIntegrationTests : IDisposable
{
    private readonly string _testPactDir;
    private readonly EncinaPactConsumerBuilder _sut;

    public EncinaPactConsumerBuilderIntegrationTests()
    {
        _testPactDir = Path.Combine(Path.GetTempPath(), $"pact-integration-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPactDir);
        _sut = new EncinaPactConsumerBuilder("TestConsumer", "TestProvider", _testPactDir);
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
    public async Task Verify_WithTestAction_ExecutesActionAndSetsMockServerUri()
    {
        // Arrange
        var command = new TestCreateOrderCommand(Guid.NewGuid(), "Test");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = command.OrderId });
        _sut.WithCommandExpectation(command, response);
        Uri? capturedUri = null;

        // Act - Use VerifyAsync to avoid blocking .GetAwaiter().GetResult() which can cause deadlocks
        await _sut.VerifyAsync(async uri =>
        {
            capturedUri = uri;
            // Make actual HTTP call to satisfy Pact verification
            using var client = new HttpClient { BaseAddress = uri };
            var httpResponse = await client.PostAsJsonAsync(
                "/api/commands/TestCreateOrderCommand",
                command);
            httpResponse.EnsureSuccessStatusCode();
        });

        // Assert
        capturedUri.ShouldNotBeNull();
        capturedUri!.IsAbsoluteUri.ShouldBeTrue();
        _sut.GetMockServerUri().ShouldBe(capturedUri);
    }

    [Fact]
    public async Task VerifyAsync_WithTestAction_ExecutesActionAndSetsMockServerUri()
    {
        // Arrange
        var command = new TestCreateOrderCommand(Guid.NewGuid(), "Test");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = command.OrderId });
        _sut.WithCommandExpectation(command, response);
        Uri? capturedUri = null;

        // Act
        await _sut.VerifyAsync(async uri =>
        {
            capturedUri = uri;
            // Make actual HTTP call to satisfy Pact verification
            using var client = new HttpClient { BaseAddress = uri };
            var httpResponse = await client.PostAsJsonAsync(
                "/api/commands/TestCreateOrderCommand",
                command);
            httpResponse.EnsureSuccessStatusCode();
        });

        // Assert
        capturedUri.ShouldNotBeNull();
        capturedUri!.IsAbsoluteUri.ShouldBeTrue();
        _sut.GetMockServerUri().ShouldBe(capturedUri);
    }
}
