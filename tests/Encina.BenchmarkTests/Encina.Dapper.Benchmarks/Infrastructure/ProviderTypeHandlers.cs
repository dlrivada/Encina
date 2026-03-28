using Dapper;

namespace Encina.Dapper.Benchmarks.Infrastructure;

/// <summary>
/// Registers Dapper type handlers for all supported providers.
/// </summary>
public static class ProviderTypeHandlers
{
    private static bool _isRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures all required type handlers are registered for the specified provider.
    /// This method is thread-safe and idempotent.
    /// </summary>
    /// <param name="provider">The database provider.</param>
    public static void EnsureRegistered(DatabaseProvider provider)
    {
        if (_isRegistered)
        {
            return;
        }

        lock (_lock)
        {
            if (_isRegistered)
            {
                return;
            }

            // Provider-specific type handlers can be registered here as needed.
            _isRegistered = true;
        }
    }

    /// <summary>
    /// Resets the registration state. Used for testing.
    /// </summary>
    internal static void ResetRegistration()
    {
        lock (_lock)
        {
            _isRegistered = false;
            SqlMapper.ResetTypeHandlers();
        }
    }
}
