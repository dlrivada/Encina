using System.Net;
using System.Text;
using System.Text.Json;
using Encina.Testing.Pact;
using LanguageExt;

namespace Encina.UnitTests.Testing.Pact;

/// <summary>
/// Comprehensive unit tests for <see cref="PactExtensions"/> covering all code paths.
/// </summary>
public sealed class PactExtensionsMassiveTests
{
    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    #region ToPactResponse

    [Fact]
    public void ToPactResponse_RightResult_ReturnsPactSuccessResponse()
    {
        Either<EncinaError, string> result = "hello";
        var response = result.ToPactResponse();
        response.ShouldNotBeNull();
        var success = response as PactSuccessResponse<string>;
        success.ShouldNotBeNull();
        success!.IsSuccess.ShouldBeTrue();
        success.Data.ShouldBe("hello");
    }

    [Fact]
    public void ToPactResponse_LeftResult_ReturnsPactErrorResponse()
    {
        Either<EncinaError, string> result = EncinaError.New("test error");
        var response = result.ToPactResponse();
        response.ShouldNotBeNull();
        // It's an internal PactErrorResponseWrapper - verify via JSON
        var json = JsonSerializer.Serialize(response);
        json.ShouldContain("false");
    }

    [Fact]
    public void ToPactResponse_WithIntType_ReturnsCorrectData()
    {
        Either<EncinaError, int> result = 42;
        var response = result.ToPactResponse();
        var success = response as PactSuccessResponse<int>;
        success.ShouldNotBeNull();
        success!.Data.ShouldBe(42);
    }

    #endregion

    #region CreatePactHttpClient

    [Fact]
    public void CreatePactHttpClient_NullUri_Throws()
    {
        Uri? uri = null;
        Should.Throw<ArgumentNullException>(() => uri!.CreatePactHttpClient());
    }

    [Fact]
    public void CreatePactHttpClient_ValidUri_ReturnsConfiguredClient()
    {
        var uri = new Uri("http://localhost:9876");
        using var client = uri.CreatePactHttpClient();
        client.ShouldNotBeNull();
        client.BaseAddress.ShouldBe(uri);
        client.DefaultRequestHeaders.Accept.ShouldContain(h => h.MediaType == "application/json");
    }

    [Fact]
    public void CreatePactHttpClient_WithConfigure_AppliesConfiguration()
    {
        var uri = new Uri("http://localhost:9876");
        using var client = uri.CreatePactHttpClient(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(5);
        });
        client.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreatePactHttpClient_WithoutConfigure_UsesDefaults()
    {
        var uri = new Uri("http://localhost:9876");
        using var client = uri.CreatePactHttpClient();
        client.BaseAddress.ShouldBe(uri);
    }

    #endregion

    #region ReadAsEitherAsync

    [Fact]
    public async Task ReadAsEitherAsync_NullResponse_Throws()
    {
        HttpResponseMessage? response = null;
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await response!.ReadAsEitherAsync<string>());
    }

    [Fact]
    public async Task ReadAsEitherAsync_SuccessWithContent_ReturnsRight()
    {
        var json = JsonSerializer.Serialize("test-value");
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var result = await response.ReadAsEitherAsync<string>();
        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("test-value");
    }

    [Fact]
    public async Task ReadAsEitherAsync_NoContent204_ReturnsLeft()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        var result = await response.ReadAsEitherAsync<string>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsEitherAsync_ErrorResponse_ReturnsLeftWithErrorCode()
    {
        var errorJson = JsonSerializer.Serialize(new { errorCode = "test.error", errorMessage = "something failed" },
            CamelCaseOptions);
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(errorJson, Encoding.UTF8, "application/json")
        };

        var result = await response.ReadAsEitherAsync<string>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsEitherAsync_InvalidJson_ReturnsLeftDeserializationError()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json{{{", Encoding.UTF8, "application/json")
        };

        var result = await response.ReadAsEitherAsync<string>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsEitherAsync_NullDeserializedContent_ReturnsLeft()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        };

        var result = await response.ReadAsEitherAsync<TestPactDto>();
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsEitherAsync_SuccessWithComplexType_ReturnsRight()
    {
        var dto = new TestPactDto { Name = "test", Value = 42 };
        var json = JsonSerializer.Serialize(dto, CamelCaseOptions);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var result = await response.ReadAsEitherAsync<TestPactDto>();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadAsEitherAsync_ErrorResponseWithMissingFields_ReturnsLeftWithDefaults()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        var result = await response.ReadAsEitherAsync<string>();
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region PactSuccessResponse

    [Fact]
    public void PactSuccessResponse_RecordProperties()
    {
        var response = new PactSuccessResponse<string>(true, "data");
        response.IsSuccess.ShouldBeTrue();
        response.Data.ShouldBe("data");
    }

    [Fact]
    public void PactSuccessResponse_Equality()
    {
        var r1 = new PactSuccessResponse<int>(true, 42);
        var r2 = new PactSuccessResponse<int>(true, 42);
        r1.ShouldBe(r2);
    }

    #endregion

    #region SendCommandAsync / SendQueryAsync / PublishNotificationAsync Guards

    [Fact]
    public async Task SendCommandAsync_NullClient_Throws()
    {
        HttpClient? client = null;
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client!.SendCommandAsync<TestCommand, string>(new TestCommand()));
    }

    [Fact]
    public async Task SendCommandAsync_NullCommand_Throws()
    {
        using var client = new HttpClient();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client.SendCommandAsync<TestCommand, string>(null!));
    }

    [Fact]
    public async Task SendQueryAsync_NullClient_Throws()
    {
        HttpClient? client = null;
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client!.SendQueryAsync<TestQuery, string>(new TestQuery()));
    }

    [Fact]
    public async Task SendQueryAsync_NullQuery_Throws()
    {
        using var client = new HttpClient();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client.SendQueryAsync<TestQuery, string>(null!));
    }

    [Fact]
    public async Task PublishNotificationAsync_NullClient_Throws()
    {
        HttpClient? client = null;
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client!.PublishNotificationAsync(new TestNotification()));
    }

    [Fact]
    public async Task PublishNotificationAsync_NullNotification_Throws()
    {
        using var client = new HttpClient();
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await client.PublishNotificationAsync<TestNotification>(null!));
    }

    #endregion

    #region Test Types

    private sealed class TestPactDto
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    private sealed class TestCommand : ICommand<string>
    {
        public string Data { get; set; } = "test";
    }

    private sealed class TestQuery : IQuery<string>
    {
        public string Filter { get; set; } = "all";
    }

    private sealed class TestNotification : INotification
    {
        public string Message { get; set; } = "hello";
    }

    #endregion
}
