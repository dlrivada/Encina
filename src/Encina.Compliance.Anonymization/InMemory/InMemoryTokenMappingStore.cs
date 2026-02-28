using System.Collections.Concurrent;

using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.InMemory;

/// <summary>
/// In-memory implementation of <see cref="ITokenMappingStore"/> for testing and development.
/// </summary>
/// <remarks>
/// <para>
/// Token mappings are stored in <see cref="ConcurrentDictionary{TKey,TValue}"/> with
/// two indexes: by token and by original value hash for efficient lookup and deduplication.
/// Mappings are lost when the process terminates.
/// </para>
/// <para>
/// For production use, implement <see cref="ITokenMappingStore"/> backed by a persistent
/// database. All 13 database providers are supported: ADO.NET (4), Dapper (4), EF Core (4),
/// and MongoDB (1).
/// </para>
/// </remarks>
public sealed class InMemoryTokenMappingStore : ITokenMappingStore
{
    private readonly ConcurrentDictionary<string, TokenMapping> _byToken = new();
    private readonly ConcurrentDictionary<string, TokenMapping> _byHash = new();

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Unit>> StoreAsync(
        TokenMapping mapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        _byToken[mapping.Token] = mapping;
        _byHash[mapping.OriginalValueHash] = mapping;

        return ValueTask.FromResult<Either<EncinaError, Unit>>(
            Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        var result = _byToken.TryGetValue(token, out var mapping)
            ? Some(mapping)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<TokenMapping>>>(
            Right<EncinaError, Option<TokenMapping>>(result));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByOriginalValueHashAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hash);

        var result = _byHash.TryGetValue(hash, out var mapping)
            ? Some(mapping)
            : None;

        return ValueTask.FromResult<Either<EncinaError, Option<TokenMapping>>>(
            Right<EncinaError, Option<TokenMapping>>(result));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, Unit>> DeleteByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(keyId);

        var mappingsToRemove = _byToken.Values
            .Where(m => m.KeyId == keyId)
            .ToList();

        foreach (var mapping in mappingsToRemove)
        {
            _byToken.TryRemove(mapping.Token, out _);
            _byHash.TryRemove(mapping.OriginalValueHash, out _);
        }

        return ValueTask.FromResult<Either<EncinaError, Unit>>(
            Right<EncinaError, Unit>(unit));
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, IReadOnlyList<TokenMapping>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var all = _byToken.Values
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToList()
            .AsReadOnly();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<TokenMapping>>>(
            Right<EncinaError, IReadOnlyList<TokenMapping>>(all));
    }

    /// <summary>
    /// Gets the total number of token mappings in the store (for testing).
    /// </summary>
    public int Count => _byToken.Count;

    /// <summary>
    /// Clears all token mappings from the store (for testing).
    /// </summary>
    public void Clear()
    {
        _byToken.Clear();
        _byHash.Clear();
    }
}
