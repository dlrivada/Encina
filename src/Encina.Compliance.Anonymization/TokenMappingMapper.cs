using Encina.Compliance.Anonymization.Model;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Maps between <see cref="TokenMapping"/> domain records and
/// <see cref="TokenMappingEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// Since <see cref="TokenMapping"/> uses only primitive types (strings, byte arrays,
/// <see cref="DateTimeOffset"/>), no type conversions are required — properties map directly.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve token mappings without coupling to the domain model.
/// </para>
/// </remarks>
public static class TokenMappingMapper
{
    /// <summary>
    /// Converts a domain <see cref="TokenMapping"/> to a persistence entity.
    /// </summary>
    /// <param name="mapping">The domain mapping to convert.</param>
    /// <returns>A <see cref="TokenMappingEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="mapping"/> is <c>null</c>.</exception>
    public static TokenMappingEntity ToEntity(TokenMapping mapping)
    {
        ArgumentNullException.ThrowIfNull(mapping);

        return new TokenMappingEntity
        {
            Id = mapping.Id,
            Token = mapping.Token,
            OriginalValueHash = mapping.OriginalValueHash,
            EncryptedOriginalValue = mapping.EncryptedOriginalValue,
            KeyId = mapping.KeyId,
            CreatedAtUtc = mapping.CreatedAtUtc,
            ExpiresAtUtc = mapping.ExpiresAtUtc
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="TokenMapping"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="TokenMapping"/> domain record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static TokenMapping ToDomain(TokenMappingEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TokenMapping
        {
            Id = entity.Id,
            Token = entity.Token,
            OriginalValueHash = entity.OriginalValueHash,
            EncryptedOriginalValue = entity.EncryptedOriginalValue,
            KeyId = entity.KeyId,
            CreatedAtUtc = entity.CreatedAtUtc,
            ExpiresAtUtc = entity.ExpiresAtUtc
        };
    }
}
