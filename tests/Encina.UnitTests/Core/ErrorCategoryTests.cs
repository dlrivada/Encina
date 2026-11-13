using Shouldly;

namespace Encina.UnitTests.Core;

/// <summary>
/// Unit tests of the error category contract (ADR-062, #3545): the categorized factories, the category read back by
/// <see cref="EncinaErrorExtensions.GetCategory(EncinaError)"/>, the stable enum values and the metadata keys.
/// </summary>
public sealed class ErrorCategoryTests
{
    [Fact]
    public void Create_WithCategory_KeepsCodeCategoryMessageExceptionAndDetails()
    {
        var cause = new TimeoutException("db timeout");
        var details = new Dictionary<string, object?> { ["orderId"] = 42 };

        var error = EncinaErrors.Create("orders.not_found", ErrorCategory.NotFound, "Order 42 was not found.", cause, details);

        error.GetCategory().ShouldBe(ErrorCategory.NotFound);
        error.GetCode().IfNone(string.Empty).ShouldBe("orders.not_found");
        error.Message.ShouldBe("Order 42 was not found.");
        error.GetCause().IfNone(() => throw new InvalidOperationException("expected a cause")).ShouldBeSameAs(cause);
        error.GetDetails()["orderId"].ShouldBe(42);
    }

    [Fact]
    public void Create_WithCategory_WinsOverTheCodeText()
    {
        // The code text alone would infer NotFound; the declared category is returned as is.
        var error = EncinaErrors.Create("orders.not_found", ErrorCategory.Conflict, "Order changed.");

        error.GetCategory().ShouldBe(ErrorCategory.Conflict);
    }

    [Fact]
    public void Create_WithInternalCategory_IsNotReplacedByTheInference()
    {
        // Internal is 0; an explicit Internal must not be mistaken for "no category".
        var error = EncinaErrors.Create("secrets.not_found", ErrorCategory.Internal, "Secret lookup failed.");

        error.GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void FromException_WithCategory_UsesTheExceptionMessageByDefault()
    {
        var cause = new InvalidOperationException("upstream broke");

        var error = EncinaErrors.FromException("payments.upstream", ErrorCategory.UpstreamFailure, cause);

        error.GetCategory().ShouldBe(ErrorCategory.UpstreamFailure);
        error.GetCode().IfNone(string.Empty).ShouldBe("payments.upstream");
        error.Message.ShouldBe("upstream broke");
        error.GetCause().IfNone(() => throw new InvalidOperationException("expected a cause")).ShouldBeSameAs(cause);
    }

    [Fact]
    public void FromException_WithCategory_UsesTheMessageOverrideAndDetails()
    {
        var details = new Dictionary<string, object?> { [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(30) };

        var error = EncinaErrors.FromException(
            "payments.unavailable",
            ErrorCategory.Unavailable,
            new HttpRequestException("socket closed"),
            "The payment service is unavailable.",
            details);

        error.GetCategory().ShouldBe(ErrorCategory.Unavailable);
        error.Message.ShouldBe("The payment service is unavailable.");
        error.GetDetails()[EncinaErrorMetadataKeys.RetryAfter].ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void FromException_WithoutCategory_InfersItFromTheCode()
    {
        var error = EncinaErrors.FromException("orders.duplicate", new InvalidOperationException("dup"));

        error.GetCategory().ShouldBe(ErrorCategory.AlreadyExists);
    }

    [Theory]
    [InlineData("validation.required", ErrorCategory.Validation)]
    [InlineData("orders.not_found", ErrorCategory.NotFound)]
    [InlineData("orders.conflict", ErrorCategory.Conflict)]
    [InlineData("store.failure", ErrorCategory.Internal)]
    public void Create_WithoutCategory_InfersItFromTheCode(string code, ErrorCategory expected)
    {
        EncinaErrors.Create(code, "message").GetCategory().ShouldBe(expected);
    }

    [Fact]
    public void GetCategory_ErrorWithoutCode_IsInternal()
    {
        EncinaError.New("Patient record for 'validation.x' could not be read").GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void GetCategory_ErrorFromAPlainException_IsInternal()
    {
        EncinaError.New(new InvalidOperationException("orders.not_found")).GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void GetCategory_DefaultError_IsInternal()
    {
        default(EncinaError).GetCategory().ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void BuiltInFactories_CarryTheirCategory()
    {
        EncinaErrors.Unknown.GetCategory().ShouldBe(ErrorCategory.Internal);
        EncinaErrors.Forbidden("Admin").GetCategory().ShouldBe(ErrorCategory.Forbidden);
        EncinaErrors.Forbidden().GetCategory().ShouldBe(ErrorCategory.Forbidden);
        EncinaErrors.Unauthorized().GetCategory().ShouldBe(ErrorCategory.Forbidden);
        EncinaErrors.Unauthenticated().GetCategory().ShouldBe(ErrorCategory.Unauthenticated);
    }

    [Theory]
    [InlineData(ErrorCategory.Internal, 0)]
    [InlineData(ErrorCategory.Validation, 1)]
    [InlineData(ErrorCategory.InvalidRequest, 2)]
    [InlineData(ErrorCategory.Unauthenticated, 3)]
    [InlineData(ErrorCategory.Forbidden, 4)]
    [InlineData(ErrorCategory.NotFound, 5)]
    [InlineData(ErrorCategory.Conflict, 6)]
    [InlineData(ErrorCategory.AlreadyExists, 7)]
    [InlineData(ErrorCategory.FailedPrecondition, 8)]
    [InlineData(ErrorCategory.RateLimited, 9)]
    [InlineData(ErrorCategory.Unavailable, 10)]
    [InlineData(ErrorCategory.Timeout, 11)]
    [InlineData(ErrorCategory.UpstreamFailure, 12)]
    [InlineData(ErrorCategory.Cancelled, 13)]
    public void ErrorCategory_ValuesAreStable(ErrorCategory category, int expected)
    {
        ((int)category).ShouldBe(expected);
    }

    [Fact]
    public void ErrorCategory_HasExactlyTheDocumentedValues()
    {
        // A new category is additive: it gets the next value, a row in every protocol table (ADR-062) and a row above.
        Enum.GetValues<ErrorCategory>().Length.ShouldBe(14);
        default(ErrorCategory).ShouldBe(ErrorCategory.Internal);
    }

    [Fact]
    public void MetadataKeys_HaveStableValues()
    {
        EncinaErrorMetadataKeys.RetryAfter.ShouldBe("encina.retry_after");
        EncinaErrorMetadataKeys.AuthenticateChallenge.ShouldBe("encina.authenticate_challenge");
    }
}
