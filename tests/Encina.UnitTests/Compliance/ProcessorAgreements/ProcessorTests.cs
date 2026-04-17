#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.ProcessorAgreements.Model;

using Shouldly;

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

        processor.Id.ShouldBe("proc-001");
        processor.Name.ShouldBe("Stripe");
        processor.Country.ShouldBe("US");
        processor.Depth.ShouldBe(0);
        processor.SubProcessorAuthorizationType.ShouldBe(SubProcessorAuthorizationType.General);
        processor.CreatedAtUtc.ShouldBe(Now);
        processor.LastUpdatedAtUtc.ShouldBe(Now);
    }

    #endregion

    #region Optional Properties

    [Fact]
    public void Processor_OptionalProperties_ShouldDefaultToNull()
    {
        var processor = CreateTopLevelProcessor();

        processor.ContactEmail.ShouldBeNull();
        processor.ParentProcessorId.ShouldBeNull();
        processor.TenantId.ShouldBeNull();
        processor.ModuleId.ShouldBeNull();
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

        processor.ContactEmail.ShouldBe("dpo@aws.example.com");
        processor.TenantId.ShouldBe("tenant-abc");
        processor.ModuleId.ShouldBe("module-billing");
    }

    #endregion

    #region Top-Level Processor

    [Fact]
    public void TopLevelProcessor_ShouldHaveNullParentAndZeroDepth()
    {
        var processor = CreateTopLevelProcessor();

        processor.ParentProcessorId.ShouldBeNull();
        processor.Depth.ShouldBe(0);
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

        processor.ParentProcessorId.ShouldBe("proc-001");
        processor.Depth.ShouldBe(1);
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

        processor.Depth.ShouldBe(2);
        processor.ParentProcessorId.ShouldBe("sub-proc-001");
    }

    #endregion

    #region SubProcessorAuthorizationType Enum

    [Fact]
    public void SubProcessorAuthorizationType_ShouldHaveExpectedValues()
    {
        ((int)SubProcessorAuthorizationType.Specific).ShouldBe(0);
        ((int)SubProcessorAuthorizationType.General).ShouldBe(1);
    }

    #endregion

    #region Record Equality and With Expression

    [Fact]
    public void Processor_RecordEquality_ShouldCompareByValue()
    {
        var processor1 = CreateTopLevelProcessor();
        var processor2 = CreateTopLevelProcessor();

        processor1.ShouldBe(processor2);
    }

    [Fact]
    public void Processor_WithExpression_ShouldCreateNewInstance()
    {
        var original = CreateTopLevelProcessor();
        var modified = original with { Name = "Modified Processor" };

        modified.ShouldNotBeSameAs(original);
        modified.Name.ShouldBe("Modified Processor");
        modified.Id.ShouldBe(original.Id);
        modified.Country.ShouldBe(original.Country);
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
