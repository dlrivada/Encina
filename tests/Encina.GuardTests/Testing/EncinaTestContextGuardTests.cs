using Encina.Testing;
using Encina.Testing.Fakes.Stores;

namespace Encina.GuardTests.Testing;

public class EncinaTestContextGuardTests
{
    public class ShouldSucceedWithGuards
    {
        [Fact]
        public void NullVerify_Throws()
        {
            var fixture = new EncinaTestFixture().WithMockedOutbox();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<ArgumentNullException>(() =>
                ctx.ShouldSucceedWith(null!));
        }
    }

    public class ShouldFailWithGuards
    {
        [Fact]
        public void NullVerify_Throws()
        {
            var fixture = new EncinaTestFixture().WithMockedOutbox();
            var result = LanguageExt.Prelude.Left<EncinaError, string>(EncinaError.New("test"));
            var ctx = CreateContext(result, fixture);

            Should.Throw<ArgumentNullException>(() =>
                ctx.ShouldFailWith(null!));
        }
    }

    public class ShouldSucceedGuards
    {
        [Fact]
        public void ErrorResult_ShouldFail_Throws()
        {
            var fixture = new EncinaTestFixture().WithMockedOutbox();
            var result = LanguageExt.Prelude.Left<EncinaError, string>(EncinaError.New("something failed"));
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ShouldSucceed());
        }
    }

    public class ShouldFailGuards
    {
        [Fact]
        public void SuccessResult_ShouldFail_Throws()
        {
            var fixture = new EncinaTestFixture().WithMockedOutbox();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ShouldFail());
        }
    }

    public class OutboxGuards
    {
        [Fact]
        public void OutboxShouldContain_WithPredicate_NullPredicate_Throws()
        {
            var fixture = new EncinaTestFixture().WithMockedOutbox();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<ArgumentNullException>(() =>
                ctx.OutboxShouldContain<string>(null!));
        }

        [Fact]
        public void OutboxShouldContain_NoOutboxConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.OutboxShouldContain<string>());
        }

        [Fact]
        public void OutboxShouldBeEmpty_NoOutboxConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.OutboxShouldBeEmpty());
        }
    }

    public class SagaGuards
    {
        [Fact]
        public void SagaShouldBeStarted_NoSagaConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.SagaShouldBeStarted<TestSaga>());
        }

        [Fact]
        public void SagaShouldHaveTimedOut_NoSagaConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.SagaShouldHaveTimedOut<TestSaga>());
        }

        [Fact]
        public void SagaShouldHaveCompleted_NoSagaConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.SagaShouldHaveCompleted<TestSaga>());
        }

        [Fact]
        public void SagaShouldBeCompensating_NoSagaConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.SagaShouldBeCompensating<TestSaga>());
        }

        [Fact]
        public void SagaShouldHaveFailed_NoSagaConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.SagaShouldHaveFailed<TestSaga>());
        }
    }

    public class TimeProviderGuards
    {
        [Fact]
        public void ThenAdvanceTimeBy_NoTimeProviderConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ThenAdvanceTimeBy(TimeSpan.FromMinutes(1)));
        }

        [Fact]
        public void ThenAdvanceTimeByMinutes_NoTimeProviderConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ThenAdvanceTimeByMinutes(5));
        }

        [Fact]
        public void ThenAdvanceTimeByHours_NoTimeProviderConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ThenAdvanceTimeByHours(1));
        }

        [Fact]
        public void ThenAdvanceTimeByDays_NoTimeProviderConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ThenAdvanceTimeByDays(1));
        }

        [Fact]
        public void ThenSetTimeTo_NoTimeProviderConfigured_Throws()
        {
            var fixture = new EncinaTestFixture();
            var result = LanguageExt.Prelude.Right<EncinaError, string>("ok");
            var ctx = CreateContext(result, fixture);

            Should.Throw<InvalidOperationException>(() =>
                ctx.ThenSetTimeTo(DateTimeOffset.UtcNow));
        }
    }

    // Helper to create EncinaTestContext via reflection (constructor is internal)
    private static EncinaTestContext<T> CreateContext<T>(
        LanguageExt.Either<EncinaError, T> result,
        EncinaTestFixture fixture)
    {
        var ctor = typeof(EncinaTestContext<T>).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
        return (EncinaTestContext<T>)ctor.Invoke([result, fixture]);
    }

    private sealed class TestSaga;
}
