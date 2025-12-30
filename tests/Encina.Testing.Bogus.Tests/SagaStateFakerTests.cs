using Shouldly;
using Xunit;

namespace Encina.Testing.Bogus.Tests;

/// <summary>
/// Unit tests for <see cref="SagaStateFaker"/>.
/// </summary>
public sealed class SagaStateFakerTests
{
    [Fact]
    public void Generate_ShouldCreateValidSagaState()
    {
        // Arrange
        var faker = new SagaStateFaker();

        // Act
        var saga = faker.Generate();

        // Assert
        saga.ShouldNotBeNull();
        saga.SagaId.ShouldNotBe(Guid.Empty);
        saga.SagaType.ShouldNotBeNullOrEmpty();
        saga.Data.ShouldNotBeNullOrEmpty();
        saga.Status.ShouldBe("Running");
        saga.CurrentStep.ShouldBeGreaterThanOrEqualTo(0);
        saga.StartedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        saga.CompletedAtUtc.ShouldBeNull();
        saga.ErrorMessage.ShouldBeNull();
        saga.LastUpdatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
        saga.TimeoutAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Generate_ShouldBeReproducible()
    {
        // Arrange
        var faker1 = new SagaStateFaker();
        var faker2 = new SagaStateFaker();

        // Act
        var saga1 = faker1.Generate();
        var saga2 = faker2.Generate();

        // Assert
        saga1.SagaId.ShouldBe(saga2.SagaId);
        saga1.SagaType.ShouldBe(saga2.SagaType);
    }

    [Fact]
    public void AsCompleted_ShouldSetCompletedStatus()
    {
        // Arrange
        var faker = new SagaStateFaker().AsCompleted();

        // Act
        var saga = faker.Generate();

        // Assert
        saga.Status.ShouldBe("Completed");
        saga.CompletedAtUtc.ShouldNotBeNull();
        saga.CompletedAtUtc!.Value.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void AsCompensating_ShouldSetCompensatingStatus()
    {
        // Arrange
        var faker = new SagaStateFaker().AsCompensating();

        // Act
        var saga = faker.Generate();

        // Assert
        saga.Status.ShouldBe("Compensating");
    }

    [Fact]
    public void AsFailed_ShouldSetFailedStatusAndError()
    {
        // Arrange
        var faker = new SagaStateFaker().AsFailed();

        // Act
        var saga = faker.Generate();

        // Assert
        saga.Status.ShouldBe("Failed");
        saga.ErrorMessage.ShouldNotBeNullOrEmpty();
        saga.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void AsFailed_WithCustomError_ShouldSetSpecificError()
    {
        // Arrange
        var errorMessage = "Payment gateway timeout";
        var faker = new SagaStateFaker().AsFailed(errorMessage);

        // Act
        var saga = faker.Generate();

        // Assert
        saga.ErrorMessage.ShouldBe(errorMessage);
    }

    [Fact]
    public void AsTimedOut_ShouldSetTimedOutStatus()
    {
        // Arrange
        var faker = new SagaStateFaker().AsTimedOut();

        // Act
        var saga = faker.Generate();

        // Assert
        saga.Status.ShouldBe("TimedOut");
        saga.TimeoutAtUtc.ShouldNotBeNull();
        saga.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void WithSagaType_ShouldSetSpecificType()
    {
        // Arrange
        var faker = new SagaStateFaker().WithSagaType("CustomSaga");

        // Act
        var saga = faker.Generate();

        // Assert
        saga.SagaType.ShouldBe("CustomSaga");
    }

    [Fact]
    public void WithSagaId_ShouldSetSpecificId()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var faker = new SagaStateFaker().WithSagaId(sagaId);

        // Act
        var saga = faker.Generate();

        // Assert
        saga.SagaId.ShouldBe(sagaId);
    }

    [Fact]
    public void WithData_ShouldSetSpecificData()
    {
        // Arrange
        var data = "{\"orderId\": \"123\", \"amount\": 99.99}";
        var faker = new SagaStateFaker().WithData(data);

        // Act
        var saga = faker.Generate();

        // Assert
        saga.Data.ShouldBe(data);
    }

    [Fact]
    public void AtStep_ShouldSetCurrentStep()
    {
        // Arrange
        var faker = new SagaStateFaker().AtStep(5);

        // Act
        var saga = faker.Generate();

        // Assert
        saga.CurrentStep.ShouldBe(5);
    }

    [Fact]
    public void GenerateState_ShouldReturnAsInterface()
    {
        // Arrange
        var faker = new SagaStateFaker();

        // Act
        var saga = faker.GenerateState();

        // Assert
        saga.ShouldNotBeNull();
        saga.ShouldBeOfType<FakeSagaState>();
    }

    [Fact]
    public void GenerateMultiple_ShouldCreateUniqueSagas()
    {
        // Arrange
        var faker = new SagaStateFaker();

        // Act
        var sagas = faker.Generate(5);

        // Assert
        sagas.Count.ShouldBe(5);
        sagas.Select(s => s.SagaId).Distinct().Count().ShouldBe(5);
    }

    [Fact]
    public void MethodChaining_ShouldWork()
    {
        // Arrange & Act
        var saga = new SagaStateFaker()
            .WithSagaType("OrderSaga")
            .WithData("{\"test\": true}")
            .AtStep(3)
            .AsCompensating()
            .Generate();

        // Assert
        saga.SagaType.ShouldBe("OrderSaga");
        saga.Data.ShouldBe("{\"test\": true}");
        saga.CurrentStep.ShouldBe(3);
        saga.Status.ShouldBe("Compensating");
    }
}
