using Encina.Cdc.Abstractions;

namespace Encina.Cdc;

/// <summary>
/// Metadata associated with a captured change event, including position tracking
/// and source information for provenance.
/// </summary>
/// <param name="Position">The CDC position at which this change was captured, used for resume after restart.</param>
/// <param name="CapturedAtUtc">The UTC timestamp when the change was captured from the source.</param>
/// <param name="TransactionId">The optional database transaction identifier that produced this change.</param>
/// <param name="SourceDatabase">The optional name of the source database.</param>
/// <param name="SourceSchema">The optional name of the source schema.</param>
public sealed record ChangeMetadata(
    CdcPosition Position,
    DateTime CapturedAtUtc,
    string? TransactionId,
    string? SourceDatabase,
    string? SourceSchema);
