using System.Reflection;
using Encina.Compliance.GDPR;
using Shouldly;

namespace Encina.UnitTests.Compliance.GDPR.Attributes;

/// <summary>
/// Unit tests for <see cref="ProcessesPersonalDataAttribute"/>.
/// </summary>
public class ProcessesPersonalDataAttributeTests
{
    [Fact]
    public void Attribute_ShouldTargetClassOnly()
    {
        // Arrange
        var usage = typeof(ProcessesPersonalDataAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        usage.ShouldNotBeNull();
        usage!.ValidOn.ShouldBe(AttributeTargets.Class);
        usage.AllowMultiple.ShouldBeFalse();
        usage.Inherited.ShouldBeTrue();
    }

    [Fact]
    public void Attribute_OnDecoratedType_ShouldBeDiscoverable()
    {
        // Act
        var attr = typeof(SampleMarkerOnlyRequest).GetCustomAttribute<ProcessesPersonalDataAttribute>();

        // Assert
        attr.ShouldNotBeNull();
    }

    [Fact]
    public void Attribute_OnNonDecoratedType_ShouldBeNull()
    {
        // Act
        var attr = typeof(SampleNoAttributeRequest).GetCustomAttribute<ProcessesPersonalDataAttribute>();

        // Assert
        attr.ShouldBeNull();
    }
}
