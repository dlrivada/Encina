using System.Reflection;
using Tags = Encina.OpenTelemetry.ActivityTagNames;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Tests for <see cref="Encina.OpenTelemetry.ActivityTagNames"/> constants.
/// </summary>
public class ActivityTagNamesTests
{
    #region Messaging

    [Fact]
    public void Messaging_System_ShouldBeMessagingSystem()
    {
        Tags.Messaging.System.ShouldBe("messaging.system");
    }

    [Fact]
    public void Messaging_OperationName_ShouldBeCorrect()
    {
        Tags.Messaging.OperationName.ShouldBe("messaging.operation.name");
    }

    [Fact]
    public void Messaging_MessageId_ShouldBeCorrect()
    {
        Tags.Messaging.MessageId.ShouldBe("messaging.message.id");
    }

    #endregion

    #region Systems

    [Fact]
    public void Systems_Outbox_ShouldBeEncinaOutbox()
    {
        Tags.Systems.Outbox.ShouldBe("encina.outbox");
    }

    [Fact]
    public void Systems_Inbox_ShouldBeEncinaInbox()
    {
        Tags.Systems.Inbox.ShouldBe("encina.inbox");
    }

    [Fact]
    public void Systems_Scheduling_ShouldBeEncinaScheduling()
    {
        Tags.Systems.Scheduling.ShouldBe("encina.scheduling");
    }

    #endregion

    #region Operations

    [Fact]
    public void Operations_Publish_ShouldBePublish()
    {
        Tags.Operations.Publish.ShouldBe("publish");
    }

    [Fact]
    public void Operations_Receive_ShouldBeReceive()
    {
        Tags.Operations.Receive.ShouldBe("receive");
    }

    [Fact]
    public void Operations_Schedule_ShouldBeSchedule()
    {
        Tags.Operations.Schedule.ShouldBe("schedule");
    }

    #endregion

    #region Saga

    [Fact]
    public void Saga_Id_ShouldBeSagaId()
    {
        Tags.Saga.Id.ShouldBe("saga.id");
    }

    [Fact]
    public void Saga_Type_ShouldBeSagaType()
    {
        Tags.Saga.Type.ShouldBe("saga.type");
    }

    #endregion

    #region CDC

    [Fact]
    public void Cdc_ConnectorId_ShouldBeCorrect()
    {
        Tags.Cdc.ConnectorId.ShouldBe("cdc.connector_id");
    }

    [Fact]
    public void Cdc_ShardId_ShouldBeCorrect()
    {
        Tags.Cdc.ShardId.ShouldBe("cdc.shard_id");
    }

    #endregion

    #region Repository

    [Fact]
    public void Repository_Operation_ShouldBeCorrect()
    {
        Tags.Repository.Operation.ShouldBe("repository.operation");
    }

    [Fact]
    public void Repository_EntityType_ShouldBeCorrect()
    {
        Tags.Repository.EntityType.ShouldBe("repository.entity_type");
    }

    #endregion

    #region Uniqueness

    [Fact]
    public void AllConstants_AcrossAllGroups_ShouldBeUnique()
    {
        var allValues = new List<string>();
        var tagType = typeof(global::Encina.OpenTelemetry.ActivityTagNames);

        foreach (var nestedType in tagType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = (string)field.GetRawConstantValue()!;
                allValues.Add(value);
            }
        }

        allValues.Count.ShouldBeGreaterThan(50);
        allValues.Distinct().Count().ShouldBe(allValues.Count,
            $"Duplicate tag values found: {string.Join(", ", allValues.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key))}");
    }

    [Fact]
    public void AllConstants_ShouldNotBeNullOrEmpty()
    {
        var tagType = typeof(global::Encina.OpenTelemetry.ActivityTagNames);

        foreach (var nestedType in tagType.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            var fields = nestedType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = (string)field.GetRawConstantValue()!;
                value.ShouldNotBeNullOrWhiteSpace($"{nestedType.Name}.{field.Name} should not be null or empty");
            }
        }
    }

    #endregion

    #region Specific Group Spot Checks

    [Fact]
    public void Colocation_Group_ShouldHaveEncinaPrefix()
    {
        Tags.Colocation.Group.ShouldStartWith("encina.sharding");
    }

    [Fact]
    public void Shadow_ProductionShard_ShouldHaveEncinaPrefix()
    {
        Tags.Shadow.ProductionShard.ShouldStartWith("encina.sharding");
    }

    [Fact]
    public void Tiering_ShardTier_ShouldBeCorrect()
    {
        Tags.Tiering.ShardTier.ShouldBe("shard.tier");
    }

    [Fact]
    public void Resharding_Id_ShouldBeCorrect()
    {
        Tags.Resharding.Id.ShouldBe("resharding.id");
    }

    [Fact]
    public void Migration_Strategy_ShouldBeCorrect()
    {
        Tags.Migration.Strategy.ShouldBe("migration.strategy");
    }

    [Fact]
    public void UnitOfWork_Outcome_ShouldBeCorrect()
    {
        Tags.UnitOfWork.Outcome.ShouldBe("uow.outcome");
    }

    [Fact]
    public void Bulk_Operation_ShouldBeCorrect()
    {
        Tags.Bulk.Operation.ShouldBe("bulk.operation");
    }

    [Fact]
    public void SoftDelete_Operation_ShouldBeCorrect()
    {
        Tags.SoftDelete.Operation.ShouldBe("softdelete.operation");
    }

    [Fact]
    public void Audit_EntityType_ShouldBeCorrect()
    {
        Tags.Audit.EntityType.ShouldBe("audit.entity_type");
    }

    [Fact]
    public void Tenancy_TenantId_ShouldBeCorrect()
    {
        Tags.Tenancy.TenantId.ShouldBe("tenancy.tenant_id");
    }

    [Fact]
    public void Modules_Name_ShouldBeCorrect()
    {
        Tags.Modules.Name.ShouldBe("module.name");
    }

    [Fact]
    public void QueryCache_Outcome_ShouldBeCorrect()
    {
        Tags.QueryCache.Outcome.ShouldBe("querycache.outcome");
    }

    [Fact]
    public void Specification_Name_ShouldBeCorrect()
    {
        Tags.Specification.Name.ShouldBe("specification.name");
    }

    [Fact]
    public void EventMetadata_CorrelationId_ShouldBeCorrect()
    {
        Tags.EventMetadata.CorrelationId.ShouldBe("event.correlation_id");
    }

    [Fact]
    public void ReferenceTable_EntityType_ShouldBeCorrect()
    {
        Tags.ReferenceTable.EntityType.ShouldBe("reference_table.entity_type");
    }

    #endregion
}
