namespace Encina.Testing.WireMock.Tests;

/// <summary>
/// Unit tests for <see cref="ReceivedRequest"/>.
/// </summary>
public sealed class ReceivedRequestTests
{
    [Fact]
    public void ReceivedRequest_ShouldStoreAllProperties()
    {
        // Arrange
        var path = "/api/test";
        var method = "GET";
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };
        var body = """{"key": "value"}""";
        var timestamp = DateTime.UtcNow;

        // Act
        var request = new ReceivedRequest(path, method, headers, body, timestamp);

        // Assert
        request.Path.ShouldBe(path);
        request.Method.ShouldBe(method);
        request.Headers.ShouldBe(headers);
        request.Body.ShouldBe(body);
        request.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void ReceivedRequest_Equality_ShouldWork()
    {
        // Arrange
        var headers = new Dictionary<string, string> { ["Accept"] = "application/json" };
        var timestamp = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var request1 = new ReceivedRequest("/api/test", "POST", headers, "{}", timestamp);
        var request2 = new ReceivedRequest("/api/test", "POST", headers, "{}", timestamp);

        // Act & Assert
        request1.ShouldBe(request2);
        (request1 == request2).ShouldBeTrue();
    }

    [Fact]
    public void ReceivedRequest_Inequality_ShouldWork()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var timestamp = DateTime.UtcNow;

        var request1 = new ReceivedRequest("/api/a", "GET", headers, "", timestamp);
        var request2 = new ReceivedRequest("/api/b", "GET", headers, "", timestamp);

        // Act & Assert
        request1.ShouldNotBe(request2);
        (request1 != request2).ShouldBeTrue();
    }

    [Fact]
    public void ReceivedRequest_Deconstruction_ShouldWork()
    {
        // Arrange
        var headers = new Dictionary<string, string>();
        var timestamp = DateTime.UtcNow;
        var request = new ReceivedRequest("/api/test", "DELETE", headers, "body", timestamp);

        // Act
        var (path, method, h, body, ts) = request;

        // Assert
        path.ShouldBe("/api/test");
        method.ShouldBe("DELETE");
        h.ShouldBe(headers);
        body.ShouldBe("body");
        ts.ShouldBe(timestamp);
    }

    [Fact]
    public void ReceivedRequest_ToString_ShouldContainRelevantInfo()
    {
        // Arrange
        var request = new ReceivedRequest(
            "/api/users",
            "GET",
            new Dictionary<string, string>(),
            "",
            DateTime.UtcNow);

        // Act
        var str = request.ToString();

        // Assert
        str.ShouldContain("ReceivedRequest");
        str.ShouldContain("/api/users");
        str.ShouldContain("GET");
    }
}
