using Encina.Testing.Pact;
using Shouldly;
using System.Text.Json;

namespace Encina.UnitTests.Testing.Pact;

public sealed class PactExtensionsTests
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void ToPactResponse_Success_ReturnsSuccessResponse()
    {
        // Arrange
        var dto = new TestOrderDto { Id = Guid.NewGuid(), Status = "Created" };
        var result = Either<EncinaError, TestOrderDto>.Right(dto);

        // Act
        var response = result.ToPactResponse();

        // Assert
        response.ShouldBeOfType<PactSuccessResponse<TestOrderDto>>();
        var successResponse = (PactSuccessResponse<TestOrderDto>)response;
        successResponse.IsSuccess.ShouldBeTrue();
        successResponse.Data.ShouldBe(dto);
    }

    [Fact]
    public void ToPactResponse_Error_ReturnsErrorResponse()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error message");
        var result = Either<EncinaError, TestOrderDto>.Left(error);

        // Act
        var response = result.ToPactResponse();

        // Assert - use consistent serialization options for round-trip
        var json = JsonSerializer.Serialize(response, s_jsonOptions);
        var errorResponse = JsonSerializer.Deserialize<PactErrorResponse>(json, s_jsonOptions);

        errorResponse.ShouldNotBeNull();
        errorResponse.IsSuccess.ShouldBeFalse();
        errorResponse.ErrorCode.ShouldBe("test.error");
        errorResponse.ErrorMessage.ShouldBe("Test error message");
    }

    [Fact]
    public void CreatePactHttpClient_NullUri_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => PactExtensions.CreatePactHttpClient(null!));
    }

    [Fact]
    public void CreatePactHttpClient_ValidUri_ReturnsConfiguredClient()
    {
        // Arrange
        var uri = new Uri("http://localhost:9292");

        // Act
        using var client = uri.CreatePactHttpClient();

        // Assert
        client.ShouldNotBeNull();
        client.BaseAddress.ShouldBe(uri);
        client.DefaultRequestHeaders.Accept.ShouldContain(a => a.MediaType == "application/json");
    }

    [Fact]
    public void CreatePactHttpClient_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var uri = new Uri("http://localhost:9292");

        // Act
        using var client = uri.CreatePactHttpClient(c =>
        {
            c.DefaultRequestHeaders.Add("X-Custom-Header", "CustomValue");
        });

        // Assert
        client.DefaultRequestHeaders.Contains("X-Custom-Header").ShouldBeTrue();
    }
}

public sealed class PactErrorResponseTests
{
    [Fact]
    public void PactErrorResponse_Properties_AreSet()
    {
        // Arrange & Act
        var response = new PactErrorResponse(false, "error.code", "Error message");

        // Assert
        response.IsSuccess.ShouldBeFalse();
        response.ErrorCode.ShouldBe("error.code");
        response.ErrorMessage.ShouldBe("Error message");
    }

    [Fact]
    public void PactSuccessResponse_Properties_AreSet()
    {
        // Arrange
        var data = new TestOrderDto { Id = Guid.NewGuid() };

        // Act
        var response = new PactSuccessResponse<TestOrderDto>(true, data);

        // Assert
        response.IsSuccess.ShouldBeTrue();
        response.Data.ShouldBe(data);
    }
}

public sealed class PactVerificationResultTests
{
    [Fact]
    public void PactVerificationResult_Success_HasNoErrors()
    {
        // Arrange & Act
        var result = new PactVerificationResult(true, [], []);

        // Assert
        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
        result.InteractionResults.ShouldBeEmpty();
    }

    [Fact]
    public void PactVerificationResult_Failure_HasErrors()
    {
        // Arrange & Act
        var result = new PactVerificationResult(
            false,
            ["Error 1", "Error 2"],
            [new InteractionVerificationResult("Test", false, "Failed")]);

        // Assert
        result.Success.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
        result.InteractionResults.Count.ShouldBe(1);
    }
}

public sealed class InteractionVerificationResultTests
{
    [Fact]
    public void InteractionVerificationResult_Success_HasNoError()
    {
        // Arrange & Act
        var result = new InteractionVerificationResult("Test interaction", true, null);

        // Assert
        result.Description.ShouldBe("Test interaction");
        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void InteractionVerificationResult_Failure_HasError()
    {
        // Arrange & Act
        var result = new InteractionVerificationResult("Test interaction", false, "Something went wrong");

        // Assert
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Something went wrong");
    }
}
