using System.Reflection;
using Encina.Compliance.GDPR;
using FluentAssertions;

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
        usage.Should().NotBeNull();
        usage!.ValidOn.Should().Be(AttributeTargets.Class);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void Attribute_OnDecoratedType_ShouldBeDiscoverable()
    {
        // Act
        var attr = typeof(SampleMarkerOnlyRequest).GetCustomAttribute<ProcessesPersonalDataAttribute>();

        // Assert
        attr.Should().NotBeNull();
    }

    [Fact]
    public void Attribute_OnNonDecoratedType_ShouldBeNull()
    {
        // Act
        var attr = typeof(SampleNoAttributeRequest).GetCustomAttribute<ProcessesPersonalDataAttribute>();

        // Assert
        attr.Should().BeNull();
    }
}
