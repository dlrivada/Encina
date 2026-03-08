namespace Encina.Security.ABAC;

/// <summary>
/// Marker interface for XACML 3.0 expression tree nodes used in conditions and attribute assignments.
/// </summary>
/// <remarks>
/// <para>
/// XACML 3.0 §7.7 — The expression tree forms a recursive structure where each node
/// can be an <see cref="Apply"/> (function application), <see cref="AttributeDesignator"/>
/// (attribute lookup), <see cref="AttributeValue"/> (literal value), or
/// <see cref="VariableReference"/> (reference to a named variable).
/// </para>
/// <para>
/// This interface enables type-safe recursive composition of condition expressions.
/// An <see cref="Apply"/> node contains <see cref="IExpression"/> arguments, which can
/// themselves be nested <see cref="Apply"/> nodes, creating arbitrarily deep expression trees.
/// </para>
/// <para><b>Implementing types:</b></para>
/// <list type="bullet">
/// <item><description><see cref="Apply"/> — Function application with arguments</description></item>
/// <item><description><see cref="AttributeDesignator"/> — Attribute lookup by category, ID, and data type</description></item>
/// <item><description><see cref="AttributeValue"/> — Literal value with data type</description></item>
/// <item><description><see cref="VariableReference"/> — Reference to a <see cref="VariableDefinition"/></description></item>
/// </list>
/// </remarks>
public interface IExpression;
