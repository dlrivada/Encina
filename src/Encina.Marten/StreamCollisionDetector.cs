namespace Encina.Marten;

/// <summary>
/// Recognises the failure of creating a stream whose identifier already exists.
/// </summary>
internal static class StreamCollisionDetector
{
    private const string StreamPrimaryKeyConstraint = "pkey_mt_streams_id";

    private static readonly string[] MartenCollisionTypeNames =
    [
        "StreamIdCollision",
        "ExistingStream",
        "DuplicateStream",
    ];

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
            if (IsCollisionShape(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCollisionShape(Exception exception)
    {
        var typeName = exception.GetType().Name;

        return IsMartenCollisionType(typeName) || IsStreamPrimaryKeyViolation(typeName, exception);
    }

    private static bool IsMartenCollisionType(string typeName) =>
        Array.Exists(
            MartenCollisionTypeNames,
            name => typeName.Contains(name, StringComparison.OrdinalIgnoreCase));

    private static bool IsStreamPrimaryKeyViolation(string typeName, Exception exception) =>
        typeName.Equals("PostgresException", StringComparison.Ordinal)
        && exception.ToString().Contains(StreamPrimaryKeyConstraint, StringComparison.OrdinalIgnoreCase);
}
