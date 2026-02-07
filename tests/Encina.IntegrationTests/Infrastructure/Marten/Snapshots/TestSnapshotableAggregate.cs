using Encina.DomainModeling;
using Encina.Marten.Snapshots;

namespace Encina.IntegrationTests.Infrastructure.Marten.Snapshots;

/// <summary>
/// Test aggregate that supports snapshotting.
/// </summary>
public sealed class TestSnapshotableAggregate : AggregateBase, ISnapshotable<TestSnapshotableAggregate>
{
    // Use 'set' instead of 'private set' to allow JSON deserialization by Marten
    public string Name { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public string Status { get; set; } = "Created";

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
