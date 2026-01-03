using FsCheck.Xunit;

namespace Encina.Testing.FsCheck;

/// <summary>
/// Base class for property-based tests with Encina arbitraries pre-registered.
/// </summary>
/// <remarks>
/// <para>
/// Inherit from this class to use Encina arbitraries in your property-based tests.
/// FsCheck 3.x automatically discovers arbitraries via <see cref="EncinaArbitraryProvider"/>,
/// so no explicit registration is required.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public class MyPropertyTests : PropertyTestBase
/// {
///     [EncinaProperty]
///     public Property EncinaError_HasNonEmptyMessage(EncinaError error)
///     {
///         return EncinaProperties.ErrorHasNonEmptyMessage(error);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
public abstract class PropertyTestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyTestBase"/> class.
    /// </summary>
    protected PropertyTestBase()
    {
        // FsCheck 3.x auto-discovers arbitraries via EncinaArbitraryProvider
    }
}

/// <summary>
/// Provides configuration constants for property-based tests.
/// </summary>
public static class PropertyTestConfig
{
    /// <summary>
    /// Default number of tests to run for each property.
    /// </summary>
    public const int DefaultMaxTest = 100;

    /// <summary>
    /// Fewer tests for quick development feedback.
    /// </summary>
    public const int QuickMaxTest = 20;

    /// <summary>
    /// More tests for comprehensive testing.
    /// </summary>
    public const int ThoroughMaxTest = 1000;

    /// <summary>
    /// Default end size for generated values.
    /// </summary>
    public const int DefaultEndSize = 100;

    /// <summary>
    /// Larger end size for thorough testing.
    /// </summary>
    public const int ThoroughEndSize = 200;
}

/// <summary>
/// Custom property attribute with Encina-specific defaults.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute instead of <see cref="PropertyAttribute"/> to get
/// sensible defaults for Encina property-based tests.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EncinaPropertyAttribute : PropertyAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncinaPropertyAttribute"/> class.
    /// </summary>
    public EncinaPropertyAttribute()
    {
        MaxTest = PropertyTestConfig.DefaultMaxTest;
        Arbitrary = [typeof(EncinaArbitraryProvider)];
    }

    /// <summary>
    /// Initializes a new instance with a specific number of tests.
    /// </summary>
    /// <param name="maxTest">Maximum number of tests to run.</param>
    public EncinaPropertyAttribute(int maxTest)
    {
        MaxTest = maxTest;
        Arbitrary = [typeof(EncinaArbitraryProvider)];
    }
}

/// <summary>
/// Quick property attribute for fast development feedback.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class QuickPropertyAttribute : PropertyAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuickPropertyAttribute"/> class.
    /// </summary>
    public QuickPropertyAttribute()
    {
        MaxTest = PropertyTestConfig.QuickMaxTest;
        Arbitrary = [typeof(EncinaArbitraryProvider)];
    }
}

/// <summary>
/// Thorough property attribute for comprehensive testing.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ThoroughPropertyAttribute : PropertyAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThoroughPropertyAttribute"/> class.
    /// </summary>
    public ThoroughPropertyAttribute()
    {
        MaxTest = PropertyTestConfig.ThoroughMaxTest;
        EndSize = PropertyTestConfig.ThoroughEndSize;
        Arbitrary = [typeof(EncinaArbitraryProvider)];
    }
}
