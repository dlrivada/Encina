using Encina.Compliance.ProcessorAgreements.Model;

using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Compliance.ProcessorAgreements;

/// <summary>
/// Property-based tests for <see cref="Processor"/> verifying domain invariants
/// using FsCheck random data generation.
/// </summary>
public class ProcessorPropertyTests
{
    #region Identity Invariants

    /// <summary>
    /// Invariant: Setting Id always preserves the assigned value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Id_SetValue_AlwaysPreserved(NonEmptyString id)
    {
        var processor = CreateProcessor(id: id.Get);
        return processor.Id == id.Get;
    }

    /// <summary>
    /// Invariant: Setting Name always preserves the assigned value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Name_SetValue_AlwaysPreserved(NonEmptyString name)
    {
        var processor = CreateProcessor(name: name.Get);
        return processor.Name == name.Get;
    }

    /// <summary>
    /// Invariant: Setting Country always preserves the assigned value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Country_SetValue_AlwaysPreserved(NonEmptyString country)
    {
        var processor = CreateProcessor(country: country.Get);
        return processor.Country == country.Get;
    }

    #endregion

    #region Depth Invariants

    /// <summary>
    /// Invariant: Depth is always non-negative.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool Depth_NonNegativeInput_AlwaysNonNegative(NonNegativeInt depth)
    {
        var processor = CreateProcessor(depth: depth.Get);
        return processor.Depth >= 0;
    }

    /// <summary>
    /// Invariant: Top-level processors have Depth 0 and no parent.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool TopLevelProcessor_DepthZero_NoParent()
    {
        var processor = CreateProcessor(depth: 0, parentProcessorId: null);
        return processor.Depth == 0 && processor.ParentProcessorId is null;
    }

    #endregion

    #region SubProcessorAuthorizationType Invariants

    /// <summary>
    /// Invariant: SubProcessorAuthorizationType is always a valid enum value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SubProcessorAuthorizationType_AlwaysValidEnum()
    {
        var validValues = Enum.GetValues<SubProcessorAuthorizationType>();

        return validValues.All(authType =>
        {
            var processor = CreateProcessor(authType: authType);
            return Enum.IsDefined(processor.SubProcessorAuthorizationType);
        });
    }

    /// <summary>
    /// Invariant: SubProcessorAuthorizationType preserves the assigned value.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SubProcessorAuthorizationType_SetValue_AlwaysPreserved(bool useGeneral)
    {
        var authType = useGeneral
            ? SubProcessorAuthorizationType.General
            : SubProcessorAuthorizationType.Specific;

        var processor = CreateProcessor(authType: authType);
        return processor.SubProcessorAuthorizationType == authType;
    }

    #endregion

    #region Record Equality Invariants

    /// <summary>
    /// Invariant: 'with' expression creates a new instance preserving all other properties.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool WithExpression_PreservesOtherProperties(NonEmptyString name)
    {
        var original = CreateProcessor(name: "OriginalName");
        var modified = original with { Name = name.Get };

        return modified.Id == original.Id
            && modified.Name == name.Get
            && modified.Country == original.Country
            && modified.Depth == original.Depth
            && modified.SubProcessorAuthorizationType == original.SubProcessorAuthorizationType
            && modified.CreatedAtUtc == original.CreatedAtUtc;
    }

    #endregion

    #region Helpers

    private static Processor CreateProcessor(
        string? id = null,
        string? name = null,
        string? country = null,
        int depth = 0,
        string? parentProcessorId = null,
        SubProcessorAuthorizationType authType = SubProcessorAuthorizationType.Specific)
    {
        var now = DateTimeOffset.UtcNow;
        return new Processor
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name ?? "TestProcessor",
            Country = country ?? "DE",
            Depth = depth,
            ParentProcessorId = parentProcessorId,
            SubProcessorAuthorizationType = authType,
            CreatedAtUtc = now,
            LastUpdatedAtUtc = now
        };
    }

    #endregion
}
