using Encina.DomainModeling;
using Encina.DomainModeling.Auditing;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Encina.MongoDB.Inbox;
using Encina.MongoDB.Outbox;
using Encina.MongoDB.Sagas;
using Encina.MongoDB.Scheduling;
using Encina.MongoDB.UnitOfWork;
using Shouldly;

namespace Encina.ContractTests.MongoDB;

/// <summary>
/// Contract tests verifying MongoDB implementations conform to messaging interfaces.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "MongoDB")]
public sealed class MongoDbContractTests
{
    [Fact]
    public void InboxMessage_ImplementsIInboxMessage()
    {
        typeof(InboxMessage).GetInterfaces().ShouldContain(typeof(IInboxMessage));
    }

    [Fact]
    public void OutboxMessage_ImplementsIOutboxMessage()
    {
        typeof(OutboxMessage).GetInterfaces().ShouldContain(typeof(IOutboxMessage));
    }

    [Fact]
    public void SagaState_ImplementsISagaState()
    {
        typeof(SagaState).GetInterfaces().ShouldContain(typeof(ISagaState));
    }

    [Fact]
    public void ScheduledMessage_ImplementsIScheduledMessage()
    {
        typeof(ScheduledMessage).GetInterfaces().ShouldContain(typeof(IScheduledMessage));
    }

    [Fact]
    public void UnitOfWorkMongoDB_ImplementsIUnitOfWork()
    {
        typeof(UnitOfWorkMongoDB).GetInterfaces().ShouldContain(typeof(IUnitOfWork));
    }

    [Fact]
    public void InboxMessage_HasRequiredProperties()
    {
        var props = typeof(InboxMessage).GetProperties();
        props.ShouldContain(p => p.Name == "MessageId");
        props.ShouldContain(p => p.Name == "RequestType");
        props.ShouldContain(p => p.Name == "IsProcessed");
    }

    [Fact]
    public void OutboxMessage_HasRequiredProperties()
    {
        var props = typeof(OutboxMessage).GetProperties();
        props.ShouldContain(p => p.Name == "Id");
        props.ShouldContain(p => p.Name == "NotificationType");
        props.ShouldContain(p => p.Name == "IsProcessed");
    }

    [Fact]
    public void SagaState_HasRequiredProperties()
    {
        var props = typeof(SagaState).GetProperties();
        props.ShouldContain(p => p.Name == "SagaId");
        props.ShouldContain(p => p.Name == "SagaType");
        props.ShouldContain(p => p.Name == "Status");
        props.ShouldContain(p => p.Name == "CurrentStep");
    }
}
