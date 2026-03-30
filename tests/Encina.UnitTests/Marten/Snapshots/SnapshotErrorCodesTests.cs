using Encina.Marten.Snapshots;

namespace Encina.UnitTests.Marten.Snapshots;

public sealed class SnapshotErrorCodesTests
{
    [Fact]
    public void Prefix_ShouldBeSnapshot() => SnapshotErrorCodes.Prefix.ShouldBe("snapshot");

    [Theory]
    [InlineData(nameof(SnapshotErrorCodes.LoadFailed))]
    [InlineData(nameof(SnapshotErrorCodes.SaveFailed))]
    [InlineData(nameof(SnapshotErrorCodes.DeleteFailed))]
    [InlineData(nameof(SnapshotErrorCodes.PruneFailed))]
    [InlineData(nameof(SnapshotErrorCodes.RestoreFailed))]
    [InlineData(nameof(SnapshotErrorCodes.InvalidVersion))]
    [InlineData(nameof(SnapshotErrorCodes.NotSnapshotable))]
    [InlineData(nameof(SnapshotErrorCodes.CreationSkipped))]
    public void AllCodes_ShouldStartWithPrefix(string fieldName)
    {
        var value = typeof(SnapshotErrorCodes).GetField(fieldName)?.GetValue(null) as string;
        value.ShouldNotBeNull();
        value.ShouldStartWith(SnapshotErrorCodes.Prefix);
    }

    [Fact]
    public void AllCodes_ShouldBeUnique()
    {
        var codes = new[]
        {
            SnapshotErrorCodes.LoadFailed, SnapshotErrorCodes.SaveFailed,
            SnapshotErrorCodes.DeleteFailed, SnapshotErrorCodes.PruneFailed,
            SnapshotErrorCodes.RestoreFailed, SnapshotErrorCodes.InvalidVersion,
            SnapshotErrorCodes.NotSnapshotable, SnapshotErrorCodes.CreationSkipped
        };
        codes.Distinct().Count().ShouldBe(codes.Length);
    }
}
