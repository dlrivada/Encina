using Encina.Marten.Snapshots;

namespace Encina.Marten.Tests.Snapshots;

public sealed class SnapshotErrorCodesTests
{
    [Fact]
    public void Prefix_IsSnapshot()
    {
        SnapshotErrorCodes.Prefix.Should().Be("snapshot");
    }

    [Theory]
    [InlineData(nameof(SnapshotErrorCodes.LoadFailed), "snapshot.load_failed")]
    [InlineData(nameof(SnapshotErrorCodes.SaveFailed), "snapshot.save_failed")]
    [InlineData(nameof(SnapshotErrorCodes.DeleteFailed), "snapshot.delete_failed")]
    [InlineData(nameof(SnapshotErrorCodes.PruneFailed), "snapshot.prune_failed")]
    [InlineData(nameof(SnapshotErrorCodes.RestoreFailed), "snapshot.restore_failed")]
    [InlineData(nameof(SnapshotErrorCodes.InvalidVersion), "snapshot.invalid_version")]
    [InlineData(nameof(SnapshotErrorCodes.NotSnapshotable), "snapshot.not_snapshotable")]
    [InlineData(nameof(SnapshotErrorCodes.CreationSkipped), "snapshot.creation_skipped")]
    public void ErrorCode_HasCorrectValue(string propertyName, string expectedValue)
    {
        // Arrange
        var property = typeof(SnapshotErrorCodes).GetField(propertyName);

        // Act
        var actualValue = property?.GetValue(null) as string;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    [Fact]
    public void AllErrorCodes_StartWithPrefix()
    {
        // Arrange
        var fields = typeof(SnapshotErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.Name != nameof(SnapshotErrorCodes.Prefix));

        // Act & Assert
        foreach (var field in fields)
        {
            var value = field.GetValue(null) as string;
            value.Should().StartWith(SnapshotErrorCodes.Prefix + ".",
                $"{field.Name} should start with '{SnapshotErrorCodes.Prefix}.'");
        }
    }

    [Fact]
    public void AllErrorCodes_AreUnique()
    {
        // Arrange
        var fields = typeof(SnapshotErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.Name != nameof(SnapshotErrorCodes.Prefix))
            .Select(f => f.GetValue(null) as string)
            .ToList();

        // Act & Assert
        fields.Should().OnlyHaveUniqueItems();
    }
}
