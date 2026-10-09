using System.Text.Json.Serialization;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Source-generated JSON metadata for the decision audit: the evaluation trace and the attribute
/// names stored as metadata strings, and the lines of the JSON Lines export.
/// </summary>
/// <remarks>
/// Enums are written by name so a stored trace and an exported line stay readable and stable when
/// enum values are reordered.
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(List<PolicyEvaluationTrace>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, List<string>>))]
[JsonSerializable(typeof(ABACDecisionAuditRecord))]
internal sealed partial class ABACDecisionAuditJsonContext : JsonSerializerContext;
