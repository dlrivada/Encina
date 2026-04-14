using Encina.GraphQL;
using Encina.GraphQL.Pagination;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.GraphQL;

/// <summary>
/// Guard tests for Encina.GraphQL covering constructor/method null guards and happy paths
/// for <see cref="GraphQLEncinaBridge"/>, <see cref="ConnectionExtensions"/>,
/// and <see cref="ServiceCollectionExtensions"/>.
/// </summary>
[Trait("Category", "Guard")]
public sealed class GraphQLGuardTests
{
    // ─── GraphQLEncinaBridge constructor guards ───

    [Fact]
    public void Bridge_NullEncina_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            new GraphQLEncinaBridge(null!,
                NullLogger<GraphQLEncinaBridge>.Instance,
                Options.Create(new EncinaGraphQLOptions())));
    }

    [Fact]
    public void Bridge_NullLogger_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new GraphQLEncinaBridge(encina, null!,
                Options.Create(new EncinaGraphQLOptions())));
    }

    [Fact]
    public void Bridge_NullOptions_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new GraphQLEncinaBridge(encina,
                NullLogger<GraphQLEncinaBridge>.Instance, null!));
    }

    [Fact]
    public void Bridge_ValidArgs_Constructs()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GraphQLEncinaBridge(encina,
            NullLogger<GraphQLEncinaBridge>.Instance,
            Options.Create(new EncinaGraphQLOptions()));
        sut.ShouldNotBeNull();
    }

    // ─── GraphQLEncinaBridge method guards ───

    [Fact]
    public async Task Bridge_QueryAsync_NullQuery_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GraphQLEncinaBridge(encina,
            NullLogger<GraphQLEncinaBridge>.Instance,
            Options.Create(new EncinaGraphQLOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.QueryAsync<TestQuery, string>(null!).AsTask());
    }

    [Fact]
    public async Task Bridge_MutateAsync_NullMutation_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GraphQLEncinaBridge(encina,
            NullLogger<GraphQLEncinaBridge>.Instance,
            Options.Create(new EncinaGraphQLOptions()));

        await Should.ThrowAsync<ArgumentNullException>(
            () => sut.MutateAsync<TestMutation, string>(null!).AsTask());
    }

    [Fact]
    public void Bridge_SubscribeAsync_NullSubscription_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sut = new GraphQLEncinaBridge(encina,
            NullLogger<GraphQLEncinaBridge>.Instance,
            Options.Create(new EncinaGraphQLOptions()));

        Should.Throw<ArgumentNullException>(() =>
            sut.SubscribeAsync<TestSubscription, string>(null!));
    }

    public sealed record TestQuery : IRequest<string>;
    public sealed record TestMutation : IRequest<string>;
    public sealed class TestSubscription;

    // ─── ConnectionExtensions.ToConnection(CursorPaginatedResult) guard ───

    [Fact]
    public void ToConnection_NullResult_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ConnectionExtensions.ToConnection<string>(null!));
    }

    // ─── ConnectionExtensions.Map guards ───

    [Fact]
    public void Map_NullConnection_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ConnectionExtensions.Map<string, int>(null!, s => s.Length));
    }

    [Fact]
    public void Map_NullSelector_Throws()
    {
        var connection = new Connection<string>
        {
            Edges = [],
            PageInfo = new RelayPageInfo(),
            TotalCount = 0
        };

        Should.Throw<ArgumentNullException>(() =>
            ConnectionExtensions.Map<string, int>(connection, null!));
    }

    [Fact]
    public void Map_ValidArgs_MapsNodes()
    {
        var connection = new Connection<string>
        {
            Edges =
            [
                new Edge<string> { Node = "hello", Cursor = "c1" },
                new Edge<string> { Node = "world", Cursor = "c2" }
            ],
            PageInfo = new RelayPageInfo
            {
                HasNextPage = false,
                HasPreviousPage = false,
                StartCursor = "c1",
                EndCursor = "c2"
            },
            TotalCount = 2
        };

        var mapped = connection.Map(s => s.Length);

        mapped.Edges.Count.ShouldBe(2);
        mapped.Edges[0].Node.ShouldBe(5);
        mapped.Edges[0].Cursor.ShouldBe("c1");
        mapped.Edges[1].Node.ShouldBe(5);
        mapped.Edges[1].Cursor.ShouldBe("c2");
        mapped.TotalCount.ShouldBe(2);
        mapped.PageInfo.StartCursor.ShouldBe("c1");
        mapped.PageInfo.EndCursor.ShouldBe("c2");
    }

    // ─── Connection<T> / Edge<T> / RelayPageInfo POCOs ───

    [Fact]
    public void Connection_Defaults()
    {
        var connection = new Connection<int>
        {
            Edges = [],
            PageInfo = new RelayPageInfo(),
            TotalCount = 0
        };

        connection.Nodes.ShouldBeEmpty();
        connection.Edges.ShouldBeEmpty();
        connection.TotalCount.ShouldBe(0);
    }

    [Fact]
    public void Edge_Defaults()
    {
        var edge = new Edge<string> { Node = "test", Cursor = "abc" };
        edge.Node.ShouldBe("test");
        edge.Cursor.ShouldBe("abc");
    }

    [Fact]
    public void RelayPageInfo_Defaults()
    {
        var info = new RelayPageInfo();
        info.HasNextPage.ShouldBeFalse();
        info.HasPreviousPage.ShouldBeFalse();
        info.StartCursor.ShouldBeNull();
        info.EndCursor.ShouldBeNull();
    }

    // ─── ServiceCollectionExtensions ───

    [Fact]
    public void AddEncinaGraphQL_NullServices_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddEncinaGraphQL());
    }

    [Fact]
    public void AddEncinaGraphQL_ValidServices_RegistersBridge()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IEncina>());

        var result = services.AddEncinaGraphQL();
        var bridgeRegistrations = services.Where(sd => sd.ServiceType == typeof(IGraphQLEncinaBridge)).ToList();

        result.ShouldNotBeNull();
        bridgeRegistrations.Count.ShouldBe(1);

        var bridgeRegistration = bridgeRegistrations[0];
        bridgeRegistration.Lifetime.ShouldBe(ServiceLifetime.Scoped);
        bridgeRegistration.ImplementationType.ShouldBe(typeof(GraphQLEncinaBridge));
        bridgeRegistration.ImplementationFactory.ShouldBeNull();
        bridgeRegistration.ImplementationInstance.ShouldBeNull();
    }

    // ─── EncinaGraphQLOptions ───

    [Fact]
    public void EncinaGraphQLOptions_Defaults()
    {
        var options = new EncinaGraphQLOptions();

        options.Path.ShouldBe("/graphql");
        options.EnableGraphQLIDE.ShouldBeTrue();
        options.EnableIntrospection.ShouldBeTrue();
        options.IncludeExceptionDetails.ShouldBeFalse();
        options.MaxExecutionDepth.ShouldBe(15);
        options.ExecutionTimeout.ShouldBe(TimeSpan.FromSeconds(30));
        options.EnableSubscriptions.ShouldBeTrue();
        options.EnablePersistedQueries.ShouldBeFalse();
    }
}
