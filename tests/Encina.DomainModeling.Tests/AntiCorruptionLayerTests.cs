using System.Globalization;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.Tests;

/// <summary>
/// Unit tests for the Anti-Corruption Layer pattern implementation.
/// </summary>
public class AntiCorruptionLayerTests
{
    #region External Models

    private sealed record StripeWebhook(
        string Id,
        string Type,
        StripeData Data);

    private sealed record StripeData(
        long Amount,
        string Currency,
        long Created,
        Dictionary<string, string> Metadata);

    #endregion

    #region Internal Models

    private sealed record PaymentReceived(
        Guid PaymentId,
        decimal Amount,
        DateTimeOffset ReceivedAt);

    #endregion

    #region ACL Implementations

    private sealed class StripePaymentACL : AntiCorruptionLayerBase<StripeWebhook, PaymentReceived>
    {
        protected override string? ExternalSystemId => "Stripe";

        public override Either<TranslationError, PaymentReceived> TranslateToInternal(StripeWebhook external)
        {
            if (external.Type != "payment_intent.succeeded")
                return UnsupportedType(external.Type);

            if (!external.Data.Metadata.TryGetValue("order_id", out var orderId))
                return MissingField("order_id");

            if (!Guid.TryParse(orderId, out var paymentId))
                return InvalidFormat("order_id");

            return new PaymentReceived(
                PaymentId: paymentId,
                Amount: external.Data.Amount / 100m,
                ReceivedAt: DateTimeOffset.FromUnixTimeSeconds(external.Data.Created));
        }

        public override Either<TranslationError, StripeWebhook> TranslateToExternal(PaymentReceived internalModel)
        {
            return UnsupportedType("PaymentReceived");
        }
    }

    private sealed class BidirectionalACL : IAntiCorruptionLayer<string, int>
    {
        public Either<TranslationError, int> TranslateToInternal(string external)
        {
            if (int.TryParse(external, out var result))
                return result;
            return TranslationError.InvalidFormat("value");
        }

        public Either<TranslationError, string> TranslateToExternal(int internalModel)
        {
            return internalModel.ToString(CultureInfo.InvariantCulture);
        }
    }

    #endregion

    #region TranslateToInternal Tests

    [Fact]
    public void TranslateToInternal_ValidData_ReturnsRight()
    {
        // Arrange
        var acl = new StripePaymentACL();
        var webhook = new StripeWebhook(
            Id: "evt_123",
            Type: "payment_intent.succeeded",
            Data: new StripeData(
                Amount: 10000,
                Currency: "usd",
                Created: 1704067200,
                Metadata: new Dictionary<string, string>
                {
                    ["order_id"] = "550e8400-e29b-41d4-a716-446655440000"
                }));

        // Act
        var result = acl.TranslateToInternal(webhook);

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(payment =>
        {
            payment.PaymentId.Should().Be(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"));
            payment.Amount.Should().Be(100m);
        });
    }

    [Fact]
    public void TranslateToInternal_UnsupportedType_ReturnsLeft()
    {
        // Arrange
        var acl = new StripePaymentACL();
        var webhook = new StripeWebhook(
            Id: "evt_123",
            Type: "payment_intent.failed",
            Data: new StripeData(0, "usd", 0, new Dictionary<string, string>()));

        // Act
        var result = acl.TranslateToInternal(webhook);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.ErrorCode.Should().Be("ACL_UNSUPPORTED_TYPE");
            error.ExternalSystemId.Should().Be("Stripe");
        });
    }

    [Fact]
    public void TranslateToInternal_MissingField_ReturnsLeft()
    {
        // Arrange
        var acl = new StripePaymentACL();
        var webhook = new StripeWebhook(
            Id: "evt_123",
            Type: "payment_intent.succeeded",
            Data: new StripeData(10000, "usd", 0, new Dictionary<string, string>()));

        // Act
        var result = acl.TranslateToInternal(webhook);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.ErrorCode.Should().Be("ACL_MISSING_FIELD");
        });
    }

    [Fact]
    public void TranslateToInternal_InvalidFormat_ReturnsLeft()
    {
        // Arrange
        var acl = new StripePaymentACL();
        var webhook = new StripeWebhook(
            Id: "evt_123",
            Type: "payment_intent.succeeded",
            Data: new StripeData(
                Amount: 10000,
                Currency: "usd",
                Created: 0,
                Metadata: new Dictionary<string, string>
                {
                    ["order_id"] = "not-a-guid"
                }));

        // Act
        var result = acl.TranslateToInternal(webhook);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error =>
        {
            error.ErrorCode.Should().Be("ACL_INVALID_FORMAT");
        });
    }

    #endregion

    #region TranslateToExternal Tests

    [Fact]
    public void TranslateToExternal_WhenSupported_ReturnsRight()
    {
        // Arrange
        var acl = new BidirectionalACL();

        // Act
        var result = acl.TranslateToExternal(42);

        // Assert
        result.IsRight.Should().BeTrue();
        result.IfRight(str => str.Should().Be("42"));
    }

    [Fact]
    public void TranslateToExternal_WhenNotSupported_ReturnsLeft()
    {
        // Arrange
        var acl = new StripePaymentACL();
        var payment = new PaymentReceived(Guid.NewGuid(), 100m, DateTimeOffset.UtcNow);

        // Act
        var result = acl.TranslateToExternal(payment);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    #endregion

    #region TranslationError Tests

    [Fact]
    public void TranslationError_UnsupportedType_CreatesCorrectError()
    {
        // Act
        var error = TranslationError.UnsupportedType("SomeType", "ExternalSystem");

        // Assert
        error.ErrorCode.Should().Be("ACL_UNSUPPORTED_TYPE");
        error.ErrorMessage.Should().Contain("SomeType");
        error.ExternalSystemId.Should().Be("ExternalSystem");
    }

    [Fact]
    public void TranslationError_MissingRequiredField_CreatesCorrectError()
    {
        // Act
        var error = TranslationError.MissingRequiredField("fieldName", "ExternalSystem");

        // Assert
        error.ErrorCode.Should().Be("ACL_MISSING_FIELD");
        error.ErrorMessage.Should().Contain("fieldName");
    }

    [Fact]
    public void TranslationError_InvalidFormat_CreatesCorrectError()
    {
        // Act
        var error = TranslationError.InvalidFormat("fieldName", "ExternalSystem");

        // Assert
        error.ErrorCode.Should().Be("ACL_INVALID_FORMAT");
        error.ErrorMessage.Should().Contain("fieldName");
    }

    #endregion

    #region AntiCorruptionLayerBase Helper Methods Tests

    [Fact]
    public void Error_CreatesErrorWithSystemId()
    {
        // Arrange
        var acl = new TestACL();

        // Act
        var error = acl.TestError("ERR001", "Test error");

        // Assert
        error.ErrorCode.Should().Be("ERR001");
        error.ErrorMessage.Should().Be("Test error");
        error.ExternalSystemId.Should().Be("TestSystem");
    }

    private sealed class TestACL : AntiCorruptionLayerBase<string, int>
    {
        protected override string? ExternalSystemId => "TestSystem";

        public override Either<TranslationError, int> TranslateToInternal(string external)
            => int.TryParse(external, out var i) ? i : InvalidFormat("value");

        public override Either<TranslationError, string> TranslateToExternal(int internalModel)
            => internalModel.ToString(CultureInfo.InvariantCulture);

        public TranslationError TestError(string code, string message)
            => Error(code, message);
    }

    #endregion
}
