using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Strategy for erasing a single personal data field at a specific data location.
/// </summary>
/// <remarks>
/// <para>
/// Erasure strategies are simple and composable. Each strategy knows how to erase data
/// from a specific type of backing store (e.g., database column, file, external service).
/// The <see cref="IDataErasureExecutor"/> selects the appropriate strategy for each
/// <see cref="PersonalDataLocation"/> during the erasure workflow.
/// </para>
/// <para>
/// Common implementations include:
/// </para>
/// <list type="bullet">
/// <item><b>Nullification</b>: Set the field value to <c>null</c> or a default</item>
/// <item><b>Anonymization</b>: Replace with anonymized/pseudonymized data</item>
/// <item><b>Hard delete</b>: Remove the entire record from storage</item>
/// <item><b>Crypto-shredding</b>: Destroy the encryption key, rendering data irrecoverable</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class NullificationStrategy : IDataErasureStrategy
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; EraseFieldAsync(
///         PersonalDataLocation location, CancellationToken cancellationToken)
///     {
///         // Set the field to null in the database
///         await _repository.SetFieldToNullAsync(
///             location.EntityType, location.EntityId, location.FieldName, cancellationToken);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface IDataErasureStrategy
{
    /// <summary>
    /// Erases a single personal data field at the specified location.
    /// </summary>
    /// <param name="location">The location of the personal data field to erase.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the field
    /// could not be erased.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> EraseFieldAsync(
        PersonalDataLocation location,
        CancellationToken cancellationToken = default);
}
