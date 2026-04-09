#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="SCCAgreementAggregate"/> verifying argument validation
/// and state transition guards on all factory and instance methods.
/// </summary>
public class SCCAgreementAggregateGuardTests
{
    private static readonly DateTimeOffset ExecutedAt = DateTimeOffset.UtcNow.AddDays(-30);

    #region Register Guards

    [Fact]
    public void Register_NullProcessorId_ThrowsArgumentException()
    {
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), null!, SCCModule.ControllerToProcessor, "2021/914", ExecutedAt);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorId");
    }

    [Fact]
    public void Register_EmptyProcessorId_ThrowsArgumentException()
    {
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), "", SCCModule.ControllerToProcessor, "2021/914", ExecutedAt);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorId");
    }

    [Fact]
    public void Register_WhitespaceProcessorId_ThrowsArgumentException()
    {
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), "   ", SCCModule.ControllerToProcessor, "2021/914", ExecutedAt);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("processorId");
    }

    [Fact]
    public void Register_NullVersion_ThrowsArgumentException()
    {
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, null!, ExecutedAt);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("version");
    }

    [Fact]
    public void Register_EmptyVersion_ThrowsArgumentException()
    {
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, "", ExecutedAt);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("version");
    }

    [Fact]
    public void Register_WhitespaceVersion_ThrowsArgumentException()
    {
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, "   ", ExecutedAt);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("version");
    }

    [Fact]
    public void Register_ValidParameters_ReturnsAggregate()
    {
        var aggregate = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, "2021/914", ExecutedAt);

        aggregate.ShouldNotBeNull();
        aggregate.ProcessorId.ShouldBe("proc-1");
        aggregate.Module.ShouldBe(SCCModule.ControllerToProcessor);
        aggregate.SCCVersion.ShouldBe("2021/914");
    }

    #endregion

    #region AddSupplementaryMeasure Guards

    [Fact]
    public void AddSupplementaryMeasure_NullDescription_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void AddSupplementaryMeasure_EmptyDescription_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void AddSupplementaryMeasure_WhitespaceDescription_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("description");
    }

    [Fact]
    public void AddSupplementaryMeasure_WhenRevoked_ThrowsInvalidOperationException()
    {
        var sut = CreateRevokedAggregate();

        var act = () => sut.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AddSupplementaryMeasure_WhenExpired_ThrowsInvalidOperationException()
    {
        var sut = CreateExpiredAggregate();

        var act = () => sut.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void AddSupplementaryMeasure_ValidOnActive_AddsMeasure()
    {
        var sut = CreateActiveAggregate();

        sut.AddSupplementaryMeasure(Guid.NewGuid(), SupplementaryMeasureType.Technical, "Encryption at rest");

        sut.SupplementaryMeasures.Count.ShouldBe(1);
    }

    #endregion

    #region Revoke Guards

    [Fact]
    public void Revoke_NullReason_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke(null!, "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_EmptyReason_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("", "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_WhitespaceReason_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("   ", "admin1");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("reason");
    }

    [Fact]
    public void Revoke_NullRevokedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("Non-compliance", null!);

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("revokedBy");
    }

    [Fact]
    public void Revoke_EmptyRevokedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("Non-compliance", "");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("revokedBy");
    }

    [Fact]
    public void Revoke_WhitespaceRevokedBy_ThrowsArgumentException()
    {
        var sut = CreateActiveAggregate();

        var act = () => sut.Revoke("Non-compliance", "   ");

        Should.Throw<ArgumentException>(act).ParamName.ShouldBe("revokedBy");
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperationException()
    {
        var sut = CreateRevokedAggregate();

        var act = () => sut.Revoke("Another reason", "admin2");

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Revoke_ValidOnActive_SetsRevoked()
    {
        var sut = CreateActiveAggregate();

        sut.Revoke("Non-compliance detected", "admin1");

        sut.IsRevoked.ShouldBeTrue();
        sut.RevokedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Expire Guards

    [Fact]
    public void Expire_AlreadyExpired_ThrowsInvalidOperationException()
    {
        var sut = CreateExpiredAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_WhenRevoked_ThrowsInvalidOperationException()
    {
        var sut = CreateRevokedAggregate();

        var act = () => sut.Expire();

        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void Expire_ValidOnActive_SetsExpired()
    {
        var sut = CreateActiveAggregate();

        sut.Expire();

        sut.IsExpired.ShouldBeTrue();
    }

    #endregion

    #region IsValid Guards

    [Fact]
    public void IsValid_ActiveAgreement_ReturnsTrue()
    {
        var sut = CreateActiveAggregate();

        sut.IsValid(DateTimeOffset.UtcNow).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_RevokedAgreement_ReturnsFalse()
    {
        var sut = CreateRevokedAggregate();

        sut.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_ExpiredAgreement_ReturnsFalse()
    {
        var sut = CreateExpiredAggregate();

        sut.IsValid(DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_PastExpirationDate_ReturnsFalse()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(10);
        var sut = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, "2021/914",
            ExecutedAt, expiresAt);

        sut.IsValid(expiresAt.AddDays(1)).ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static SCCAgreementAggregate CreateActiveAggregate() =>
        SCCAgreementAggregate.Register(
            Guid.NewGuid(), "proc-1", SCCModule.ControllerToProcessor, "2021/914", ExecutedAt);

    private static SCCAgreementAggregate CreateRevokedAggregate()
    {
        var agg = CreateActiveAggregate();
        agg.Revoke("Compliance violation", "admin1");
        return agg;
    }

    private static SCCAgreementAggregate CreateExpiredAggregate()
    {
        var agg = CreateActiveAggregate();
        agg.Expire();
        return agg;
    }

    #endregion
}
