#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using System.Net.Sockets;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class SecretsTransientErrorDetectorTests
{
    #region IsTransient

    [Fact]
    public void IsTransient_ProviderUnavailable_Should_ReturnTrue()
    {
        var error = SecretsErrors.ProviderUnavailable("test-provider");

        var result = SecretsTransientErrorDetector.IsTransient(error);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransient_NotFound_Should_ReturnFalse()
    {
        var error = SecretsErrors.NotFound("test-secret");

        var result = SecretsTransientErrorDetector.IsTransient(error);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransient_AccessDenied_Should_ReturnFalse()
    {
        var error = SecretsErrors.AccessDenied("test-secret");

        var result = SecretsTransientErrorDetector.IsTransient(error);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DeserializationFailed_Should_ReturnFalse()
    {
        var error = SecretsErrors.DeserializationFailed("test-secret", typeof(string));

        var result = SecretsTransientErrorDetector.IsTransient(error);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransient_ErrorWithoutCode_Should_ReturnFalse()
    {
        var error = EncinaError.New("Some error without a code");

        var result = SecretsTransientErrorDetector.IsTransient(error);

        result.Should().BeFalse();
    }

    #endregion

    #region IsTransientException

    [Fact]
    public void IsTransientException_HttpRequestException_Should_ReturnTrue()
    {
        var exception = new HttpRequestException("Connection refused");

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientException_TimeoutException_Should_ReturnTrue()
    {
        var exception = new TimeoutException("Operation timed out");

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientException_IOException_Should_ReturnTrue()
    {
        var exception = new IOException("Network stream error");

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientException_SocketException_Should_ReturnTrue()
    {
        var exception = new SocketException((int)SocketError.ConnectionRefused);

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientException_TransientSecretException_Should_ReturnTrue()
    {
        var error = SecretsErrors.ProviderUnavailable("test-provider");
        var exception = new TransientSecretException(error);

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientException_TaskCanceledException_WithInnerTimeoutException_Should_ReturnTrue()
    {
        var inner = new TimeoutException("Timed out");
        var exception = new TaskCanceledException("Task was cancelled", inner);

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientException_TaskCanceledException_WithoutInnerTimeoutException_Should_ReturnFalse()
    {
        var exception = new TaskCanceledException("Task was cancelled");

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransientException_InvalidOperationException_Should_ReturnFalse()
    {
        var exception = new InvalidOperationException("Not a transient error");

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransientException_ArgumentException_Should_ReturnFalse()
    {
        var exception = new ArgumentException("Bad argument");

        var result = SecretsTransientErrorDetector.IsTransientException(exception);

        result.Should().BeFalse();
    }

    #endregion
}
