using Encina.gRPC;
using Shouldly;
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

    #region Boolean Property Tests

    [Fact]
    public void EnableReflection_CanBeDisabled()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableReflection = false;

        // Assert
        options.EnableReflection.ShouldBeFalse();
    }

    [Fact]
    public void EnableHealthChecks_CanBeDisabled()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableHealthChecks = false;

        // Assert
        options.EnableHealthChecks.ShouldBeFalse();
    }

    [Fact]
    public void EnableLoggingInterceptor_CanBeDisabled()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableLoggingInterceptor = false;

        // Assert
        options.EnableLoggingInterceptor.ShouldBeFalse();
    }

    [Fact]
    public void EnableCompression_CanBeEnabled()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.EnableCompression = true;

        // Assert
        options.EnableCompression.ShouldBeTrue();
    }

    #endregion

    #region Message Size Property Tests

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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void MaxReceiveMessageSize_AcceptsBoundaryValues(int value)
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.MaxReceiveMessageSize = value;

        // Assert - property accepts any int value (validation deferred to gRPC runtime)
        options.MaxReceiveMessageSize.ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void MaxSendMessageSize_AcceptsBoundaryValues(int value)
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.MaxSendMessageSize = value;

        // Assert - property accepts any int value (validation deferred to gRPC runtime)
        options.MaxSendMessageSize.ShouldBe(value);
    }

    #endregion

    #region DefaultDeadline Property Tests

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
    public void DefaultDeadline_Zero_AcceptsValue()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.DefaultDeadline = TimeSpan.Zero;

        // Assert - property accepts zero (may mean no deadline)
        options.DefaultDeadline.ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void DefaultDeadline_Negative_AcceptsValue(int milliseconds)
    {
        // Arrange
        var options = new EncinaGrpcOptions();
        var negativeSpan = TimeSpan.FromMilliseconds(milliseconds);

        // Act
        options.DefaultDeadline = negativeSpan;

        // Assert - property accepts negative values (validation deferred to gRPC runtime)
        options.DefaultDeadline.ShouldBe(negativeSpan);
    }

    [Fact]
    public void DefaultDeadline_MaxValue_AcceptsValue()
    {
        // Arrange
        var options = new EncinaGrpcOptions();

        // Act
        options.DefaultDeadline = TimeSpan.MaxValue;

        // Assert
        options.DefaultDeadline.ShouldBe(TimeSpan.MaxValue);
    }

    #endregion
}
