using Encina.Testing.Fakes.Models;
using LanguageExt;
using Shouldly;

namespace Encina.Testing.Verify.Tests;

/// <summary>
/// Unit tests for <see cref="EncinaVerify"/>.
/// </summary>
public class EncinaVerifyTests
{
    #region PrepareEither Tests

    [Fact]
    public Task PrepareEither_WithRightValue_ReturnsCorrectSnapshot()
    {
        // Arrange
        var either = Either<string, int>.Right(42);

        // Act
        var result = EncinaVerify.PrepareEither(either);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public Task PrepareEither_WithLeftValue_ReturnsCorrectSnapshot()
    {
        // Arrange
        var either = Either<string, int>.Left("Error occurred");

        // Act
        var result = EncinaVerify.PrepareEither(either);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public Task PrepareEither_WithEncinaErrorLeft_ReturnsCorrectSnapshot()
    {
        // Arrange
        var error = EncinaError.New("Validation failed");
        var either = Either<EncinaError, string>.Left(error);

        // Act
        var result = EncinaVerify.PrepareEither(either);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public Task PrepareEither_WithComplexRightValue_ReturnsCorrectSnapshot()
    {
        // Arrange
        var response = new { OrderId = "ORD-001", Total = 99.99m, Items = new[] { "Item1", "Item2" } };
        var either = Either<EncinaError, object>.Right(response);

        // Act
        var result = EncinaVerify.PrepareEither(either);

        // Assert
        return Verifier.Verify(result);
    }

    #endregion

    #region ExtractSuccess Tests

    [Fact]
    public void ExtractSuccess_WithSuccess_ReturnsValue()
    {
        // Arrange
        var expected = "Success value";
        var either = Either<EncinaError, string>.Right(expected);

        // Act
        var result = EncinaVerify.ExtractSuccess(either);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact]
    public void ExtractSuccess_WithError_ThrowsInvalidOperationException()
    {
        // Arrange
        var error = EncinaError.New("Something went wrong");
        var either = Either<EncinaError, string>.Left(error);

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => EncinaVerify.ExtractSuccess(either));
        ex.Message.ShouldContain("Something went wrong");
    }

    #endregion

    #region ExtractError Tests

    [Fact]
    public void ExtractError_WithError_ReturnsError()
    {
        // Arrange
        var expected = EncinaError.New("Expected error");
        var either = Either<EncinaError, string>.Left(expected);

        // Act
        var result = EncinaVerify.ExtractError(either);

        // Assert
        result.Message.ShouldBe("Expected error");
    }

    [Fact]
    public void ExtractError_WithSuccess_ThrowsInvalidOperationException()
    {
        // Arrange
        var either = Either<EncinaError, string>.Right("Success");

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() => EncinaVerify.ExtractError(either));
        ex.Message.ShouldContain("Expected error but got success");
    }

    #endregion

    #region PrepareOutboxMessages Tests

    [Fact]
    public Task PrepareOutboxMessages_WithMessages_ReturnsCorrectSnapshot()
    {
        // Arrange
        var messages = new List<FakeOutboxMessage>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                NotificationType = "OrderCreated",
                Content = "{\"orderId\": \"ORD-001\"}",
                RetryCount = 0
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                NotificationType = "OrderShipped",
                Content = "{\"orderId\": \"ORD-001\", \"trackingNumber\": \"TRK-123\"}",
                RetryCount = 2,
                ErrorMessage = "Timeout"
            }
        };

        // Act
        var result = EncinaVerify.PrepareOutboxMessages(messages);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public void PrepareOutboxMessages_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => EncinaVerify.PrepareOutboxMessages(null!));
    }

    #endregion

    #region PrepareSagaState Tests

    [Fact]
    public Task PrepareSagaState_WithValidState_ReturnsCorrectSnapshot()
    {
        // Arrange
        var sagaState = new FakeSagaState
        {
            SagaId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            SagaType = "OrderFulfillmentSaga",
            Status = "Completed",
            CurrentStep = 3,
            Data = "{\"orderId\": \"ORD-001\", \"customerId\": \"CUST-001\"}"
        };

        // Act
        var result = EncinaVerify.PrepareSagaState(sagaState);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public Task PrepareSagaState_WithError_ReturnsCorrectSnapshot()
    {
        // Arrange
        var sagaState = new FakeSagaState
        {
            SagaId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            SagaType = "PaymentSaga",
            Status = "Failed",
            CurrentStep = 1,
            Data = "{\"paymentId\": \"PAY-001\"}",
            ErrorMessage = "Payment gateway timeout"
        };

        // Act
        var result = EncinaVerify.PrepareSagaState(sagaState);

        // Assert
        return Verifier.Verify(result);
    }

    [Fact]
    public void PrepareSagaState_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => EncinaVerify.PrepareSagaState(null!));
    }

    #endregion
}
