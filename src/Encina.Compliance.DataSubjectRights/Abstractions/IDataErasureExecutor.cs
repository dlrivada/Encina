using LanguageExt;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Orchestrates the erasure of personal data for a data subject (Article 17).
/// </summary>
/// <remarks>
/// <para>
/// The erasure executor coordinates the full erasure workflow: locating personal data via
/// <see cref="IPersonalDataLocator"/>, evaluating retention requirements, applying erasure
/// strategies via <see cref="IDataErasureStrategy"/>, and producing a detailed
/// <see cref="ErasureResult"/>.
/// </para>
/// <para>
/// Fields with <see cref="PersonalDataAttribute.LegalRetention"/> set to <c>true</c> are
/// automatically excluded from erasure and documented in the result's retention reasons.
/// </para>
/// <para>
/// Per Article 17(3), the executor respects exemptions for freedom of expression, legal
/// obligations, public health, archiving, and legal claims.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var scope = new ErasureScope
/// {
///     Reason = ErasureReason.ConsentWithdrawn,
///     Categories = [PersonalDataCategory.Contact, PersonalDataCategory.Identity]
/// };
///
/// var result = await executor.EraseAsync("subject-123", scope, cancellationToken);
///
/// result.Match(
///     Right: r => Console.WriteLine($"Erased {r.FieldsErased}, retained {r.FieldsRetained}"),
///     Left: error => Console.WriteLine($"Erasure failed: {error.Message}"));
/// </code>
/// </example>
public interface IDataErasureExecutor
{
    /// <summary>
    /// Erases personal data for the specified data subject within the given scope.
    /// </summary>
    /// <param name="subjectId">The identifier of the data subject whose data should be erased.</param>
    /// <param name="scope">The erasure scope defining categories, specific fields, and exemptions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="ErasureResult"/> detailing the outcome of the erasure operation, including
    /// counts of fields erased, retained, and failed, along with retention reasons and applied
    /// exemptions, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, ErasureResult>> EraseAsync(
        string subjectId,
        ErasureScope scope,
        CancellationToken cancellationToken = default);
}
