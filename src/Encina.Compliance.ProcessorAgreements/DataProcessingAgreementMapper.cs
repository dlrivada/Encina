using System.Text.Json;

using Encina.Compliance.ProcessorAgreements.Model;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Maps between <see cref="DataProcessingAgreement"/> domain records and
/// <see cref="DataProcessingAgreementEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This mapper handles the following type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="DataProcessingAgreement.Status"/> (<see cref="DPAStatus"/>) ↔
/// <see cref="DataProcessingAgreementEntity.StatusValue"/> (<see cref="int"/>).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DataProcessingAgreement.MandatoryTerms"/> (<see cref="DPAMandatoryTerms"/>) ↔
/// 8 individual boolean columns in <see cref="DataProcessingAgreementEntity"/>.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DataProcessingAgreement.ProcessingPurposes"/>
/// (<see cref="IReadOnlyList{String}"/>) ↔
/// <see cref="DataProcessingAgreementEntity.ProcessingPurposesJson"/> (<see cref="string"/>),
/// using JSON serialization for portable storage.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Invalid enum values in a persistence entity result in a <c>null</c> return from
/// <see cref="ToDomain"/>. Invalid JSON in <see cref="DataProcessingAgreementEntity.ProcessingPurposesJson"/>
/// also results in a <c>null</c> return.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve Data Processing Agreements without coupling to the domain model.
/// </para>
/// </remarks>
public static class DataProcessingAgreementMapper
{
    /// <summary>
    /// Converts a domain <see cref="DataProcessingAgreement"/> to a persistence entity.
    /// </summary>
    /// <param name="agreement">The domain record to convert.</param>
    /// <returns>A <see cref="DataProcessingAgreementEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="agreement"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The <see cref="DPAMandatoryTerms"/> record is flattened into 8 individual boolean columns,
    /// and <see cref="DataProcessingAgreement.ProcessingPurposes"/> is serialized as JSON.
    /// </remarks>
    public static DataProcessingAgreementEntity ToEntity(DataProcessingAgreement agreement)
    {
        ArgumentNullException.ThrowIfNull(agreement);

        return new DataProcessingAgreementEntity
        {
            Id = agreement.Id,
            ProcessorId = agreement.ProcessorId,
            StatusValue = (int)agreement.Status,
            SignedAtUtc = agreement.SignedAtUtc,
            ExpiresAtUtc = agreement.ExpiresAtUtc,
            HasSCCs = agreement.HasSCCs,
            ProcessingPurposesJson = JsonSerializer.Serialize(agreement.ProcessingPurposes),
            ProcessOnDocumentedInstructions = agreement.MandatoryTerms.ProcessOnDocumentedInstructions,
            ConfidentialityObligations = agreement.MandatoryTerms.ConfidentialityObligations,
            SecurityMeasures = agreement.MandatoryTerms.SecurityMeasures,
            SubProcessorRequirements = agreement.MandatoryTerms.SubProcessorRequirements,
            DataSubjectRightsAssistance = agreement.MandatoryTerms.DataSubjectRightsAssistance,
            ComplianceAssistance = agreement.MandatoryTerms.ComplianceAssistance,
            DataDeletionOrReturn = agreement.MandatoryTerms.DataDeletionOrReturn,
            AuditRights = agreement.MandatoryTerms.AuditRights,
            TenantId = agreement.TenantId,
            ModuleId = agreement.ModuleId,
            CreatedAtUtc = agreement.CreatedAtUtc,
            LastUpdatedAtUtc = agreement.LastUpdatedAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="DataProcessingAgreement"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>
    /// A <see cref="DataProcessingAgreement"/> if the entity state is valid (all enum values are defined
    /// and JSON deserialization succeeds), or <c>null</c> if the entity contains invalid values.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    /// <remarks>
    /// The 8 individual boolean columns are rehydrated into a <see cref="DPAMandatoryTerms"/> record,
    /// and <see cref="DataProcessingAgreementEntity.ProcessingPurposesJson"/> is deserialized from JSON.
    /// </remarks>
    public static DataProcessingAgreement? ToDomain(DataProcessingAgreementEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!Enum.IsDefined(typeof(DPAStatus), entity.StatusValue))
        {
            return null;
        }

        List<string>? processingPurposes;
        try
        {
            processingPurposes = !string.IsNullOrEmpty(entity.ProcessingPurposesJson)
                ? JsonSerializer.Deserialize<List<string>>(entity.ProcessingPurposesJson)
                : [];
        }
        catch (JsonException)
        {
            return null;
        }

        return new DataProcessingAgreement
        {
            Id = entity.Id,
            ProcessorId = entity.ProcessorId,
            Status = (DPAStatus)entity.StatusValue,
            SignedAtUtc = entity.SignedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            HasSCCs = entity.HasSCCs,
            ProcessingPurposes = processingPurposes ?? [],
            MandatoryTerms = new DPAMandatoryTerms
            {
                ProcessOnDocumentedInstructions = entity.ProcessOnDocumentedInstructions,
                ConfidentialityObligations = entity.ConfidentialityObligations,
                SecurityMeasures = entity.SecurityMeasures,
                SubProcessorRequirements = entity.SubProcessorRequirements,
                DataSubjectRightsAssistance = entity.DataSubjectRightsAssistance,
                ComplianceAssistance = entity.ComplianceAssistance,
                DataDeletionOrReturn = entity.DataDeletionOrReturn,
                AuditRights = entity.AuditRights
            },
            TenantId = entity.TenantId,
            ModuleId = entity.ModuleId,
            CreatedAtUtc = entity.CreatedAtUtc,
            LastUpdatedAtUtc = entity.LastUpdatedAtUtc
        };
    }
}
