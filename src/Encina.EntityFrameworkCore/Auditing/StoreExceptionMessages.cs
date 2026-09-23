using System.Data.Common;

namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Builds diagnostic messages for exceptions raised by the EF Core audit stores.
/// </summary>
/// <remarks>
/// EF Core wraps provider failures in <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>, whose message
/// only says "See the inner exception for details". Provider exception messages (for example SQL Server 2628,
/// "Truncated value: '...'") can embed column values, so appending the root cause's <see cref="Exception.Message"/>
/// to an <see cref="EncinaError"/> would leak data into logs and OpenTelemetry span status. This helper therefore
/// reports only the root cause's exception type and, when it is a <see cref="DbException"/>, a provider error
/// identifier (<see cref="DbException.SqlState"/> or <see cref="DbException.ErrorCode"/>) - never the message text
/// (#1128).
/// </remarks>
internal static class StoreExceptionMessages
{
    /// <summary>
    /// Returns the type of <paramref name="exception"/>'s root cause, followed by a provider error identifier
    /// when the root cause is a <see cref="DbException"/>.
    /// </summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>The diagnostic message, which never includes exception message text.</returns>
    public static string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var root = exception.GetBaseException();
        var identifier = GetProviderErrorIdentifier(root);

        return identifier is null
            ? root.GetType().Name
            : $"{root.GetType().Name} ({identifier})";
    }

    /// <summary>
    /// Extracts a provider error identifier from a <see cref="DbException"/>, preferring the ANSI SQLSTATE code
    /// over the provider-specific numeric error code.
    /// </summary>
    /// <param name="exception">The root cause exception.</param>
    /// <returns>The identifier, or <see langword="null"/> when unavailable.</returns>
    private static string? GetProviderErrorIdentifier(Exception exception)
    {
        if (exception is not DbException dbException)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(dbException.SqlState))
        {
            return $"SqlState={dbException.SqlState}";
        }

        return dbException.ErrorCode == 0
            ? null
            : $"ErrorCode={dbException.ErrorCode}";
    }
}
