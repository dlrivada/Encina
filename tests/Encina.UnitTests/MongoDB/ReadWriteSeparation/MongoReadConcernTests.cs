using Encina.MongoDB.ReadWriteSeparation;
using Shouldly;

namespace Encina.UnitTests.MongoDB.ReadWriteSeparation;

public sealed class MongoReadConcernTests
{
    [Fact]
    public void Default_HasCorrectValue()
    {
        MongoReadConcern.Default.ShouldBe((MongoReadConcern)0);
    }

    [Fact]
    public void Local_HasCorrectValue()
    {
        MongoReadConcern.Local.ShouldBe((MongoReadConcern)1);
    }

    [Fact]
    public void Majority_HasCorrectValue()
    {
        MongoReadConcern.Majority.ShouldBe((MongoReadConcern)2);
    }

    [Fact]
    public void Linearizable_HasCorrectValue()
    {
        MongoReadConcern.Linearizable.ShouldBe((MongoReadConcern)3);
    }

    [Fact]
    public void Available_HasCorrectValue()
    {
        MongoReadConcern.Available.ShouldBe((MongoReadConcern)4);
    }

    [Fact]
    public void Snapshot_HasCorrectValue()
    {
        MongoReadConcern.Snapshot.ShouldBe((MongoReadConcern)5);
    }

    [Fact]
    public void AllValues_AreDistinct()
    {
        // Arrange & Act
        var values = Enum.GetValues<MongoReadConcern>();

        // Assert
        values.ShouldBeUnique();
        values.Length.ShouldBe(6);
    }
}
