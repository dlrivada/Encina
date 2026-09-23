using Encina.Compliance.Retention.Model;

using LanguageExt;

namespace Encina.Compliance.Retention.Abstractions;

/// <summary>
/// Erases the data governed by one expired retention record: the data of one data category held by
/// one entity.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RetentionEnforcementService"/> calls this port once per expired record that is not under a
/// legal hold, and marks the record <see cref="RetentionStatus.Deleted"/> only after it returns
/// <c>Right</c>. The application implements it because only the application knows where the data of a
/// retention category lives and how it must be removed (row deletion, field nullification,
/// anonymization, crypto-shredding, a call to another service).
/// </para>
/// <para>
/// Implementations must:
/// <list type="bullet">
/// <item><description>Erase only the data of <see cref="RetentionErasureTarget.DataCategory"/> for
/// <see cref="RetentionErasureTarget.EntityId"/>. Data of other categories of the same entity may still be
/// within its own retention period.</description></item>
/// <item><description>Return <c>Left</c> when any part of that data could not be erased. The record then
/// stays <see cref="RetentionStatus.Expired"/> and is retried on the next enforcement cycle.</description></item>
/// <item><description>Be idempotent: a retry may ask again for data that an earlier call already erased
/// (for example when marking the record deleted failed after a successful erasure).</description></item>
/// </list>
/// </para>
/// <para>
/// Retention records are keyed by entity and retention category, which is why this port exists instead of
/// reusing the data subject rights erasure executor of <c>Encina.Compliance.DataSubjectRights</c>: that
/// executor erases by data subject and by <c>PersonalDataCategory</c>, and neither maps one-to-one onto a
/// retention record. An implementation may still delegate to it when, in the application, the entity is
/// the data subject and the retention category corresponds to a set of personal data categories.
/// </para>
/// <para>
/// When no implementation is registered, the enforcement service erases nothing and never marks a
/// record deleted: expired records stay <see cref="RetentionStatus.Expired"/> and are counted as failed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class PatientDataEraser(AppDbContext db) : IRetentionDataEraser
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; EraseAsync(
///         RetentionErasureTarget target, CancellationToken cancellationToken = default)
///     {
///         switch (target.DataCategory)
///         {
///             case "patient-contact":
///                 await db.PatientContacts
///                     .Where(c =&gt; c.PatientId == target.EntityId)
///                     .ExecuteDeleteAsync(cancellationToken);
///                 return Unit.Default;
///
///             case "clinical-record":
///                 await db.ClinicalNotes
///                     .Where(n =&gt; n.PatientId == target.EntityId)
///                     .ExecuteDeleteAsync(cancellationToken);
///                 return Unit.Default;
///
///             default:
///                 return EncinaError.New($"No eraser for retention category '{target.DataCategory}'.");
///         }
///     }
/// }
///
/// services.AddScoped&lt;IRetentionDataEraser, PatientDataEraser&gt;();
/// </code>
/// </example>
public interface IRetentionDataEraser
{
    /// <summary>
    /// Erases the data of <see cref="RetentionErasureTarget.DataCategory"/> held by
    /// <see cref="RetentionErasureTarget.EntityId"/>.
    /// </summary>
    /// <param name="target">The expired record's entity, data category and scoping identifiers.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Right</c> when all the data of the category was erased (or was already gone); <c>Left</c> when
    /// any of it could not be erased, in which case the record is retried on the next cycle.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> EraseAsync(
        RetentionErasureTarget target,
        CancellationToken cancellationToken = default);
}
