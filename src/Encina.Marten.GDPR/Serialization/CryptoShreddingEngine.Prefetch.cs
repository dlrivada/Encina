using Encina.Marten.GDPR.Abstractions;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.Marten.GDPR;

internal sealed partial class CryptoShreddingEngine
{
    /// <summary>
    /// Fetches, with the caller's token, every key and tombstone confirmation the pending owners need, so the
    /// apply pass finds them in the frame cache. Values that cannot be resolved here (missing subject, malformed
    /// token) are left to the apply pass, which throws for them.
    /// </summary>
    private async ValueTask PrefetchAsync(CryptoShreddingFrame frame, CancellationToken cancellationToken)
    {
        foreach (var (owner, plan) in frame.Pending)
        {
            foreach (var field in plan.Fields)
            {
                await PrefetchFieldAsync(frame, owner, field, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async ValueTask PrefetchFieldAsync(
        CryptoShreddingFrame frame, object owner, CryptoShreddedField field, CancellationToken cancellationToken)
    {
        if (field.Getter(owner) is not string stored || TryPrefetchSubject(frame, owner, field) is not { } subjectId)
        {
            return;
        }

        if (CryptoShreddingToken.IsTombstone(stored))
        {
            await PrefetchForgottenAsync(frame, subjectId, cancellationToken).ConfigureAwait(false);
        }
        else if (CryptoShreddingToken.TryParse(stored, out var token))
        {
            await PrefetchKeyAsync(frame, field, subjectId, token.Version, cancellationToken).ConfigureAwait(false);
        }
    }

    private static string? TryPrefetchSubject(CryptoShreddingFrame frame, object owner, CryptoShreddedField field)
    {
        try
        {
            var subjectId = field.ResolveSubjectId(owner);
            return frame.SubjectFilter is null || string.Equals(subjectId, frame.SubjectFilter, StringComparison.Ordinal)
                ? subjectId
                : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static async ValueTask PrefetchForgottenAsync(CryptoShreddingFrame frame, string subjectId, CancellationToken cancellationToken)
    {
        if (frame.ForgottenChecks.ContainsKey(subjectId))
        {
            return;
        }

        Either<EncinaError, bool> result;
        try
        {
            result = await frame.Services.GetRequiredService<ISubjectKeyProvider>()
                .IsSubjectForgottenAsync(subjectId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = CryptoShreddingErrors.KeyStoreError("IsSubjectForgotten", ex);
        }

        frame.ForgottenChecks[subjectId] = ToForgottenCheck(result);
    }

    private async ValueTask PrefetchKeyAsync(
        CryptoShreddingFrame frame, CryptoShreddedField field, string subjectId, int version, CancellationToken cancellationToken)
    {
        if (frame.ReadKeys.ContainsKey((subjectId, version)))
        {
            return;
        }

        Either<EncinaError, byte[]> result;
        try
        {
            result = await frame.Services.GetRequiredService<ISubjectKeyProvider>()
                .GetSubjectKeyAsync(subjectId, version, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result = CryptoShreddingErrors.KeyStoreError("GetSubjectKey", ex);
        }

        frame.ReadKeys[(subjectId, version)] = ToReadKey(frame, field, result);
    }
}
