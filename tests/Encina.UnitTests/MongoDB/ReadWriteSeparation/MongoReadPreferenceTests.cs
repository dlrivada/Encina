using Encina.MongoDB.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public sealed class MongoReadPreferenceTests
{
    [Fact]
    public void Primary_HasCorrectValue()
    {
        MongoReadPreference.Primary.ShouldBe((MongoReadPreference)0);
    }

    [Fact]
    public void PrimaryPreferred_HasCorrectValue()
    {
        MongoReadPreference.PrimaryPreferred.ShouldBe((MongoReadPreference)1);
    }

    [Fact]
    public void Secondary_HasCorrectValue()
    {
        MongoReadPreference.Secondary.ShouldBe((MongoReadPreference)2);
    }

    [Fact]
    public void SecondaryPreferred_HasCorrectValue()
    {
        MongoReadPreference.SecondaryPreferred.ShouldBe((MongoReadPreference)3);
    }

    [Fact]
    public void Nearest_HasCorrectValue()
    {
        MongoReadPreference.Nearest.ShouldBe((MongoReadPreference)4);
    }

    [Fact]
    public void AllValues_AreDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<MongoReadPreference>();

        // Assert
        values.ShouldBeUnique();
        values.Length.ShouldBe(5);
    }
}
