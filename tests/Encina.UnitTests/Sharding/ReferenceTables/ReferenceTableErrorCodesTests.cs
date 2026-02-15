using Encina.Sharding.ReferenceTables;

namespace Encina.UnitTests.Sharding.ReferenceTables;

/// <summary>
/// Unit tests for <see cref="ReferenceTableErrorCodes"/>.
/// </summary>
public sealed class ReferenceTableErrorCodesTests
{
    // ────────────────────────────────────────────────────────────
    //  Error Code Values
    // ────────────────────────────────────────────────────────────

    #region Error Code Values

    [Fact]
    public void EntityNotRegistered_HasCorrectValue()
    {
        ReferenceTableErrorCodes.EntityNotRegistered
            .ShouldBe("encina.reference_table.entity_not_registered");
    }

    [Fact]
    public void PrimaryShardNotFound_HasCorrectValue()
    {
        ReferenceTableErrorCodes.PrimaryShardNotFound
            .ShouldBe("encina.reference_table.primary_shard_not_found");
    }

    [Fact]
    public void NoTargetShards_HasCorrectValue()
    {
        ReferenceTableErrorCodes.NoTargetShards
            .ShouldBe("encina.reference_table.no_target_shards");
    }

    [Fact]
    public void PrimaryReadFailed_HasCorrectValue()
    {
        ReferenceTableErrorCodes.PrimaryReadFailed
            .ShouldBe("encina.reference_table.primary_read_failed");
    }

    [Fact]
    public void ReplicationPartialFailure_HasCorrectValue()
    {
        ReferenceTableErrorCodes.ReplicationPartialFailure
            .ShouldBe("encina.reference_table.replication_partial_failure");
    }

    [Fact]
    public void ReplicationFailed_HasCorrectValue()
    {
        ReferenceTableErrorCodes.ReplicationFailed
            .ShouldBe("encina.reference_table.replication_failed");
    }

    [Fact]
    public void HashComputationFailed_HasCorrectValue()
    {
        ReferenceTableErrorCodes.HashComputationFailed
            .ShouldBe("encina.reference_table.hash_computation_failed");
    }

    [Fact]
    public void StoreNotRegistered_HasCorrectValue()
    {
        ReferenceTableErrorCodes.StoreNotRegistered
            .ShouldBe("encina.reference_table.store_not_registered");
    }

    [Fact]
    public void InvalidBatchSize_HasCorrectValue()
    {
        ReferenceTableErrorCodes.InvalidBatchSize
            .ShouldBe("encina.reference_table.invalid_batch_size");
    }

    [Fact]
    public void InvalidPollingInterval_HasCorrectValue()
    {
        ReferenceTableErrorCodes.InvalidPollingInterval
            .ShouldBe("encina.reference_table.invalid_polling_interval");
    }

    [Fact]
    public void MissingAttribute_HasCorrectValue()
    {
        ReferenceTableErrorCodes.MissingAttribute
            .ShouldBe("encina.reference_table.missing_attribute");
    }

    [Fact]
    public void ReplicationTimeout_HasCorrectValue()
    {
        ReferenceTableErrorCodes.ReplicationTimeout
            .ShouldBe("encina.reference_table.replication_timeout");
    }

    [Fact]
    public void UpsertFailed_HasCorrectValue()
    {
        ReferenceTableErrorCodes.UpsertFailed
            .ShouldBe("encina.reference_table.upsert_failed");
    }

    [Fact]
    public void GetAllFailed_HasCorrectValue()
    {
        ReferenceTableErrorCodes.GetAllFailed
            .ShouldBe("encina.reference_table.get_all_failed");
    }

    [Fact]
    public void NoPrimaryKeyFound_HasCorrectValue()
    {
        ReferenceTableErrorCodes.NoPrimaryKeyFound
            .ShouldBe("encina.reference_table.no_primary_key_found");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Namespace Convention
    // ────────────────────────────────────────────────────────────

    #region Namespace Convention

    [Theory]
    [InlineData(nameof(ReferenceTableErrorCodes.EntityNotRegistered))]
    [InlineData(nameof(ReferenceTableErrorCodes.PrimaryShardNotFound))]
    [InlineData(nameof(ReferenceTableErrorCodes.NoTargetShards))]
    [InlineData(nameof(ReferenceTableErrorCodes.PrimaryReadFailed))]
    [InlineData(nameof(ReferenceTableErrorCodes.ReplicationPartialFailure))]
    [InlineData(nameof(ReferenceTableErrorCodes.ReplicationFailed))]
    [InlineData(nameof(ReferenceTableErrorCodes.HashComputationFailed))]
    [InlineData(nameof(ReferenceTableErrorCodes.StoreNotRegistered))]
    [InlineData(nameof(ReferenceTableErrorCodes.InvalidBatchSize))]
    [InlineData(nameof(ReferenceTableErrorCodes.InvalidPollingInterval))]
    [InlineData(nameof(ReferenceTableErrorCodes.MissingAttribute))]
    [InlineData(nameof(ReferenceTableErrorCodes.ReplicationTimeout))]
    [InlineData(nameof(ReferenceTableErrorCodes.UpsertFailed))]
    [InlineData(nameof(ReferenceTableErrorCodes.GetAllFailed))]
    [InlineData(nameof(ReferenceTableErrorCodes.NoPrimaryKeyFound))]
    public void AllErrorCodes_FollowNamespaceConvention(string fieldName)
    {
        // Arrange
        var field = typeof(ReferenceTableErrorCodes).GetField(fieldName);
        var value = (string)field!.GetValue(null)!;

        // Assert
        value.ShouldStartWith("encina.reference_table.");
    }

    #endregion

    // ────────────────────────────────────────────────────────────
    //  Uniqueness
    // ────────────────────────────────────────────────────────────

    #region Uniqueness

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        // Arrange
        var fields = typeof(ReferenceTableErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        // Act & Assert
        fields.Count.ShouldBe(fields.Distinct().Count());
    }

    #endregion
}
