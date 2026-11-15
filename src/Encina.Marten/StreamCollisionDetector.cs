namespace Encina.Marten;

/// <summary>
/// Recognises the failure of creating a stream whose identifier already exists.
/// </summary>
internal static class StreamCollisionDetector
{
    private const string StreamPrimaryKeyConstraint = "pkey_mt_streams_id";

    /// <summary>
    /// Determines whether <paramref name="ex"/> (or any exception in its inner chain) reports a stream collision.
    /// </summary>
    /// <remarks>
    /// Two shapes exist: Marten's own collision exceptions (matched by type name), and, under truly concurrent
    /// creators of one stream, a <c>PostgresException</c> unique violation on the <c>mt_streams</c> primary key
    /// (SQLSTATE 23505) wrapped by a Marten command exception. The latter is matched by type name and constraint
    /// text because this package does not reference Npgsql directly.
    /// </remarks>
    internal static bool IsStreamCollision(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().Name;

            if (typeName.Contains("StreamIdCollision", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("ExistingStream", StringComparison.OrdinalIgnoreCase)
                || typeName.Contains("DuplicateStream", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (typeName.Equals("PostgresException", StringComparison.Ordinal)
                && current.ToString().Contains(StreamPrimaryKeyConstraint, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
