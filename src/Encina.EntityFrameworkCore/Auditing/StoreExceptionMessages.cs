namespace Encina.EntityFrameworkCore.Auditing;

/// <summary>
/// Builds diagnostic messages for exceptions raised by the EF Core audit stores.
/// </summary>
/// <remarks>
/// EF Core wraps provider failures in <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>, whose message
/// only says "See the inner exception for details". The message of the root cause (for example the provider's
/// <c>DbException</c>) is what tells an operator why the write failed, so it is appended to the summary (#1128).
/// </remarks>
internal static class StoreExceptionMessages
{
    /// <summary>
    /// Returns the message of <paramref name="exception"/> followed by the type and message of its root cause.
    /// </summary>
    /// <param name="exception">The exception to describe.</param>
    /// <returns>The diagnostic message.</returns>
    public static string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var root = exception.GetBaseException();
        return ReferenceEquals(root, exception)
            ? exception.Message
            : $"{exception.Message} ({root.GetType().Name}: {root.Message})";
    }
}
