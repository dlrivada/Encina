using Encina.Marten.Snapshots;

namespace Encina.Marten.IntegrationTests.Snapshots;

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
}

// Test events
public sealed record TestAggregateCreated(Guid Id, string Name);
public sealed record TestItemAdded(Guid AggregateId, decimal Amount);
public sealed record TestAggregateCompleted(Guid AggregateId);
