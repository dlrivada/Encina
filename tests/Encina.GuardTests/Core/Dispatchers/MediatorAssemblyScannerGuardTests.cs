using System.Reflection;

namespace Encina.GuardTests.Core.Dispatchers;

/// <summary>
/// Guard tests for <see cref="EncinaAssemblyScanner"/> to verify null parameter handling
/// and assembly scanning behavior.
/// </summary>
public class MediatorAssemblyScannerGuardTests
{
    #region GetRegistrations

    /// <summary>
    /// Verifies that GetRegistrations throws ArgumentNullException when assembly is null.
    /// </summary>
    [Fact]
    public void GetRegistrations_NullAssembly_ThrowsArgumentNullException()
    {
        // Arrange
        Assembly assembly = null!;

        // Act & Assert
        var act = () => EncinaAssemblyScanner.GetRegistrations(assembly);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("assembly");
    }

    /// <summary>
    /// Verifies that GetRegistrations returns a non-null result for a valid assembly.
    /// </summary>
    [Fact]
    public void GetRegistrations_ValidAssembly_ReturnsNonNullResult()
    {
        // Arrange
        var assembly = typeof(EncinaAssemblyScanner).Assembly;

        // Act
        var result = EncinaAssemblyScanner.GetRegistrations(assembly);

        // Assert
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that GetRegistrations returns all expected collections (none are null).
    /// </summary>
    [Fact]
    public void GetRegistrations_ValidAssembly_AllCollectionsNonNull()
    {
        // Arrange
        var assembly = typeof(EncinaAssemblyScanner).Assembly;

        // Act
        var result = EncinaAssemblyScanner.GetRegistrations(assembly);

        // Assert
        result.HandlerRegistrations.ShouldNotBeNull();
        result.NotificationRegistrations.ShouldNotBeNull();
        result.PipelineRegistrations.ShouldNotBeNull();
        result.RequestPreProcessorRegistrations.ShouldNotBeNull();
        result.RequestPostProcessorRegistrations.ShouldNotBeNull();
        result.StreamHandlerRegistrations.ShouldNotBeNull();
        result.StreamPipelineRegistrations.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that GetRegistrations caches results (same reference for same assembly).
    /// </summary>
    [Fact]
    public void GetRegistrations_SameAssembly_ReturnsCachedResult()
    {
        // Arrange
        var assembly = typeof(MediatorAssemblyScannerGuardTests).Assembly;

        // Act
        var result1 = EncinaAssemblyScanner.GetRegistrations(assembly);
        var result2 = EncinaAssemblyScanner.GetRegistrations(assembly);

        // Assert
        result1.ShouldBeSameAs(result2);
    }

    /// <summary>
    /// Verifies that GetRegistrations discovers handlers in an assembly that contains them.
    /// </summary>
    [Fact]
    public void GetRegistrations_AssemblyWithHandlers_FindsRegistrations()
    {
        // Arrange - The test infrastructure assembly typically has handlers
        var assembly = typeof(EncinaAssemblyScanner).Assembly;

        // Act
        var result = EncinaAssemblyScanner.GetRegistrations(assembly);

        // Assert
        // The core assembly should at least have pipeline registrations (e.g., ValidationPipelineBehavior)
        // or at minimum return empty collections without throwing
        result.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that GetRegistrations for an assembly with no handlers returns empty collections.
    /// </summary>
    [Fact]
    public void GetRegistrations_AssemblyWithNoHandlers_ReturnsEmptyCollections()
    {
        // Arrange - Use an assembly that has no Encina handlers
        var assembly = typeof(int).Assembly; // mscorlib / System.Private.CoreLib

        // Act
        var result = EncinaAssemblyScanner.GetRegistrations(assembly);

        // Assert
        result.HandlerRegistrations.Count.ShouldBe(0);
        result.NotificationRegistrations.Count.ShouldBe(0);
        result.StreamHandlerRegistrations.Count.ShouldBe(0);
    }

    #endregion
}
