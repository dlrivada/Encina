using System.Collections.Concurrent;
using System.Reflection;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.GDPR;

/// <summary>
/// In-memory implementation of <see cref="ILawfulBasisRegistry"/> for development, testing, and simple deployments.
/// </summary>
/// <remarks>
/// <para>
/// Registrations are stored in a <see cref="ConcurrentDictionary{TKey,TValue}"/> keyed by
/// the assembly-qualified name of the request type, ensuring thread-safe concurrent access.
/// </para>
/// <para>
/// This implementation also supports auto-registration from <see cref="LawfulBasisAttribute"/>
/// decorations via <see cref="AutoRegisterFromAssemblies"/>. Call this method at startup
/// to scan assemblies and populate the registry automatically.
/// </para>
/// <para>
/// For production systems requiring durable storage, consider a database-backed implementation
/// of <see cref="ILawfulBasisRegistry"/>.
/// </para>
/// </remarks>
public sealed class InMemoryLawfulBasisRegistry : ILawfulBasisRegistry
{
    private readonly ConcurrentDictionary<string, LawfulBasisRegistration> _registrations = new();

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> RegisterAsync(
        LawfulBasisRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);

        var key = registration.RequestType.AssemblyQualifiedName!;

        if (!_registrations.TryAdd(key, registration))
        {
            return ValueTask.FromResult<Either<EncinaError, Unit>>(
                EncinaError.New($"A lawful basis registration already exists for request type '{registration.RequestType.FullName}'."));
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeAsync(
        Type requestType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestType);

        Option<LawfulBasisRegistration> result = _registrations.TryGetValue(requestType.AssemblyQualifiedName!, out var registration)
            ? Some(registration)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<LawfulBasisRegistration>>>(result);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<LawfulBasisRegistration>>> GetByRequestTypeNameAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestTypeName);

        Option<LawfulBasisRegistration> result = _registrations.TryGetValue(requestTypeName, out var registration)
            ? Some(registration)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<LawfulBasisRegistration>>>(result);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<LawfulBasisRegistration>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LawfulBasisRegistration> result = _registrations.Values.ToList().AsReadOnly();
        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<LawfulBasisRegistration>>>(Right(result));
    }

    /// <summary>
    /// Scans the specified assemblies for types decorated with <see cref="LawfulBasisAttribute"/>
    /// and registers them automatically.
    /// </summary>
    /// <param name="assemblies">Assemblies to scan for lawful basis attributes.</param>
    /// <param name="timeProvider">Time provider for timestamps. Uses <see cref="TimeProvider.System"/> if <c>null</c>.</param>
    /// <returns>The number of lawful basis registrations auto-registered.</returns>
    /// <remarks>
    /// <para>
    /// Call this method at application startup when auto-registration from attributes is enabled.
    /// Types already registered are silently skipped.
    /// </para>
    /// </remarks>
    public int AutoRegisterFromAssemblies(IEnumerable<Assembly> assemblies, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var count = 0;

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                var registration = LawfulBasisRegistration.FromAttribute(type, timeProvider);
                if (registration is null)
                {
                    continue;
                }

                var key = type.AssemblyQualifiedName!;
                if (_registrations.TryAdd(key, registration))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
