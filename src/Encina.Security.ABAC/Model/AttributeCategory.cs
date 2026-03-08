namespace Encina.Security.ABAC;

/// <summary>
/// Identifies the four standard attribute categories defined by XACML 3.0 Appendix B.
/// </summary>
/// <remarks>
/// <para>
/// Attribute categories classify the source of attribute values used during policy evaluation.
/// Each category corresponds to a distinct aspect of the access request:
/// who is requesting (<see cref="Subject"/>), what is being accessed (<see cref="Resource"/>),
/// how it is being accessed (<see cref="Action"/>), and under what conditions (<see cref="Environment"/>).
/// </para>
/// <para>
/// The category is part of the <see cref="AttributeDesignator"/> and determines which
/// <c>IAttributeProvider</c> is responsible for resolving the attribute value.
/// </para>
/// </remarks>
public enum AttributeCategory
{
    /// <summary>
    /// Attributes describing the entity requesting access (the user or service).
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.2 — Corresponds to <c>urn:oasis:names:tc:xacml:1.0:subject-category:access-subject</c>.
    /// Common attributes: user ID, roles, department, clearance level, group membership.
    /// </remarks>
    Subject,

    /// <summary>
    /// Attributes describing the resource being accessed.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.3 — Corresponds to <c>urn:oasis:names:tc:xacml:3.0:attribute-category:resource</c>.
    /// Common attributes: resource type, classification level, owner, sensitivity label.
    /// </remarks>
    Resource,

    /// <summary>
    /// Attributes describing the current environmental conditions.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.5 — Corresponds to <c>urn:oasis:names:tc:xacml:3.0:attribute-category:environment</c>.
    /// Common attributes: current time, day of week, IP address, business hours, tenant ID.
    /// </remarks>
    Environment,

    /// <summary>
    /// Attributes describing the action being performed on the resource.
    /// </summary>
    /// <remarks>
    /// XACML 3.0 §B.4 — Corresponds to <c>urn:oasis:names:tc:xacml:3.0:attribute-category:action</c>.
    /// Common attributes: action name (read, write, delete), HTTP method, operation type.
    /// </remarks>
    Action
}
