using Encina.Security.ABAC;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Encina.MongoDB.ABAC;

/// <summary>
/// Registers BSON class maps and conventions for ABAC model types with the MongoDB driver.
/// </summary>
/// <remarks>
/// <para>
/// This registration is required for native BSON storage of the ABAC domain model.
/// It handles:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <see cref="IExpression"/> polymorphic discriminators for <see cref="Apply"/>,
///     <see cref="AttributeDesignator"/>, <see cref="AttributeValue"/>, and
///     <see cref="VariableReference"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     Enum string representation convention for all ABAC types, ensuring enums like
///     <see cref="CombiningAlgorithmId"/>, <see cref="Effect"/>, <see cref="AttributeCategory"/>,
///     and <see cref="FulfillOn"/> are stored as human-readable strings.
///     </description>
///   </item>
///   <item>
///     <description>
///     Immutable record type support via <see cref="ImmutableTypeClassMapConvention"/> for
///     constructor-based deserialization of sealed record types.
///     </description>
///   </item>
/// </list>
/// <para>
/// This class follows the same thread-safe, idempotent registration pattern used by
/// <c>IdGenerationSerializerRegistration</c>.
/// </para>
/// </remarks>
internal static class ABACBsonClassMapRegistration
{
    private static bool _isRegistered;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures all ABAC BSON class maps and conventions are registered.
    /// This method is thread-safe and idempotent.
    /// </summary>
    public static void EnsureRegistered()
    {
        if (_isRegistered) return;

        lock (_lock)
        {
            if (_isRegistered) return;

            RegisterConventions();
            RegisterIExpressionDiscriminators();

            _isRegistered = true;
        }
    }

    /// <summary>
    /// Registers convention packs for ABAC types: enum as string representation
    /// and immutable record type support.
    /// </summary>
    private static void RegisterConventions()
    {
        // Enum string convention for all ABAC types
        var enumStringConventionPack = new ConventionPack
        {
            new EnumRepresentationConvention(BsonType.String)
        };

        ConventionRegistry.Register(
            "ABAC_EnumAsString",
            enumStringConventionPack,
            t => t.Namespace is not null &&
                 (t.Namespace.StartsWith("Encina.Security.ABAC", StringComparison.Ordinal) ||
                  t.Namespace.StartsWith("Encina.MongoDB.ABAC", StringComparison.Ordinal)));

        // Immutable type convention for sealed record deserialization
        var immutableConventionPack = new ConventionPack
        {
            new ImmutableTypeClassMapConvention()
        };

        ConventionRegistry.Register(
            "ABAC_ImmutableTypes",
            immutableConventionPack,
            t => t.Namespace is not null &&
                 t.Namespace.StartsWith("Encina.Security.ABAC", StringComparison.Ordinal));
    }

    /// <summary>
    /// Registers BSON discriminators for the <see cref="IExpression"/> polymorphic hierarchy.
    /// </summary>
    /// <remarks>
    /// MongoDB needs discriminator conventions to correctly serialize and deserialize
    /// <see cref="IExpression"/> implementations stored in fields like
    /// <see cref="AttributeAssignment.Value"/>, <see cref="VariableDefinition.Expression"/>,
    /// and <see cref="Apply.Arguments"/>.
    /// </remarks>
    private static void RegisterIExpressionDiscriminators()
    {
        // Register the interface as a known base type
        if (!BsonClassMap.IsClassMapRegistered(typeof(IExpression)))
        {
            BsonClassMap.RegisterClassMap<IExpression>(cm =>
            {
                cm.SetIsRootClass(true);
            });
        }

        // Register each concrete implementation with a scalar discriminator
        RegisterKnownType<Apply>("Apply");
        RegisterKnownType<AttributeDesignator>("AttributeDesignator");
        RegisterKnownType<AttributeValue>("AttributeValue");
        RegisterKnownType<VariableReference>("VariableReference");
    }

    /// <summary>
    /// Registers a concrete type as a known BSON discriminated subtype.
    /// </summary>
    /// <typeparam name="T">The concrete type to register.</typeparam>
    /// <param name="discriminator">The discriminator value for this type.</param>
    private static void RegisterKnownType<T>(string discriminator) where T : class
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
        {
            BsonClassMap.RegisterClassMap<T>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminator(discriminator);
            });
        }
    }
}
