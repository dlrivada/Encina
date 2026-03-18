using Encina.Compliance.DataResidency.Aggregates;
using Encina.Compliance.DataResidency.Model;

namespace Encina.GuardTests.Compliance.DataResidency;

/// <summary>
/// Guard tests for <see cref="DataLocationAggregate"/> to verify null, empty, and whitespace
/// parameter handling across all factory and instance methods.
/// </summary>
public class DataLocationAggregateGuardTests
{
    #region Register Guards — entityId

    [Fact]
    public void Register_NullEntityId_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), null!, "personal-data", "DE", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Register_EmptyEntityId_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "", "personal-data", "DE", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    [Fact]
    public void Register_WhitespaceEntityId_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "   ", "personal-data", "DE", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("entityId");
    }

    #endregion

    #region Register Guards — dataCategory

    [Fact]
    public void Register_NullDataCategory_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", null!, "DE", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Register_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "", "DE", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void Register_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "   ", "DE", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region Register Guards — regionCode

    [Fact]
    public void Register_NullRegionCode_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", null!, StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("regionCode");
    }

    [Fact]
    public void Register_EmptyRegionCode_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("regionCode");
    }

    [Fact]
    public void Register_WhitespaceRegionCode_ThrowsArgumentException()
    {
        var act = () => DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "   ", StorageType.Primary, DateTimeOffset.UtcNow);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("regionCode");
    }

    #endregion

    #region Migrate Guards — newRegionCode

    [Fact]
    public void Migrate_NullNewRegionCode_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Migrate(null!, "Business requirement");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newRegionCode");
    }

    [Fact]
    public void Migrate_EmptyNewRegionCode_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Migrate("", "Business requirement");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newRegionCode");
    }

    [Fact]
    public void Migrate_WhitespaceNewRegionCode_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Migrate("   ", "Business requirement");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("newRegionCode");
    }

    #endregion

    #region Migrate Guards — reason

    [Fact]
    public void Migrate_NullReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Migrate("FR", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Migrate_EmptyReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Migrate("FR", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Migrate_WhitespaceReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Migrate("FR", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region Remove Guards — reason

    [Fact]
    public void Remove_NullReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Remove(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Remove_EmptyReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Remove("");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Remove_WhitespaceReason_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.Remove("   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    #endregion

    #region DetectViolation Guards — dataCategory

    [Fact]
    public void DetectViolation_NullDataCategory_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation(null!, "US", "Data found in non-compliant region");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void DetectViolation_EmptyDataCategory_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("", "US", "Data found in non-compliant region");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    [Fact]
    public void DetectViolation_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("   ", "US", "Data found in non-compliant region");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region DetectViolation Guards — violatingRegionCode

    [Fact]
    public void DetectViolation_NullViolatingRegionCode_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("personal-data", null!, "Data found in non-compliant region");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("violatingRegionCode");
    }

    [Fact]
    public void DetectViolation_EmptyViolatingRegionCode_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("personal-data", "", "Data found in non-compliant region");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("violatingRegionCode");
    }

    [Fact]
    public void DetectViolation_WhitespaceViolatingRegionCode_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("personal-data", "   ", "Data found in non-compliant region");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("violatingRegionCode");
    }

    #endregion

    #region DetectViolation Guards — details

    [Fact]
    public void DetectViolation_NullDetails_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("personal-data", "US", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("details");
    }

    [Fact]
    public void DetectViolation_EmptyDetails_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("personal-data", "US", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("details");
    }

    [Fact]
    public void DetectViolation_WhitespaceDetails_ThrowsArgumentException()
    {
        var aggregate = CreateActiveLocation();

        var act = () => aggregate.DetectViolation("personal-data", "US", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("details");
    }

    #endregion

    #region ResolveViolation Guards — resolution

    [Fact]
    public void ResolveViolation_NullResolution_ThrowsArgumentException()
    {
        var aggregate = CreateLocationWithViolation();

        var act = () => aggregate.ResolveViolation(null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("resolution");
    }

    [Fact]
    public void ResolveViolation_EmptyResolution_ThrowsArgumentException()
    {
        var aggregate = CreateLocationWithViolation();

        var act = () => aggregate.ResolveViolation("");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("resolution");
    }

    [Fact]
    public void ResolveViolation_WhitespaceResolution_ThrowsArgumentException()
    {
        var aggregate = CreateLocationWithViolation();

        var act = () => aggregate.ResolveViolation("   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("resolution");
    }

    #endregion

    #region Helpers

    private static DataLocationAggregate CreateActiveLocation()
    {
        return DataLocationAggregate.Register(
            Guid.NewGuid(), "entity-1", "personal-data", "DE",
            StorageType.Primary, DateTimeOffset.UtcNow);
    }

    private static DataLocationAggregate CreateLocationWithViolation()
    {
        var aggregate = CreateActiveLocation();
        aggregate.DetectViolation("personal-data", "US", "Data found in non-compliant region");
        return aggregate;
    }

    #endregion
}
