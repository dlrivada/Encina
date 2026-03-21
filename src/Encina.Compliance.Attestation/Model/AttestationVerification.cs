namespace Encina.Compliance.Attestation.Model;

/// <summary>
/// Result of verifying an attestation receipt.
/// </summary>
public sealed record AttestationVerification
{
    /// <summary>
    /// Gets whether the attestation receipt is valid and the referenced audit record is untampered.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when verification was performed.
    /// </summary>
    public required DateTimeOffset VerifiedAtUtc { get; init; }

    /// <summary>
    /// Gets the reason for verification failure, if any.
    /// </summary>
    public string? FailureReason { get; init; }
}
