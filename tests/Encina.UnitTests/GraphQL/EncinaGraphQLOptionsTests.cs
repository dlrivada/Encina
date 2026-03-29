using Encina.GraphQL;

namespace Encina.UnitTests.GraphQL;

/// <summary>
/// Unit tests for <see cref="EncinaGraphQLOptions"/> default values.
/// </summary>
public sealed class EncinaGraphQLOptionsTests
{
    [Fact]
    public void Path_Default_ShouldBeGraphql()
    {
        new EncinaGraphQLOptions().Path.ShouldBe("/graphql");
    }

    [Fact]
    public void EnableGraphQLIDE_Default_ShouldBeTrue()
    {
        new EncinaGraphQLOptions().EnableGraphQLIDE.ShouldBeTrue();
    }

    [Fact]
    public void EnableIntrospection_Default_ShouldBeTrue()
    {
        new EncinaGraphQLOptions().EnableIntrospection.ShouldBeTrue();
    }

    [Fact]
    public void IncludeExceptionDetails_Default_ShouldBeFalse()
    {
        new EncinaGraphQLOptions().IncludeExceptionDetails.ShouldBeFalse();
    }

    [Fact]
    public void MaxExecutionDepth_Default_ShouldBe15()
    {
        new EncinaGraphQLOptions().MaxExecutionDepth.ShouldBe(15);
    }

    [Fact]
    public void ExecutionTimeout_Default_ShouldBe30Seconds()
    {
        new EncinaGraphQLOptions().ExecutionTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void EnableSubscriptions_Default_ShouldBeTrue()
    {
        new EncinaGraphQLOptions().EnableSubscriptions.ShouldBeTrue();
    }

    [Fact]
    public void EnablePersistedQueries_Default_ShouldBeFalse()
    {
        new EncinaGraphQLOptions().EnablePersistedQueries.ShouldBeFalse();
    }
}
