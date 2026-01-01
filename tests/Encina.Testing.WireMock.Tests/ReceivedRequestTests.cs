namespace Encina.Testing.WireMock.Tests;

/// <summary>
/// Unit tests for <see cref="ReceivedRequest"/>.
/// </summary>
public sealed class ReceivedRequestTests
{
    private static Dictionary<string, IReadOnlyList<string>> CreateHeaders(params (string Key, string Value)[] headers)
    {
        var dict = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var (key, value) in headers)
        {
            if (dict.TryGetValue(key, out var existing))
            {
                dict[key] = existing.Concat([value]).ToList();
            }
            else
            {
                dict[key] = new List<string> { value };
            }
        }
        return dict;
    }

    [Fact]
    public void ReceivedRequest_ShouldStoreAllProperties()
    {
        // Arrange
        var path = "/api/test";
        var method = "GET";
        var headers = CreateHeaders(("Content-Type", "application/json"));
        var body = """{"key": "value"}""";
        var timestamp = DateTime.UtcNow;

        // Act
        var request = new ReceivedRequest(path, method, headers, body, timestamp);

        // Assert
        request.Path.ShouldBe(path);
        request.Method.ShouldBe(method);
        request.Headers.ShouldContainKey("Content-Type");
        request.Headers["Content-Type"].ShouldContain("application/json");
        request.Body.ShouldBe(body);
        request.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void ReceivedRequest_Equality_ShouldWork()
    {
        // Arrange
        var headers = CreateHeaders(("Accept", "application/json"));
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
        var headers = CreateHeaders();
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
        var headers = CreateHeaders();
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
            CreateHeaders(),
            "",
            DateTime.UtcNow);

        // Act
        var str = request.ToString();

        // Assert
        str.ShouldContain("ReceivedRequest");
        str.ShouldContain("/api/users");
        str.ShouldContain("GET");
    }

    [Fact]
    public void ReceivedRequest_Headers_ShouldSupportMultipleValues()
    {
        // Arrange
        var headers = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Accept"] = new List<string> { "application/json", "text/plain" },
            ["X-Custom"] = new List<string> { "value1", "value2", "value3" }
        };

        // Act
        var request = new ReceivedRequest("/api/test", "GET", headers, "", DateTime.UtcNow);

        // Assert
        request.Headers["Accept"].Count.ShouldBe(2);
        request.Headers["Accept"].ShouldContain("application/json");
        request.Headers["Accept"].ShouldContain("text/plain");
        request.Headers["X-Custom"].Count.ShouldBe(3);
    }
}
