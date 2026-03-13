#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="Processor"/>.
/// </summary>
public class ProcessorTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    #region Required Properties

    [Fact]
    public void Processor_ShouldHaveAllRequiredProperties()
    {
        var processor = new Processor
        {
            Id = "proc-001",
            Name = "Stripe",
            Country = "US",
            Depth = 0,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = Now,
            LastUpdatedAtUtc = Now
        };

        processor.Id.Should().Be("proc-001");
        processor.Name.Should().Be("Stripe");
        processor.Country.Should().Be("US");
        processor.Depth.Should().Be(0);
        processor.SubProcessorAuthorizationType.Should().Be(SubProcessorAuthorizationType.General);
        processor.CreatedAtUtc.Should().Be(Now);
        processor.LastUpdatedAtUtc.Should().Be(Now);
    }

    #endregion

    #region Optional Properties

    [Fact]
    public void Processor_OptionalProperties_ShouldDefaultToNull()
    {
        var processor = CreateTopLevelProcessor();

        processor.ContactEmail.Should().BeNull();
        processor.ParentProcessorId.Should().BeNull();
        processor.TenantId.Should().BeNull();
        processor.ModuleId.Should().BeNull();
    }

    [Fact]
    public void Processor_OptionalProperties_ShouldAcceptValues()
    {
        var processor = new Processor
        {
            Id = "proc-002",
            Name = "AWS",
            Country = "US",
            Depth = 0,
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
            ContactEmail = "dpo@aws.example.com",
            TenantId = "tenant-abc",
            ModuleId = "module-billing",
            CreatedAtUtc = Now,
            LastUpdatedAtUtc = Now
        };

        processor.ContactEmail.Should().Be("dpo@aws.example.com");
        processor.TenantId.Should().Be("tenant-abc");
        processor.ModuleId.Should().Be("module-billing");
    }

    #endregion

    #region Top-Level Processor

    [Fact]
    public void TopLevelProcessor_ShouldHaveNullParentAndZeroDepth()
    {
        var processor = CreateTopLevelProcessor();

        processor.ParentProcessorId.Should().BeNull();
        processor.Depth.Should().Be(0);
    }

    #endregion

    #region Sub-Processor

    [Fact]
    public void SubProcessor_ShouldHaveParentAndPositiveDepth()
    {
        var processor = new Processor
        {
            Id = "sub-proc-001",
            Name = "Sub-Processor",
            Country = "DE",
            Depth = 1,
            ParentProcessorId = "proc-001",
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.Specific,
            CreatedAtUtc = Now,
            LastUpdatedAtUtc = Now
        };

        processor.ParentProcessorId.Should().Be("proc-001");
        processor.Depth.Should().Be(1);
    }

    [Fact]
    public void SubSubProcessor_ShouldHaveDepthGreaterThanOne()
    {
        var processor = new Processor
        {
            Id = "sub-sub-proc-001",
            Name = "Sub-Sub-Processor",
            Country = "FR",
            Depth = 2,
            ParentProcessorId = "sub-proc-001",
            SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
            CreatedAtUtc = Now,
            LastUpdatedAtUtc = Now
        };

        processor.Depth.Should().Be(2);
        processor.ParentProcessorId.Should().Be("sub-proc-001");
    }

    #endregion

    #region SubProcessorAuthorizationType Enum

    [Fact]
    public void SubProcessorAuthorizationType_ShouldHaveExpectedValues()
    {
        ((int)SubProcessorAuthorizationType.Specific).Should().Be(0);
        ((int)SubProcessorAuthorizationType.General).Should().Be(1);
    }

    #endregion

    #region Record Equality and With Expression

    [Fact]
    public void Processor_RecordEquality_ShouldCompareByValue()
    {
        var processor1 = CreateTopLevelProcessor();
        var processor2 = CreateTopLevelProcessor();

        processor1.Should().Be(processor2);
    }

    [Fact]
    public void Processor_WithExpression_ShouldCreateNewInstance()
    {
        var original = CreateTopLevelProcessor();
        var modified = original with { Name = "Modified Processor" };

        modified.Should().NotBeSameAs(original);
        modified.Name.Should().Be("Modified Processor");
        modified.Id.Should().Be(original.Id);
        modified.Country.Should().Be(original.Country);
    }

    #endregion

    #region Helpers

    private static Processor CreateTopLevelProcessor() => new()
    {
        Id = "proc-001",
        Name = "Test Processor",
        Country = "DE",
        Depth = 0,
        SubProcessorAuthorizationType = SubProcessorAuthorizationType.General,
        CreatedAtUtc = Now,
        LastUpdatedAtUtc = Now
    };

    #endregion
}
