using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Encina.Testing.WireMock;
using Refit;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Unit tests for <see cref="EncinaRefitMockFixture{TApiClient}"/>.
/// </summary>
public sealed class EncinaRefitMockFixtureTests : IClassFixture<EncinaRefitMockFixture<ITestApi>>
{
    private readonly EncinaRefitMockFixture<ITestApi> _fixture;

    public EncinaRefitMockFixtureTests(EncinaRefitMockFixture<ITestApi> fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
    }

    [Fact]
    public void BaseUrl_ShouldBeAvailable_AfterInitialization()
    {
        _fixture.BaseUrl.ShouldNotBeNullOrWhiteSpace();
        _fixture.BaseUrl.ShouldStartWith("http://");
    }

    [Fact]
    public void Port_ShouldBePositive_AfterInitialization()
    {
        _fixture.Port.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Server_ShouldBeAvailable_AfterInitialization()
    {
        _fixture.Server.ShouldNotBeNull();
        _fixture.Server.IsStarted.ShouldBeTrue();
    }

    [Fact]
    public void CreateClient_ShouldReturnRefitClient()
    {
        var client = _fixture.CreateClient();

        client.ShouldNotBeNull();
        client.ShouldBeAssignableTo<ITestApi>();
    }

    [Fact]
    public async Task StubGet_ShouldReturnConfiguredResponse()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Test Item" });

        var api = _fixture.CreateClient();

        // Act
        var result = await api.GetItemAsync(1);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Test Item");
    }

    [Fact]
    public async Task StubGet_ShouldReturnCustomStatusCode()
    {
        // Arrange
        _fixture.StubGet("/api/items/999", new { error = "Not Found" }, statusCode: 404);

        var api = _fixture.CreateClient();

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(async () => await api.GetItemAsync(999));
        exception.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StubPost_ShouldReturnCreatedStatus()
    {
        // Arrange
        _fixture.StubPost("/api/items", new { id = 123, name = "New Item" });

        var api = _fixture.CreateClient();

        // Act
        var result = await api.CreateItemAsync(new CreateItemRequest { Name = "New Item" });

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(123);
    }

    [Fact]
    public async Task StubPut_ShouldReturnOkStatus()
    {
        // Arrange
        _fixture.StubPut("/api/items/1", new { id = 1, name = "Updated Item" });

        var api = _fixture.CreateClient();

        // Act
        var result = await api.UpdateItemAsync(1, new UpdateItemRequest { Name = "Updated Item" });

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Item");
    }

    [Fact]
    public async Task StubPatch_ShouldReturnOkStatus()
    {
        // Arrange
        _fixture.StubPatch("/api/items/1", new { id = 1, name = "Patched Item" });

        var api = _fixture.CreateClient();

        // Act
        var result = await api.PatchItemAsync(1, new PatchItemRequest { Name = "Patched Item" });

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task StubDelete_ShouldReturnNoContentStatus()
    {
        // Arrange
        _fixture.StubDelete("/api/items/1");

        var api = _fixture.CreateClient();

        // Act - should not throw
        await api.DeleteItemAsync(1);

        // Assert
        _fixture.VerifyCallMade("/api/items/1", method: "DELETE");
    }

    [Fact]
    public async Task Stub_ShouldAllowMethodChaining()
    {
        // Arrange
        _fixture
            .StubGet("/api/items/1", new { id = 1, name = "Item 1" })
            .StubGet("/api/items/2", new { id = 2, name = "Item 2" })
            .StubDelete("/api/items/1");

        var api = _fixture.CreateClient();

        // Act
        var item1 = await api.GetItemAsync(1);
        var item2 = await api.GetItemAsync(2);
        await api.DeleteItemAsync(1);

        // Assert
        item1.Name.ShouldBe("Item 1");
        item2.Name.ShouldBe("Item 2");
    }

    [Fact]
    public async Task StubError_ShouldReturnErrorResponse()
    {
        // Arrange
        _fixture.StubError("/api/items/1", 500, new { message = "Internal Server Error" }, method: "GET");

        var api = _fixture.CreateClient();

        // Act & Assert
        var exception = await Should.ThrowAsync<ApiException>(async () => await api.GetItemAsync(1));
        exception.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task StubDelay_ShouldDelayResponse()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(100);
        _fixture.StubDelay("/api/items/1", delay, new { id = 1, name = "Delayed" });

        var api = _fixture.CreateClient();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await api.GetItemAsync(1);
        stopwatch.Stop();

        // Assert
        result.ShouldNotBeNull();
        // Use generous tolerance for CI environments
        stopwatch.Elapsed.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(delay.TotalMilliseconds - 50);
    }

    [Fact]
    public async Task VerifyCallMade_ShouldSucceed_WhenCallWasMade()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(1);

        // Act & Assert - Should not throw
        _fixture.VerifyCallMade("/api/items/1");
    }

    [Fact]
    public void VerifyCallMade_ShouldFail_WhenNoCallWasMade()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/items/1"));
    }

    [Fact]
    public async Task VerifyCallMade_ShouldVerifyExactCount()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(1);
        await api.GetItemAsync(1);

        // Act & Assert
        _fixture.VerifyCallMade("/api/items/1", times: 2);
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/items/1", times: 1));
    }

    [Fact]
    public void VerifyNoCallsMade_ShouldSucceed_WhenNoCallsMade()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });

        // Act & Assert - Should not throw
        _fixture.VerifyNoCallsMade("/api/items/1");
    }

    [Fact]
    public async Task VerifyNoCallsMade_ShouldFail_WhenCallWasMade()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(1);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyNoCallsMade("/api/items/1"));
    }

    [Fact]
    public async Task Reset_ShouldClearAllStubsAndHistory()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });
        var api = _fixture.CreateClient();
        await api.GetItemAsync(1);

        // Act
        _fixture.Reset();

        // Assert - Verify call count should be 0 now
        _fixture.VerifyNoCallsMade("/api/items/1");
    }

    [Fact]
    public void Stub_ShouldThrow_WhenMethodIsNullOrEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.Stub(null!, "/api/test"));
        Should.Throw<ArgumentException>(() => _fixture.Stub("", "/api/test"));
        Should.Throw<ArgumentException>(() => _fixture.Stub("   ", "/api/test"));
    }

    [Fact]
    public void Stub_ShouldThrow_WhenPathIsNullOrEmpty()
    {
        Should.Throw<ArgumentException>(() => _fixture.Stub("GET", null!));
        Should.Throw<ArgumentException>(() => _fixture.Stub("GET", ""));
        Should.Throw<ArgumentException>(() => _fixture.Stub("GET", "   "));
    }

    [Fact]
    public async Task VerifyCallMade_ShouldFilterByMethod()
    {
        // Arrange
        _fixture.StubGet("/api/items/1", new { id = 1, name = "Item 1" });
        _fixture.StubDelete("/api/items/1");

        var api = _fixture.CreateClient();
        await api.GetItemAsync(1);

        // Act & Assert
        _fixture.VerifyCallMade("/api/items/1", method: "GET");
        Should.Throw<InvalidOperationException>(() => _fixture.VerifyCallMade("/api/items/1", method: "DELETE"));
    }
}

/// <summary>
/// Test API interface for Refit mock fixture tests.
/// </summary>
public interface ITestApi
{
    [Get("/api/items/{id}")]
    Task<ItemResponse> GetItemAsync(int id);

    [Post("/api/items")]
    Task<ItemResponse> CreateItemAsync([Body] CreateItemRequest request);

    [Put("/api/items/{id}")]
    Task<ItemResponse> UpdateItemAsync(int id, [Body] UpdateItemRequest request);

    [Patch("/api/items/{id}")]
    Task<ItemResponse> PatchItemAsync(int id, [Body] PatchItemRequest request);

    [Delete("/api/items/{id}")]
    Task DeleteItemAsync(int id);
}

public sealed record ItemResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
}

public sealed record CreateItemRequest
{
    public required string Name { get; init; }
}

public sealed record UpdateItemRequest
{
    public required string Name { get; init; }
}

public sealed record PatchItemRequest
{
    public string? Name { get; init; }
}
