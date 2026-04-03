using Encina.Testing.Sagas;

namespace Encina.GuardTests.Testing.Sagas;

/// <summary>
/// Guard tests for <see cref="SagaSpecification{TSaga, TSagaData}"/>.
/// Since it is abstract, we use a concrete test implementation.
/// </summary>
public class SagaSpecificationGuardTests
{
    public class GivenDataGuards
    {
        [Fact]
        public void NullConfigure_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallGivenData(null!));
        }
    }

    public class GivenSagaDataGuards
    {
        [Fact]
        public void NullData_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallGivenSagaData(null!));
        }
    }

    public class ThenDataGuards
    {
        [Fact]
        public async Task NullValidate_Throws()
        {
            var spec = new TestSagaSpec();
            spec.CallGivenSagaData(new TestSagaData());
            await spec.CallWhenComplete();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenData(null!));
        }
    }

    public class ThenThrowsGuards
    {
        [Fact]
        public async Task NullValidate_Throws()
        {
            var spec = new TestSagaSpec();
            spec.CallGivenSagaData(new TestSagaData());
            // Make it throw
            spec.ThrowOnExecute = true;
            await spec.CallWhenComplete();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenThrowsWithValidate(null!));
        }
    }

    public class ThenErrorWithCodeGuards
    {
        [Fact]
        public async Task NullCode_Throws()
        {
            var spec = new TestSagaSpec();
            spec.ReturnError = true;
            spec.CallGivenSagaData(new TestSagaData());
            await spec.CallWhenComplete();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenErrorWithCode(null!));
        }
    }

    public class ThenBeforeWhenGuards
    {
        [Fact]
        public void Result_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = spec.GetResult();
            });
        }

        [Fact]
        public void SagaData_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = spec.GetSagaDataValue();
            });
        }

        [Fact]
        public void Saga_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = spec.GetSagaValue();
            });
        }

        [Fact]
        public void ThenSuccess_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenSuccess());
        }

        [Fact]
        public void ThenError_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenError());
        }

        [Fact]
        public void ThenCompleted_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenCompleted());
        }

        [Fact]
        public void ThenCompensated_BeforeWhen_Throws()
        {
            var spec = new TestSagaSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenCompensated());
        }
    }

    // Concrete test implementation
    private sealed class TestSagaSpec : SagaSpecification<TestSaga, TestSagaData>
    {
        public bool ThrowOnExecute { get; set; }
        public bool ReturnError { get; set; }

        protected override TestSaga CreateSaga() => new();

        protected override ValueTask<LanguageExt.Either<EncinaError, TestSagaData>> ExecuteSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
        {
            if (ThrowOnExecute)
                throw new InvalidOperationException("Test exception");

            if (ReturnError)
                return new ValueTask<LanguageExt.Either<EncinaError, TestSagaData>>(
                    LanguageExt.Prelude.Left<EncinaError, TestSagaData>(EncinaErrors.Create("test.error", "Saga failed")));

            return new ValueTask<LanguageExt.Either<EncinaError, TestSagaData>>(
                LanguageExt.Prelude.Right<EncinaError, TestSagaData>(data));
        }

        protected override Task CompensateSagaAsync(
            TestSaga saga, TestSagaData data, int fromStep, IRequestContext context, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        // Expose protected members for testing
        public void CallGivenData(Action<TestSagaData> configure) => GivenData(configure);
        public void CallGivenSagaData(TestSagaData data) => GivenSagaData(data);
        public Task CallWhenComplete() => WhenComplete();
        public TestSagaData CallThenSuccess() => ThenSuccess();
        public EncinaError CallThenError() => ThenError();
        public void CallThenData(Action<TestSagaData> validate) => ThenData(validate);
        public SagaSpecification<TestSaga, TestSagaData> CallThenCompleted() => ThenCompleted();
        public SagaSpecification<TestSaga, TestSagaData> CallThenCompensated() => ThenCompensated();
        public EncinaError CallThenErrorWithCode(string code) => ThenErrorWithCode(code);
        public void CallThenThrowsWithValidate(Action<InvalidOperationException> validate) => ThenThrows(validate);
        public LanguageExt.Either<EncinaError, TestSagaData> GetResult() => Result;
        public TestSagaData GetSagaDataValue() => SagaData;
        public TestSaga GetSagaValue() => Saga;
    }

    private sealed class TestSaga;

    private sealed class TestSagaData
    {
        public string Name { get; set; } = "Test";
    }
}
