using Encina.Marten.Snapshots;
using Shouldly;

namespace Encina.Marten.Tests.Snapshots;

public sealed class SnapshotErrorCodesTests
{
    [Fact]
    public void Prefix_IsSnapshot()
    {
        // Arrange
        var expected = "snapshot";

        // Act
        var actual = SnapshotErrorCodes.Prefix;

        // Assert
        actual.ShouldBe(expected);
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

        // Assert
        property.ShouldNotBeNull($"Field '{propertyName}' should exist on SnapshotErrorCodes");
        var actualValue = property.GetValue(null) as string;
        actualValue.ShouldBe(expectedValue);
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
            value.ShouldNotBeNull($"Field '{field.Name}' should have a non-null string value");
            value.ShouldStartWith(SnapshotErrorCodes.Prefix + ".");
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
        fields.ShouldBeUnique();
    }
}
