#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorMapper"/> static mapping methods.
/// </summary>
public class ProcessorMapperTests
{
    #region ToEntity Tests

    [Fact]
    public void ToEntity_ValidProcessor_MapsAllProperties()
    {
        // Arrange
        var processor = CreateProcessor();

        // Act
        var entity = ProcessorMapper.ToEntity(processor);

        // Assert
        entity.Id.Should().Be(processor.Id);
        entity.Name.Should().Be(processor.Name);
        entity.Country.Should().Be(processor.Country);
        entity.ContactEmail.Should().Be(processor.ContactEmail);
        entity.ParentProcessorId.Should().Be(processor.ParentProcessorId);
        entity.Depth.Should().Be(processor.Depth);
        entity.SubProcessorAuthorizationTypeValue.Should().Be((int)processor.SubProcessorAuthorizationType);
        entity.TenantId.Should().Be(processor.TenantId);
        entity.ModuleId.Should().Be(processor.ModuleId);
        entity.CreatedAtUtc.Should().Be(processor.CreatedAtUtc);
        entity.LastUpdatedAtUtc.Should().Be(processor.LastUpdatedAtUtc);
    }

    [Fact]
    public void ToEntity_NullProcessor_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ProcessorMapper.ToEntity(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ToDomain Tests

    [Fact]
    public void ToDomain_ValidEntity_MapsAllProperties()
    {
        // Arrange
        var entity = CreateEntity();

        // Act
        var processor = ProcessorMapper.ToDomain(entity);

        // Assert
        processor.Should().NotBeNull();
        processor!.Id.Should().Be(entity.Id);
        processor.Name.Should().Be(entity.Name);
        processor.Country.Should().Be(entity.Country);
        processor.ContactEmail.Should().Be(entity.ContactEmail);
        processor.ParentProcessorId.Should().Be(entity.ParentProcessorId);
        processor.Depth.Should().Be(entity.Depth);
        processor.SubProcessorAuthorizationType.Should().Be((SubProcessorAuthorizationType)entity.SubProcessorAuthorizationTypeValue);
        processor.TenantId.Should().Be(entity.TenantId);
        processor.ModuleId.Should().Be(entity.ModuleId);
        processor.CreatedAtUtc.Should().Be(entity.CreatedAtUtc);
        processor.LastUpdatedAtUtc.Should().Be(entity.LastUpdatedAtUtc);
    }

    [Fact]
    public void ToDomain_InvalidEnumValue_ReturnsNull()
    {
        // Arrange
        var entity = CreateEntity();
        entity.SubProcessorAuthorizationTypeValue = 99;

        // Act
        var processor = ProcessorMapper.ToDomain(entity);

        // Assert
        processor.Should().BeNull();
    }

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        // Act
        var act = () => ProcessorMapper.ToDomain(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void Roundtrip_ToEntityThenToDomain_PreservesValues()
    {
        // Arrange
        var original = CreateProcessor();

        // Act
        var entity = ProcessorMapper.ToEntity(original);
        var roundtripped = ProcessorMapper.ToDomain(entity);

        // Assert
        roundtripped.Should().NotBeNull();
        roundtripped!.Id.Should().Be(original.Id);
        roundtripped.Name.Should().Be(original.Name);
        roundtripped.Country.Should().Be(original.Country);
        roundtripped.ContactEmail.Should().Be(original.ContactEmail);
        roundtripped.ParentProcessorId.Should().Be(original.ParentProcessorId);
        roundtripped.Depth.Should().Be(original.Depth);
        roundtripped.SubProcessorAuthorizationType.Should().Be(original.SubProcessorAuthorizationType);
        roundtripped.TenantId.Should().Be(original.TenantId);
        roundtripped.ModuleId.Should().Be(original.ModuleId);
        roundtripped.CreatedAtUtc.Should().Be(original.CreatedAtUtc);
        roundtripped.LastUpdatedAtUtc.Should().Be(original.LastUpdatedAtUtc);
    }

    #endregion

    private static Processor CreateProcessor() => new()
    {
        Id = "proc-001",
        Name = "Stripe Payments",
        Country = "US",
        ContactEmail = "dpo@stripe.com",
        ParentProcessorId = "proc-parent",
        Depth = 1,
        SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
        TenantId = "tenant-abc",
        ModuleId = "module-payments",
        CreatedAtUtc = new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero),
        LastUpdatedAtUtc = new DateTimeOffset(2026, 2, 20, 14, 30, 0, TimeSpan.Zero)
    };

    private static ProcessorEntity CreateEntity() => new()
    {
        Id = "proc-entity-001",
        Name = "AWS Cloud Services",
        Country = "IE",
        ContactEmail = "privacy@aws.amazon.com",
        ParentProcessorId = null,
        Depth = 0,
        SubProcessorAuthorizationTypeValue = (int)SubProcessorAuthorizationType.Specific,
        TenantId = "tenant-xyz",
        ModuleId = null,
        CreatedAtUtc = new DateTimeOffset(2026, 3, 1, 8, 0, 0, TimeSpan.Zero),
        LastUpdatedAtUtc = new DateTimeOffset(2026, 3, 10, 16, 0, 0, TimeSpan.Zero)
    };
}
