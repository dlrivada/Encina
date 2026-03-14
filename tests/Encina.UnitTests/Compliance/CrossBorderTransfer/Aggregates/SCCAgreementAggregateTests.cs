#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Aggregates;
using Encina.Compliance.CrossBorderTransfer.Model;

using FluentAssertions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Aggregates;

public class SCCAgreementAggregateTests
{
    [Fact]
    public void Register_ValidParams_SetsAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var executedAt = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddYears(2);

        // Act
        var scc = SCCAgreementAggregate.Register(
            id, "processor-1", SCCModule.ControllerToProcessor, "2021/914",
            executedAt, expiresAt, "tenant-1", "module-1");

        // Assert
        scc.Id.Should().Be(id);
        scc.ProcessorId.Should().Be("processor-1");
        scc.Module.Should().Be(SCCModule.ControllerToProcessor);
        scc.SCCVersion.Should().Be("2021/914");
        scc.ExecutedAtUtc.Should().Be(executedAt);
        scc.ExpiresAtUtc.Should().Be(expiresAt);
        scc.TenantId.Should().Be("tenant-1");
        scc.ModuleId.Should().Be("module-1");
        scc.IsRevoked.Should().BeFalse();
        scc.IsExpired.Should().BeFalse();
        scc.RevokedAtUtc.Should().BeNull();
        scc.SupplementaryMeasures.Should().BeEmpty();
    }

    [Fact]
    public void Register_NullProcessorId_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => SCCAgreementAggregate.Register(
            Guid.NewGuid(), null!, SCCModule.ControllerToProcessor, "2021/914", DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("processorId");
    }

    [Fact]
    public void AddSupplementaryMeasure_Active_AddsMeasure()
    {
        // Arrange
        var scc = CreateActiveSCC();
        var measureId = Guid.NewGuid();

        // Act
        scc.AddSupplementaryMeasure(measureId, SupplementaryMeasureType.Technical, "Data encryption at rest");

        // Assert
        scc.SupplementaryMeasures.Should().HaveCount(1);
        scc.SupplementaryMeasures[0].Id.Should().Be(measureId);
        scc.SupplementaryMeasures[0].Type.Should().Be(SupplementaryMeasureType.Technical);
        scc.SupplementaryMeasures[0].Description.Should().Be("Data encryption at rest");
        scc.SupplementaryMeasures[0].IsImplemented.Should().BeFalse();
    }

    [Fact]
    public void AddSupplementaryMeasure_Revoked_ThrowsInvalidOperation()
    {
        // Arrange
        var scc = CreateRevokedSCC();

        // Act
        var act = () => scc.AddSupplementaryMeasure(
            Guid.NewGuid(), SupplementaryMeasureType.Organizational, "Staff training");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddSupplementaryMeasure_Expired_ThrowsInvalidOperation()
    {
        // Arrange
        var scc = CreateExpiredSCC();

        // Act
        var act = () => scc.AddSupplementaryMeasure(
            Guid.NewGuid(), SupplementaryMeasureType.Contractual, "Audit clause");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Revoke_Active_SetsIsRevoked()
    {
        // Arrange
        var scc = CreateActiveSCC();

        // Act
        scc.Revoke("Processor non-compliance", "admin");

        // Assert
        scc.IsRevoked.Should().BeTrue();
        scc.RevokedAtUtc.Should().NotBeNull();
        scc.RevokedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ThrowsInvalidOperation()
    {
        // Arrange
        var scc = CreateRevokedSCC();

        // Act
        var act = () => scc.Revoke("Second revocation", "admin");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Expire_Active_SetsIsExpired()
    {
        // Arrange
        var scc = CreateActiveSCC();

        // Act
        scc.Expire();

        // Assert
        scc.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void Expire_Revoked_ThrowsInvalidOperation()
    {
        // Arrange
        var scc = CreateRevokedSCC();

        // Act
        var act = () => scc.Expire();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void IsValid_Active_ReturnsTrue()
    {
        // Arrange
        var scc = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "processor-1", SCCModule.ControllerToProcessor, "2021/914",
            DateTimeOffset.UtcNow, expiresAtUtc: DateTimeOffset.UtcNow.AddYears(1));

        // Act
        var result = scc.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_Revoked_ReturnsFalse()
    {
        // Arrange
        var scc = CreateRevokedSCC();

        // Act
        var result = scc.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_PastExpiration_ReturnsFalse()
    {
        // Arrange
        var scc = SCCAgreementAggregate.Register(
            Guid.NewGuid(), "processor-1", SCCModule.ControllerToProcessor, "2021/914",
            DateTimeOffset.UtcNow.AddYears(-2), expiresAtUtc: DateTimeOffset.UtcNow.AddHours(-1));

        // Act
        var result = scc.IsValid(DateTimeOffset.UtcNow);

        // Assert
        result.Should().BeFalse();
    }

    // --- Helper methods ---

    private static SCCAgreementAggregate CreateActiveSCC()
    {
        return SCCAgreementAggregate.Register(
            Guid.NewGuid(), "processor-1", SCCModule.ControllerToProcessor, "2021/914", DateTimeOffset.UtcNow);
    }

    private static SCCAgreementAggregate CreateRevokedSCC()
    {
        var scc = CreateActiveSCC();
        scc.Revoke("Non-compliance", "admin");
        return scc;
    }

    private static SCCAgreementAggregate CreateExpiredSCC()
    {
        var scc = CreateActiveSCC();
        scc.Expire();
        return scc;
    }
}
