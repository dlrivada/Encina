using System.Text.Json;
using Encina.SignalR;
using Xunit;

namespace Encina.SignalR.Tests;

/// <summary>
/// Tests for the <see cref="SignalROptions"/> class.
/// </summary>
public sealed class SignalROptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var options = new SignalROptions();

        // Assert
        options.EnableNotificationBroadcast.ShouldBeTrue();
        options.AuthorizationPolicy.ShouldBeNull();
        options.IncludeDetailedErrors.ShouldBeFalse();
        options.JsonSerializerOptions.ShouldBeNull();
    }

    [Fact]
    public void EnableNotificationBroadcast_CanBeSet()
    {
        // Arrange
        var options = new SignalROptions();

        // Act
        options.EnableNotificationBroadcast = false;

        // Assert
        options.EnableNotificationBroadcast.ShouldBeFalse();
    }

    [Fact]
    public void AuthorizationPolicy_CanBeSet()
    {
        // Arrange
        var options = new SignalROptions();
        const string policy = "RequireAdmin";

        // Act
        options.AuthorizationPolicy = policy;

        // Assert
        options.AuthorizationPolicy.ShouldBe(policy);
    }

    [Fact]
    public void IncludeDetailedErrors_CanBeSet()
    {
        // Arrange
        var options = new SignalROptions();

        // Act
        options.IncludeDetailedErrors = true;

        // Assert
        options.IncludeDetailedErrors.ShouldBeTrue();
    }

    [Fact]
    public void JsonSerializerOptions_CanBeSet()
    {
        // Arrange
        var options = new SignalROptions();
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Act
        options.JsonSerializerOptions = jsonOptions;

        // Assert
        options.JsonSerializerOptions.ShouldBeSameAs(jsonOptions);
        options.JsonSerializerOptions.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
    }
}
