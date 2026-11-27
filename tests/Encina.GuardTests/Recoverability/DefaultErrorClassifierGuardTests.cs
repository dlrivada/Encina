using Encina.Recoverability;

namespace Encina.GuardTests.Recoverability;

/// <summary>
/// Guard tests for <see cref="IErrorClassifier.Classify"/> as implemented by <see cref="DefaultErrorClassifier"/>:
/// the error is a struct and the exception is nullable, so there is no null check; a null exception is a valid input.
/// </summary>
public class DefaultErrorClassifierGuardTests
{
    private readonly DefaultErrorClassifier _sut = new();

    /// <summary>
    /// A null exception is valid: the error code alone decides.
    /// </summary>
    [Fact]
    public void Classify_NullException_UsesErrorCode()
    {
        var error = EncinaErrors.Create(EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden, "denied");

        _sut.Classify(error, null).ShouldBe(ErrorClassification.Permanent);
    }

    /// <summary>
    /// A transient error code is transient with a null exception.
    /// </summary>
    [Fact]
    public void Classify_TimeoutCode_ReturnsTransient()
    {
        var error = EncinaErrors.Create(EncinaErrorCodes.Timeout, ErrorCategory.Timeout, "slow");

        _sut.Classify(error, null).ShouldBe(ErrorClassification.Transient);
    }

    /// <summary>
    /// An exception argument classifies the error by its type.
    /// </summary>
    [Fact]
    public void Classify_TimeoutException_ReturnsTransient()
    {
        var error = EncinaErrors.Create("test.unknown", ErrorCategory.Internal, "x");

        _sut.Classify(error, new TimeoutException()).ShouldBe(ErrorClassification.Transient);
        _sut.Classify(error, new FormatException()).ShouldBe(ErrorClassification.Permanent);
    }

    /// <summary>
    /// A connection-pool exhaustion message is transient although the exception type is permanent.
    /// </summary>
    [Fact]
    public void Classify_PoolExhaustion_ReturnsTransient()
    {
        var error = EncinaErrors.Create("test.unknown", ErrorCategory.Internal, "x");

        _sut.Classify(error, new InvalidOperationException("The max pool size was reached.")).ShouldBe(ErrorClassification.Transient);
    }

    /// <summary>
    /// An error without code, cause or recognizable words is unknown.
    /// </summary>
    [Fact]
    public void Classify_NothingRecognizable_ReturnsUnknown()
    {
        var error = EncinaErrors.Create("test.unknown", ErrorCategory.Internal, "zzz");

        _sut.Classify(error, null).ShouldBe(ErrorClassification.Unknown);
    }

    /// <summary>
    /// The message patterns decide when nothing else does.
    /// </summary>
    [Fact]
    public void Classify_MessagePatterns_DecideLast()
    {
        _sut.Classify(EncinaErrors.Create("test.a", ErrorCategory.Internal, "request is invalid"), null).ShouldBe(ErrorClassification.Permanent);
        _sut.Classify(EncinaErrors.Create("test.b", ErrorCategory.Internal, "service unavailable"), null).ShouldBe(ErrorClassification.Transient);
    }

    /// <summary>
    /// A declared transience in the details wins over inference.
    /// </summary>
    [Fact]
    public void Classify_DeclaredTransience_WinsOverCode()
    {
        var error = EncinaErrors.Create(
            EncinaErrorCodes.AuthorizationForbidden, ErrorCategory.Forbidden, "denied",
            details: new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = true });

        _sut.Classify(error, null).ShouldBe(ErrorClassification.Transient);
    }

    /// <summary>
    /// An HTTP exception is classified by its status code.
    /// </summary>
    [Fact]
    public void Classify_HttpRequestException_UsesStatusCode()
    {
        var error = EncinaErrors.Create("test.http", ErrorCategory.Internal, "x");

        _sut.Classify(error, new HttpRequestException("x", null, System.Net.HttpStatusCode.NotFound)).ShouldBe(ErrorClassification.Permanent);
        _sut.Classify(error, new HttpRequestException("x", null, System.Net.HttpStatusCode.TooManyRequests)).ShouldBe(ErrorClassification.Transient);
        _sut.Classify(error, new HttpRequestException("network")).ShouldBe(ErrorClassification.Transient);
    }
}
