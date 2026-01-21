using Encina.Messaging.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.Messaging.ReadWriteSeparation;

/// <summary>
/// Unit tests for <see cref="DatabaseIntent"/> enum.
/// </summary>
public sealed class DatabaseIntentTests
{
    [Fact]
    public void Write_HasCorrectValue()
    {
        ((int)DatabaseIntent.Write).ShouldBe(0);
    }

    [Fact]
    public void Read_HasCorrectValue()
    {
        ((int)DatabaseIntent.Read).ShouldBe(1);
    }

    [Fact]
    public void ForceWrite_HasCorrectValue()
    {
        ((int)DatabaseIntent.ForceWrite).ShouldBe(2);
    }

    [Theory]
    [InlineData(DatabaseIntent.Write, "Write")]
    [InlineData(DatabaseIntent.Read, "Read")]
    [InlineData(DatabaseIntent.ForceWrite, "ForceWrite")]
    public void ToString_ReturnsExpectedName(DatabaseIntent intent, string expectedName)
    {
        intent.ToString().ShouldBe(expectedName);
    }

    [Fact]
    public void AllValues_AreDefined()
    {
        var values = Enum.GetValues<DatabaseIntent>();
        values.ShouldContain(DatabaseIntent.Write);
        values.ShouldContain(DatabaseIntent.Read);
        values.ShouldContain(DatabaseIntent.ForceWrite);
        values.Length.ShouldBe(3);
    }

    [Fact]
    public void DefaultValue_IsWrite()
    {
        // Arrange & Act
        var defaultIntent = default(DatabaseIntent);

        // Assert
        defaultIntent.ShouldBe(DatabaseIntent.Write);
    }
}
