using System.Net;
using FluentAssertions;
using LanguageExt;
using Xunit;

namespace Encina.AwsLambda.ContractTests;

/// <summary>
/// Contract tests to verify API Gateway response extension behavior contracts.
/// </summary>
public class ApiGatewayResponseContractTests
{
    [Fact]
    public void ToApiGatewayResponse_Success_Returns200WithJsonBody()
    {
        // Arrange
        var result = Either<EncinaError, TestResult>.Right(new TestResult { Id = 1, Name = "Test" });

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert - Contract: Success returns 200 with JSON body
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Body.Should().Contain("\"id\":1");
        response.Body.Should().Contain("\"name\":\"Test\"");
        response.Headers.Should().ContainKey("Content-Type");
        response.Headers["Content-Type"].Should().Be("application/json");
    }

    [Fact]
    public void ToApiGatewayResponse_Error_ReturnsProblemDetailsFormat()
    {
        // Arrange
        var error = EncinaErrors.Create("validation.invalid_input", "Input is invalid");
        var result = Either<EncinaError, TestResult>.Left(error);

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert - Contract: Errors return RFC 7807 Problem Details format
        response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        response.Body.Should().Contain("\"title\":");
        response.Body.Should().Contain("\"status\":");
        response.Body.Should().Contain("\"detail\":");
        response.Headers.Should().ContainKey("Content-Type");
        response.Headers["Content-Type"].Should().Be("application/problem+json");
    }

    [Fact]
    public void ToCreatedResponse_Success_Returns201WithLocationHeader()
    {
        // Arrange
        var result = Either<EncinaError, TestResult>.Right(new TestResult { Id = 42, Name = "Created" });

        // Act
        var response = result.ToCreatedResponse(r => $"/api/resources/{r.Id}");

        // Assert - Contract: Created returns 201 with Location header
        response.StatusCode.Should().Be((int)HttpStatusCode.Created);
        response.Headers.Should().ContainKey("Location");
        response.Headers["Location"].Should().Be("/api/resources/42");
    }

    [Fact]
    public void ToNoContentResponse_Success_Returns204WithEmptyOrNullBody()
    {
        // Arrange
        var result = Either<EncinaError, Unit>.Right(Unit.Default);

        // Act
        var response = result.ToNoContentResponse();

        // Assert - Contract: NoContent returns 204 with empty or null body
        response.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        response.Body.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ToHttpApiResponse_Success_Returns200WithJsonBody()
    {
        // Arrange
        var result = Either<EncinaError, TestResult>.Right(new TestResult { Id = 1, Name = "Test" });

        // Act
        var response = result.ToHttpApiResponse();

        // Assert - Contract: HTTP API V2 returns proper response format
        response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        response.Body.Should().Contain("\"id\":1");
        response.Headers.Should().ContainKey("Content-Type");
        response.Headers["Content-Type"].Should().Be("application/json");
    }

    [Theory]
    [InlineData("validation.field_required", HttpStatusCode.BadRequest)]
    [InlineData("authorization.unauthenticated", HttpStatusCode.Unauthorized)]
    [InlineData("authorization.insufficient_permissions", HttpStatusCode.Forbidden)]
    [InlineData("resource.not_found", HttpStatusCode.NotFound)]
    [InlineData("entity.conflict", HttpStatusCode.Conflict)]
    [InlineData("entity.already_exists", HttpStatusCode.Conflict)]
    [InlineData("entity.duplicate", HttpStatusCode.Conflict)]
    [InlineData("unknown.error", HttpStatusCode.InternalServerError)]
    public void ErrorCodeMapping_ReturnsExpectedStatusCode(string errorCode, HttpStatusCode expectedStatus)
    {
        // Arrange
        var error = EncinaErrors.Create(errorCode, "Test error");
        var result = Either<EncinaError, TestResult>.Left(error);

        // Act
        var response = result.ToApiGatewayResponse();

        // Assert - Contract: Error codes map to specific HTTP status codes
        response.StatusCode.Should().Be((int)expectedStatus);
    }

    private sealed class TestResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
