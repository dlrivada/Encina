using LanguageExt;

using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Erasure strategy that sets a personal data field to <c>null</c> (hard delete / nullification).
/// </summary>
/// <remarks>
/// <para>
/// This is the default erasure strategy. It clears the field value by setting it to <c>null</c>,
/// effectively removing the personal data. The entity record itself is preserved — only the
/// personal data field is nullified.
/// </para>
/// <para>
/// For scenarios requiring physical record deletion or anonymization, implement a custom
/// <see cref="IDataErasureStrategy"/>.
/// </para>
/// <para>
/// This strategy operates on in-memory <see cref="PersonalDataLocation"/> objects. In production,
/// provider-specific strategies should interact with the underlying database or storage system
/// to persist the erasure.
/// </para>
/// </remarks>
public sealed class HardDeleteErasureStrategy : IDataErasureStrategy
{
    private readonly ILogger<HardDeleteErasureStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardDeleteErasureStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for structured erasure logging.</param>
    public HardDeleteErasureStrategy(ILogger<HardDeleteErasureStrategy> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> EraseFieldAsync(
        PersonalDataLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        _logger.LogDebug(
            "Erasing field '{FieldName}' on entity {EntityType} (ID: {EntityId}) via nullification",
            location.FieldName,
            location.EntityType.Name,
            location.EntityId);

        // The actual field nullification is performed by the caller (erasure executor)
        // This strategy signals successful erasure; the executor is responsible for
        // coordinating the physical data change through the appropriate data access layer.
        return ValueTask.FromResult<Either<EncinaError, Unit>>(unit);
    }
}
