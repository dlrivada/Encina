using Encina.Security.Audit;

using LanguageExt;

namespace Encina.UnitTests.Support;

/// <summary>
/// Unwraps the Either an <see cref="IOperationAuditEntryFactory"/> returns, so a test that expects an entry
/// fails with the error code when the factory returned a Left.
/// </summary>
internal static class OperationAuditEntryResultExtensions
{
    public static OperationAuditEntry Entry(this Either<EncinaError, OperationAuditEntry> result) =>
        result.Match(
            Right: entry => entry,
            Left: error => throw new InvalidOperationException(
                $"The factory returned {error.GetCode().IfNone("encina.unknown")} instead of an entry."));
}
