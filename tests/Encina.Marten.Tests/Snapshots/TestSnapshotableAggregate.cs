using Encina.Marten.Snapshots;

namespace Encina.Marten.Tests.Snapshots;

/// <summary>
/// Test aggregate that supports snapshotting.
/// </summary>
public sealed class TestSnapshotableAggregate : AggregateBase, ISnapshotable<TestSnapshotableAggregate>
{
    public string Name { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public int ItemCount { get; private set; }
    public string Status { get; private set; } = "Created";

    public TestSnapshotableAggregate()
    {
    }

    public TestSnapshotableAggregate(Guid id, string name)
    {
        RaiseEvent(new TestAggregateCreated(id, name));
    }

    public void AddItem(decimal amount)
    {
        RaiseEvent(new TestItemAdded(Id, amount));
    }

    public void Complete()
    {
        RaiseEvent(new TestAggregateCompleted(Id));
    }

    protected override void Apply(object domainEvent)
    {
        switch (domainEvent)
        {
            case TestAggregateCreated e:
                Id = e.Id;
                Name = e.Name;
                Status = "Created";
                break;
            case TestItemAdded e:
                Total += e.Amount;
                ItemCount++;
                break;
            case TestAggregateCompleted:
                Status = "Completed";
                break;
        }
    }

    // Internal method to set version for testing
    internal void SetVersionForTest(int version)
    {
        Version = version;
    }
}

/// <summary>
/// Test aggregate without snapshot support.
/// </summary>
public sealed class TestNonSnapshotableAggregate : AggregateBase
{
    public string Value { get; private set; } = string.Empty;

    public void SetValue(string value)
    {
        RaiseEvent(new TestValueSet(value));
    }

    protected override void Apply(object domainEvent)
    {
        if (domainEvent is TestValueSet e)
        {
            Value = e.Value;
        }
    }
}

// Test events
public sealed record TestAggregateCreated(Guid Id, string Name);
public sealed record TestItemAdded(Guid AggregateId, decimal Amount);
public sealed record TestAggregateCompleted(Guid AggregateId);
public sealed record TestValueSet(string Value);
