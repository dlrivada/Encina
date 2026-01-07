using Encina.Messaging.ScatterGather;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Messaging.Tests.ScatterGather;

/// <summary>
/// Unit tests for <see cref="ScatterGatherBuilder"/> and related types.
/// </summary>
public sealed class ScatterGatherBuilderTests
{
    #region ScatterGatherBuilder Static Factory Tests

    [Fact]
    public void Create_WithName_ReturnsBuilder()
    {
        // Act
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("MyScatter");

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            ScatterGatherBuilder.Create<TestRequest, int>(null!));
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            ScatterGatherBuilder.Create<TestRequest, int>(string.Empty));
    }

    [Fact]
    public void Create_WithoutName_GeneratesName()
    {
        // Act
        var builder = ScatterGatherBuilder.Create<TestRequest, int>();

        // Assert
        builder.ShouldNotBeNull();
    }

    #endregion

    #region ScatterGatherBuilder<TRequest, TResponse> Tests

    [Fact]
    public void ScatterTo_WithName_ReturnsScatterBuilder()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var scatterBuilder = builder.ScatterTo("Handler1");

        // Assert
        scatterBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void ScatterTo_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.ScatterTo((string)null!));
    }

    [Fact]
    public void ScatterTo_WithoutName_GeneratesName()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var scatterBuilder = builder.ScatterTo();

        // Assert
        scatterBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void ScatterTo_InlineAsyncHandler_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ScatterTo(async (req, ct) =>
        {
            await Task.Delay(1, ct);
            return Right<EncinaError, int>(42);
        });

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ScatterTo_InlineAsyncHandler_NullThrows()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ScatterTo((Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, int>>>)null!));
    }

    [Fact]
    public void ScatterTo_InlineSyncHandler_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ScatterTo(req => Right<EncinaError, int>(42));

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ScatterTo_InlineSyncHandler_NullThrows()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ScatterTo((Func<TestRequest, Either<EncinaError, int>>)null!));
    }

    [Fact]
    public void ScatterTo_NamedAsyncHandler_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ScatterTo("Handler1", async (req, ct) =>
        {
            await Task.Delay(1, ct);
            return Right<EncinaError, int>(42);
        });

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ScatterTo_NamedAsyncHandler_NullNameThrows()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            builder.ScatterTo(null!, async (req, ct) => Right<EncinaError, int>(42)));
    }

    [Fact]
    public void ScatterTo_NamedAsyncHandler_NullHandlerThrows()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.ScatterTo("Handler1", (Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, int>>>)null!));
    }

    [Fact]
    public void ScatterTo_NamedSyncHandler_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ScatterTo("Handler1", req => Right<EncinaError, int>(42));

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void GatherWith_ReturnsGatherBuilder()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var gatherBuilder = builder.GatherWith(GatherStrategy.WaitForAll);

        // Assert
        gatherBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GatherAll_ReturnsGatherBuilder()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var gatherBuilder = builder.GatherAll();

        // Assert
        gatherBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GatherFirst_ReturnsGatherBuilder()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var gatherBuilder = builder.GatherFirst();

        // Assert
        gatherBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GatherQuorum_ReturnsGatherBuilder()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var gatherBuilder = builder.GatherQuorum(2);

        // Assert
        gatherBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void GatherQuorum_WithZeroCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.GatherQuorum(0));
    }

    [Fact]
    public void GatherAllAllowingPartialFailures_ReturnsGatherBuilder()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var gatherBuilder = builder.GatherAllAllowingPartialFailures();

        // Assert
        gatherBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void WithTimeout_SetsTimeout()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.WithTimeout(TimeSpan.FromSeconds(30));

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithTimeout_ZeroOrNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.Zero));
        Should.Throw<ArgumentOutOfRangeException>(() => builder.WithTimeout(TimeSpan.FromSeconds(-1)));
    }

    [Fact]
    public void ExecuteSequentially_SetsSequentialExecution()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ExecuteSequentially();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ExecuteInParallel_SetsParallelExecution()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ExecuteInParallel();

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ExecuteInParallel_WithMaxDegree_SetsMaxDegreeOfParallelism()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.ExecuteInParallel(4);

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void ExecuteInParallel_WithInvalidMaxDegree_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentOutOfRangeException>(() => builder.ExecuteInParallel(0));
    }

    [Fact]
    public void WithMetadata_AddsMetadata()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        var result = builder.WithMetadata("key", "value");

        // Assert
        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.WithMetadata(null!, "value"));
    }

    [Fact]
    public void Build_WithNoScatterHandlers_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("scatter handler");
    }

    [Fact]
    public void Build_WithNoGatherHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(42));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("gather handler");
    }

    [Fact]
    public void Build_WithQuorumExceedingScatters_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(42))
            .GatherQuorum(5)
            .Aggregate(results => Right<EncinaError, int>(1));

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("Quorum count");
    }

    [Fact]
    public void Build_WithValidConfiguration_ReturnsDefinition()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo("Handler1", req => Right<EncinaError, int>(1))
            .ScatterTo("Handler2", req => Right<EncinaError, int>(2))
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(results.Count));

        // Act
        var definition = builder.Build();

        // Assert
        definition.ShouldNotBeNull();
        definition.Name.ShouldBe("Test");
        definition.ScatterHandlers.Count.ShouldBe(2);
    }

    #endregion

    #region ScatterBuilder Tests

    [Fact]
    public void ScatterBuilder_WithPriority_SetsPriority()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        builder
            .ScatterTo("Handler1").WithPriority(5).Execute(req => Right<EncinaError, int>(1))
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(1));

        var definition = builder.Build();

        // Assert
        definition.ScatterHandlers[0].Priority.ShouldBe(5);
    }

    [Fact]
    public void ScatterBuilder_WithMetadata_AddsMetadata()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        builder
            .ScatterTo("Handler1").WithMetadata("key", "value").Execute(req => Right<EncinaError, int>(1))
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(1));

        var definition = builder.Build();

        // Assert
        definition.ScatterHandlers[0].Metadata.ShouldNotBeNull();
        definition.ScatterHandlers[0].Metadata!["key"].ShouldBe("value");
    }

    [Fact]
    public void ScatterBuilder_Execute_AsyncEither_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        builder
            .ScatterTo("Handler1").Execute(async (req, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, int>(42);
            })
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(1));

        // Assert
        var definition = builder.Build();
        definition.ScatterHandlers.Count.ShouldBe(1);
    }

    [Fact]
    public void ScatterBuilder_Execute_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");
        var scatterBuilder = builder.ScatterTo("Handler1");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            scatterBuilder.Execute((Func<TestRequest, CancellationToken, ValueTask<Either<EncinaError, int>>>)null!));
    }

    [Fact]
    public void ScatterBuilder_Execute_SyncEither_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        builder
            .ScatterTo("Handler1").Execute(req => Right<EncinaError, int>(42))
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(1));

        // Assert
        var definition = builder.Build();
        definition.ScatterHandlers.Count.ShouldBe(1);
    }

    [Fact]
    public void ScatterBuilder_Execute_AsyncResult_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        builder
            .ScatterTo("Handler1").Execute(async (req, ct) =>
            {
                await Task.Delay(1, ct);
                return 42;
            })
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(1));

        // Assert
        var definition = builder.Build();
        definition.ScatterHandlers.Count.ShouldBe(1);
    }

    [Fact]
    public void ScatterBuilder_Execute_SyncResult_AddsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test");

        // Act
        builder
            .ScatterTo("Handler1").Execute(req => 42)
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(1));

        // Assert
        var definition = builder.Build();
        definition.ScatterHandlers.Count.ShouldBe(1);
    }

    #endregion

    #region GatherBuilder Tests

    [Fact]
    public void GatherBuilder_Aggregate_Async_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.Aggregate(async (results, ct) =>
        {
            await Task.Delay(1, ct);
            return Right<EncinaError, int>(results.Count);
        });

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_Aggregate_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.Aggregate((Func<IReadOnlyList<ScatterExecutionResult<int>>, CancellationToken, ValueTask<Either<EncinaError, int>>>)null!));
    }

    [Fact]
    public void GatherBuilder_Aggregate_Sync_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.Aggregate(results => Right<EncinaError, int>(results.Count));

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_AggregateSuccessful_Async_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.AggregateSuccessful(async (results, ct) =>
        {
            await Task.Delay(1, ct);
            return Right<EncinaError, int>(results.Count());
        });

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_AggregateSuccessful_Sync_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.AggregateSuccessful(results => Right<EncinaError, int>(results.Count()));

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_TakeFirst_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.TakeFirst();

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_TakeBest_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.TakeBest(x => x);

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_TakeBest_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.TakeBest<int>(null!));
    }

    [Fact]
    public void GatherBuilder_TakeMin_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.TakeMin(x => x);

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_TakeMax_SetsHandler()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act
        var result = builder.TakeMax(x => x);

        // Assert
        result.Build().ShouldNotBeNull();
    }

    [Fact]
    public void GatherBuilder_TakeMax_NullSelector_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.TakeMax<int>(null!));
    }

    #endregion

    #region GatherHandler Execution Tests

    private static ScatterExecutionResult<int> CreateResult(string name, Either<EncinaError, int> result, TimeSpan duration)
    {
        var now = DateTime.UtcNow;
        return new ScatterExecutionResult<int>(name, result, duration, now.Subtract(duration), now);
    }

    [Fact]
    public async Task TakeFirst_WithSuccessfulResults_ReturnsFirst()
    {
        // Arrange
        var definition = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherFirst()
            .TakeFirst()
            .Build();

        var results = new List<ScatterExecutionResult<int>>
        {
            CreateResult("Handler1", Right<EncinaError, int>(10), TimeSpan.FromMilliseconds(10)),
            CreateResult("Handler2", Right<EncinaError, int>(20), TimeSpan.FromMilliseconds(5))
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => v.ShouldBe(10),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task TakeFirst_WithNoSuccessfulResults_ReturnsError()
    {
        // Arrange
        var definition = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherFirst()
            .TakeFirst()
            .Build();

        var results = new List<ScatterExecutionResult<int>>
        {
            CreateResult("Handler1", Left<EncinaError, int>(EncinaError.New("Error1")), TimeSpan.FromMilliseconds(10)),
            CreateResult("Handler2", Left<EncinaError, int>(EncinaError.New("Error2")), TimeSpan.FromMilliseconds(5))
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task TakeBest_WithSuccessfulResults_ReturnsMin()
    {
        // Arrange
        var definition = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll()
            .TakeBest(x => x)
            .Build();

        var results = new List<ScatterExecutionResult<int>>
        {
            CreateResult("Handler1", Right<EncinaError, int>(30), TimeSpan.FromMilliseconds(10)),
            CreateResult("Handler2", Right<EncinaError, int>(10), TimeSpan.FromMilliseconds(5)),
            CreateResult("Handler3", Right<EncinaError, int>(20), TimeSpan.FromMilliseconds(8))
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => v.ShouldBe(10),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task TakeBest_WithNoSuccessfulResults_ForReferenceType_ReturnsError()
    {
        // Arrange - Use reference type (string) to properly test null check in TakeBest
        // (For value types like int, FirstOrDefault returns 0 instead of null)
        var definition = ScatterGatherBuilder.Create<TestRequest, string>("Test")
            .ScatterTo(req => Right<EncinaError, string>("test"))
            .GatherAll()
            .TakeBest(x => x.Length)
            .Build();

        var now = DateTime.UtcNow;
        var results = new List<ScatterExecutionResult<string>>
        {
            new("Handler1", Left<EncinaError, string>(EncinaError.New("Error")), TimeSpan.FromMilliseconds(10), now, now)
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task TakeMax_WithSuccessfulResults_ReturnsMax()
    {
        // Arrange
        var definition = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll()
            .TakeMax(x => x)
            .Build();

        var results = new List<ScatterExecutionResult<int>>
        {
            CreateResult("Handler1", Right<EncinaError, int>(30), TimeSpan.FromMilliseconds(10)),
            CreateResult("Handler2", Right<EncinaError, int>(10), TimeSpan.FromMilliseconds(5)),
            CreateResult("Handler3", Right<EncinaError, int>(50), TimeSpan.FromMilliseconds(8))
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => v.ShouldBe(50),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task TakeMax_WithNoSuccessfulResults_ForReferenceType_ReturnsError()
    {
        // Arrange - Use reference type (string) to properly test null check in TakeMax
        // (For value types like int, FirstOrDefault returns 0 instead of null)
        var definition = ScatterGatherBuilder.Create<TestRequest, string>("Test")
            .ScatterTo(req => Right<EncinaError, string>("test"))
            .GatherAll()
            .TakeMax(x => x.Length)
            .Build();

        var now = DateTime.UtcNow;
        var results = new List<ScatterExecutionResult<string>>
        {
            new("Handler1", Left<EncinaError, string>(EncinaError.New("Error")), TimeSpan.FromMilliseconds(10), now, now)
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task AggregateSuccessful_FiltersFailedResults()
    {
        // Arrange
        var definition = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAllAllowingPartialFailures()
            .AggregateSuccessful(results => Right<EncinaError, int>(results.Sum()))
            .Build();

        var results = new List<ScatterExecutionResult<int>>
        {
            CreateResult("Handler1", Right<EncinaError, int>(10), TimeSpan.FromMilliseconds(10)),
            CreateResult("Handler2", Left<EncinaError, int>(EncinaError.New("Error")), TimeSpan.FromMilliseconds(5)),
            CreateResult("Handler3", Right<EncinaError, int>(20), TimeSpan.FromMilliseconds(8))
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => v.ShouldBe(30),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    [Fact]
    public async Task Aggregate_Sync_ExecutesHandler()
    {
        // Arrange
        var definition = ScatterGatherBuilder.Create<TestRequest, int>("Test")
            .ScatterTo(req => Right<EncinaError, int>(1))
            .GatherAll()
            .Aggregate(results => Right<EncinaError, int>(results.Count))
            .Build();

        var results = new List<ScatterExecutionResult<int>>
        {
            CreateResult("Handler1", Right<EncinaError, int>(10), TimeSpan.FromMilliseconds(10)),
            CreateResult("Handler2", Right<EncinaError, int>(20), TimeSpan.FromMilliseconds(5))
        };

        // Act
        var result = await definition.GatherHandler(results, CancellationToken.None);

        // Assert
        result.Match(
            Right: v => v.ShouldBe(2),
            Left: _ => throw new InvalidOperationException("Expected Right"));
    }

    #endregion

    private sealed class TestRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}
