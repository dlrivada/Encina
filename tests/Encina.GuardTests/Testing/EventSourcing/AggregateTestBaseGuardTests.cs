using System.Diagnostics.CodeAnalysis;
using Encina.DomainModeling;
using Encina.Testing.EventSourcing;

namespace Encina.GuardTests.Testing.EventSourcing;

/// <summary>
/// Guard tests for <see cref="AggregateTestBase{TAggregate, TId}"/>.
/// Since it is abstract, we use a concrete test implementation.
/// </summary>
public class AggregateTestBaseGuardTests
{
    public class GivenGuards
    {
        [Fact]
        public void NullEvents_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallGiven(null!));
        }
    }

    public class WhenGuards
    {
        [Fact]
        public void NullAction_Throws()
        {
            var spec = new TestAggregateSpec();
            spec.CallGivenEmpty();

            Should.Throw<ArgumentNullException>(() =>
                spec.CallWhen((Action<TestAggregate>)null!));
        }

        [Fact]
        public void WhenBeforeGiven_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallWhen(_ => { }));
        }
    }

    public class WhenAsyncGuards
    {
        [Fact]
        public async Task NullAction_Throws()
        {
            var spec = new TestAggregateSpec();
            spec.CallGivenEmpty();

            await Should.ThrowAsync<ArgumentNullException>(async () =>
                await spec.CallWhenAsync(null!));
        }

        [Fact]
        public async Task WhenAsyncBeforeGiven_Throws()
        {
            var spec = new TestAggregateSpec();

            await Should.ThrowAsync<InvalidOperationException>(async () =>
                await spec.CallWhenAsync(_ => Task.CompletedTask));
        }
    }

    public class ThenGuards
    {
        [Fact]
        public void ThenWithValidator_NullValidator_Throws()
        {
            var spec = new TestAggregateSpec();
            spec.CallGivenEmpty();
            spec.CallWhen(a => a.RaiseTestEvent());

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenWithValidator<TestEvent>(null!));
        }

        [Fact]
        public void ThenEvents_NullEventTypes_Throws()
        {
            var spec = new TestAggregateSpec();
            spec.CallGivenEmpty();
            spec.CallWhen(a => a.RaiseTestEvent());

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenEvents(null!));
        }

        [Fact]
        public void ThenState_NullValidator_Throws()
        {
            var spec = new TestAggregateSpec();
            spec.CallGivenEmpty();
            spec.CallWhen(a => a.RaiseTestEvent());

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenState(null!));
        }

        [Fact]
        public void ThenThrowsWithValidator_NullValidator_Throws()
        {
            var spec = new TestAggregateSpec();
            spec.CallGivenEmpty();
            spec.CallWhen(_ => throw new InvalidOperationException("test"));

            Should.Throw<ArgumentNullException>(() =>
                spec.CallThenThrowsWithValidator<InvalidOperationException>(null!));
        }
    }

    public class ThenBeforeWhenGuards
    {
        [Fact]
        public void Then_BeforeWhen_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThen<TestEvent>());
        }

        [Fact]
        public void ThenNoEvents_BeforeWhen_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenNoEvents());
        }

        [Fact]
        public void ThenThrows_BeforeWhen_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallThenThrows<InvalidOperationException>());
        }

        [Fact]
        public void Aggregate_BeforeWhen_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<InvalidOperationException>(() =>
            {
                _ = spec.GetAggregate();
            });
        }

        [Fact]
        public void GetUncommittedEvents_BeforeWhen_Throws()
        {
            var spec = new TestAggregateSpec();

            Should.Throw<InvalidOperationException>(() =>
                spec.CallGetUncommittedEvents());
        }
    }

    // Test aggregate
    public sealed class TestAggregate : AggregateBase<Guid>
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Calls base class instance method RaiseDomainEvent")]
        public void RaiseTestEvent()
        {
            RaiseEvent(new TestEvent());
        }

        protected override void Apply(object domainEvent)
        {
            // No state to apply for test aggregate
        }
    }

    public sealed class TestEvent;

    // Concrete test implementation exposing protected members
    private sealed class TestAggregateSpec : AggregateTestBase<TestAggregate, Guid>
    {
        public void CallGiven(params object[] events) => Given(events);
        public void CallGivenEmpty() => GivenEmpty();
        public void CallWhen(Action<TestAggregate> action) => When(action);
        public Task CallWhenAsync(Func<TestAggregate, Task> action) => WhenAsync(action);
        public TEvent CallThen<TEvent>() where TEvent : class => Then<TEvent>();
        public TEvent CallThenWithValidator<TEvent>(Action<TEvent> validator) where TEvent : class => Then(validator);
        public void CallThenEvents(params Type[] types) => ThenEvents(types);
        public void CallThenNoEvents() => ThenNoEvents();
        public void CallThenState(Action<TestAggregate> validator) => ThenState(validator);
        public TException CallThenThrows<TException>() where TException : Exception => ThenThrows<TException>();
        public TException CallThenThrowsWithValidator<TException>(Action<TException> validator)
            where TException : Exception => ThenThrows(validator);
        public TestAggregate GetAggregate() => Aggregate;
        public IReadOnlyList<object> CallGetUncommittedEvents() => GetUncommittedEvents();
    }
}
