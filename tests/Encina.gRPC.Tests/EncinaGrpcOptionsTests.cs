using Encina.gRPC;
using Xunit;

namespace Encina.gRPC.Tests;

/// <summary>
/// Tests for the <see cref="EncinaGrpcOptions"/> class.
/// </summary>
public sealed class EncinaGrpcOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new EncinaGrpcOptions();

        // Assert
        options.EnableReflection.ShouldBeTrue();
        options.EnableHealthChecks.ShouldBeTrue();
        options.MaxReceiveMessageSize.ShouldBe(4 * 1024 * 1024);
        options.MaxSendMessageSize.ShouldBe(4 * 1024 * 1024);
        options.EnableLoggingInterceptor.ShouldBeTrue();
        options.DefaultDeadline.ShouldBe(TimeSpan.FromSeconds(30));
        options.EnableCompression.ShouldBeFalse();
    }

    [Fact]
    public void EnableReflection_CanBeSetToFalse()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableReflection = false;

        // Assert
        options.EnableReflection.ShouldBeFalse();
    }

    [Fact]
    public void EnableHealthChecks_CanBeSetToFalse()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableHealthChecks = false;

        // Assert
        options.EnableHealthChecks.ShouldBeFalse();
    }

    [Fact]
    public void MaxReceiveMessageSize_CanBeCustomized()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.MaxReceiveMessageSize = 16 * 1024 * 1024;

        // Assert
        options.MaxReceiveMessageSize.ShouldBe(16 * 1024 * 1024);
    }

    [Fact]
    public void MaxSendMessageSize_CanBeCustomized()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.MaxSendMessageSize = 16 * 1024 * 1024;

        // Assert
        options.MaxSendMessageSize.ShouldBe(16 * 1024 * 1024);
    }

    [Fact]
    public void EnableLoggingInterceptor_CanBeSetToFalse()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableLoggingInterceptor = false;

        // Assert
        options.EnableLoggingInterceptor.ShouldBeFalse();
    }

    [Fact]
    public void DefaultDeadline_CanBeCustomized()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.DefaultDeadline = TimeSpan.FromMinutes(5);

        // Assert
        options.DefaultDeadline.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void EnableCompression_CanBeSetToTrue()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableCompression = true;

        // Assert
        options.EnableCompression.ShouldBeTrue();
    }
}
