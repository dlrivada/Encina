namespace Encina.Security;

/// <summary>
/// Requires the current user to be the owner of the resource being accessed.
/// </summary>
/// <remarks>
/// <para>
/// Ownership is verified by comparing <see cref="ISecurityContext.UserId"/> with the value
/// of the property specified by <see cref="OwnerProperty"/> on the request object.
/// </para>
/// <para>
/// Evaluation is delegated to <see cref="IResourceOwnershipEvaluator"/> for extensibility.
/// The default evaluator uses cached reflection to read the property value.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Verify that the current user owns the order via OwnerId property
/// [RequireOwnership("OwnerId")]
/// public sealed record UpdateOrderCommand(Guid OrderId, string OwnerId, OrderData Data) : ICommand;
///
/// // Combine with permission fallback (admin can bypass ownership)
/// [RequireOwnership("CreatedBy")]
/// [RequireRole("Admin")]
/// public sealed record DeleteDocumentCommand(Guid DocumentId, string CreatedBy) : ICommand;
/// </code>
/// </example>
public sealed class RequireOwnershipAttribute : SecurityAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequireOwnershipAttribute"/> class.
    /// </summary>
    /// <param name="ownerProperty">
    /// The name of the property on the request that contains the owner identifier.
    /// </param>
    public RequireOwnershipAttribute(string ownerProperty)
    {
        OwnerProperty = ownerProperty;
    }

    /// <summary>
    /// Gets the name of the request property that contains the owner identifier.
    /// </summary>
    /// <remarks>
    /// The property value is compared against <see cref="ISecurityContext.UserId"/>
    /// using the registered <see cref="IResourceOwnershipEvaluator"/>.
    /// </remarks>
    public string OwnerProperty { get; }
}
