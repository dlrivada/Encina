using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging;
using LanguageExt;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Encina.UnitTests.EntityFrameworkCore.Sagas;

/// <summary>
/// Unit tests for <see cref="Saga{TSagaData}"/>.
/// </summary>
public sealed class SagaTests
{
    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_AllStepsSucceed_ReturnsRightWithFinalData()
    {
        // Arrange
        var saga = new TestSaga();
        var data = new TestSagaData { Value = 0 };
        var context = CreateMockRequestContext();

        // Act
        var result = await saga.ExecuteAsync(data, 0, context, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: finalData =>
            {
                finalData.Value.ShouldBe(3); // Three steps, each adds 1
                return Unit.Default;
            },
            Left: _ => throw new InvalidOperationException("Should be Right"));
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_ReturnsLeftWithError()
    {
        // Arrange
        var saga = new FailingTestSaga();
        var data = new TestSagaData { Value = 0 };
        var context = CreateMockRequestContext();

        // Act
        var result = await saga.ExecuteAsync(data, 0, context, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_StepFails_ExecutesCompensations()
    {
        // Arrange
        var saga = new FailingTestSagaWithCompensation();
        var data = new TestSagaData { Value = 0 };
        var context = CreateMockRequestContext();

        // Act
        await saga.ExecuteAsync(data, 0, context, CancellationToken.None);

        // Assert - Compensation should have been called
        saga.CompensationCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_FromMiddleStep_ResumesCorrectly()
    {
        // Arrange
        var saga = new TestSaga();
        var data = new TestSagaData { Value = 1 }; // Already ran step 0
        var context = CreateMockRequestContext();

        // Act - Start from step 1
        var result = await saga.ExecuteAsync(data, 1, context, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _ = result.Match(
            Right: finalData =>
            {
                finalData.Value.ShouldBe(3); // Started at 1, added 2 more
                return Unit.Default;
            },
            Left: _ => throw new InvalidOperationException("Should be Right"));
    }

    #endregion

    #region CompensateAsync Tests

    [Fact]
    public async Task CompensateAsync_ExecutesCompensationsInReverseOrder()
    {
        // Arrange
        var saga = new CompensationOrderTestSaga();
        var data = new TestSagaData { Value = 0 };
        var context = CreateMockRequestContext();

        // Execute all steps first
        await saga.ExecuteAsync(data, 0, context, CancellationToken.None);

        // Clear compensation order
        saga.CompensationOrder.Clear();

        // Act - Compensate from step 2 (index) backwards
        await saga.CompensateAsync(data, 2, context, CancellationToken.None);

        // Assert - Should compensate in reverse order: 2, 1, 0
        saga.CompensationOrder.ShouldBe([2, 1, 0]);
    }

    [Fact]
    public async Task CompensateAsync_SkipsNullCompensations()
    {
        // Arrange
        var saga = new SagaWithNullCompensation();
        var data = new TestSagaData { Value = 0 };
        var context = CreateMockRequestContext();

        // Execute to configure steps
        await saga.ExecuteAsync(data, 0, context, CancellationToken.None);

        // Act - Should not throw even with null compensations
        await saga.CompensateAsync(data, 1, context, CancellationToken.None);

        // Assert - Compensation that exists should be called
        saga.CompensationCalled.ShouldBeTrue();
    }

    #endregion

    #region StepCount Tests

    [Fact]
    public void StepCount_ReturnsCorrectCount()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        var count = saga.StepCount;

        // Assert
        count.ShouldBe(3);
    }

    [Fact]
    public void StepCount_CalledTwice_ReturnsConsistentValue()
    {
        // Arrange
        var saga = new TestSaga();

        // Act
        var count1 = saga.StepCount;
        var count2 = saga.StepCount;

        // Assert
        count1.ShouldBe(count2);
    }

    #endregion

    #region Helper Methods

    private static IRequestContext CreateMockRequestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");
        return context;
    }

    #endregion

    #region Test Sagas

    private sealed class TestSagaData
    {
        public int Value { get; set; }
    }

    private sealed class TestSaga : Saga<TestSagaData>
    {
        protected override void ConfigureSteps()
        {
            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                });

            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                });

            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                });
        }
    }

    private sealed class FailingTestSaga : Saga<TestSagaData>
    {
        protected override void ConfigureSteps()
        {
            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                });

            AddStep(
                execute: (_, _, _) =>
                {
                    var error = EncinaError.New("Step 2 failed");
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Left(error));
                });

            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                });
        }
    }

    private sealed class FailingTestSagaWithCompensation : Saga<TestSagaData>
    {
        public bool CompensationCalled { get; private set; }

        protected override void ConfigureSteps()
        {
            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                },
                compensate: (_, _, _) =>
                {
                    CompensationCalled = true;
                    return Task.CompletedTask;
                });

            AddStep(
                execute: (_, _, _) =>
                {
                    var error = EncinaError.New("Step 2 failed");
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Left(error));
                });
        }
    }

    private sealed class CompensationOrderTestSaga : Saga<TestSagaData>
    {
        public List<int> CompensationOrder { get; } = [];

        protected override void ConfigureSteps()
        {
            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                },
                compensate: (_, _, _) =>
                {
                    CompensationOrder.Add(0);
                    return Task.CompletedTask;
                });

            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                },
                compensate: (_, _, _) =>
                {
                    CompensationOrder.Add(1);
                    return Task.CompletedTask;
                });

            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                },
                compensate: (_, _, _) =>
                {
                    CompensationOrder.Add(2);
                    return Task.CompletedTask;
                });
        }
    }

    private sealed class SagaWithNullCompensation : Saga<TestSagaData>
    {
        public bool CompensationCalled { get; private set; }

        protected override void ConfigureSteps()
        {
            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                },
                compensate: (_, _, _) =>
                {
                    CompensationCalled = true;
                    return Task.CompletedTask;
                });

            // Step without compensation
            AddStep(
                execute: (data, _, _) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Either<EncinaError, TestSagaData>.Right(data));
                },
                compensate: null);
        }
    }

    #endregion
}
