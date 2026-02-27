namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Request to rectify inaccurate personal data under GDPR Article 16.
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to obtain from the controller without undue delay
/// the rectification of inaccurate personal data concerning them. Taking into account
/// the purposes of the processing, the data subject has the right to have incomplete
/// personal data completed.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject requesting rectification.</param>
/// <param name="FieldName">The name of the field to be rectified.</param>
/// <param name="NewValue">The corrected value to replace the current data.</param>
/// <param name="EntityType">
/// The CLR type of the entity containing the field to rectify.
/// <c>null</c> to rectify across all entities containing the named field.
/// </param>
/// <param name="EntityId">
/// The identifier of the specific entity instance to rectify.
/// <c>null</c> to rectify across all instances.
/// </param>
public sealed record RectificationRequest(
    string SubjectId,
    string FieldName,
    object NewValue,
    Type? EntityType,
    string? EntityId);
