namespace Encina.Recoverability;

/// <summary>
/// The rules every retry layer applies to the metadata of an outbound error.
/// </summary>
/// <remarks>
/// <para>
/// A retry layer first classifies the error (<see cref="IErrorClassifier"/>) and never retries a
/// <see cref="ErrorClassification.Permanent"/> one. For a transient error it then asks these rules two things:
/// whether an automatic retry is safe (<see cref="AllowsAutomaticRetry"/>) and whether the API dictated when to
/// retry (<see cref="GetRetryAfter"/>).
/// </para>
/// </remarks>
public static class OutboundRetryRules
{
    /// <summary>
    /// Decides whether a transient error may be retried automatically.
    /// </summary>
    /// <param name="error">The error to retry.</param>
    /// <param name="idempotencyKey">
    /// The idempotency key of the request (<see cref="IRequestContext.IdempotencyKey"/>); a non-empty key is an
    /// opt-in, because the provider can deduplicate the retry.
    /// </param>
    /// <param name="allowNonIdempotentRetry">
    /// The explicit opt-in of the retry configuration for providers that deduplicate
    /// (<c>RetryAttribute.RetryNonIdempotent</c>).
    /// </param>
    /// <returns>
    /// <see langword="false"/> when the error is ambiguous (<see cref="OutboundErrorMetadataKeys.Ambiguous"/>: the
    /// request of a non-idempotent method may already have been processed) and neither opt-in holds;
    /// <see langword="true"/> otherwise.
    /// </returns>
    public static bool AllowsAutomaticRetry(EncinaError error, string? idempotencyKey, bool allowNonIdempotentRetry)
    {
        if (allowNonIdempotentRetry || !string.IsNullOrEmpty(idempotencyKey))
        {
            return true;
        }

        var details = error.GetDetails();
        return !(details.Count > 0
            && details.TryGetValue(OutboundErrorMetadataKeys.Ambiguous, out var ambiguous)
            && ambiguous is true);
    }

    /// <summary>
    /// Reads the delay that the API asked the caller to wait.
    /// </summary>
    /// <param name="error">The error to read.</param>
    /// <returns>The <see cref="OutboundErrorMetadataKeys.HttpRetryAfter"/> metadata, or <see langword="null"/> when the error has none.</returns>
    public static TimeSpan? GetRetryAfter(EncinaError error)
    {
        var details = error.GetDetails();
        return details.Count > 0
            && details.TryGetValue(OutboundErrorMetadataKeys.HttpRetryAfter, out var value)
            && value is TimeSpan delay
                ? delay
                : null;
    }
}
