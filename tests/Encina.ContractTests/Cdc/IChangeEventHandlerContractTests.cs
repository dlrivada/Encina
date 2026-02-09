using Encina.Cdc;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that the <see cref="IChangeEventHandler{TEntity}"/> interface
/// contract is correctly satisfied by implementations. Tests validate that each handler
/// method returns an <see cref="Either{EncinaError, Unit}"/> result following the
/// Railway Oriented Programming pattern.
/// Uses a tracking test implementation to verify invocation behavior.
/// </summary>
[Trait("Category", "Contract")]
public sealed class IChangeEventHandlerContractTests
{
    #region Test Helpers

    /// <summary>
    /// Simple entity type for testing handler dispatch.
    /// </summary>
    private sealed record TestEntity(int Id, string Name);

    /// <summary>
    /// Test-only CDC position for constructing <see cref="ChangeMetadata"/>.
    /// </summary>
    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Tracking implementation of <see cref="IChangeEventHandler{TEntity}"/> that records
    /// all invocations for assertion. Returns <c>Right(unit)</c> for success by default,
    /// or a configured error when <see cref="ShouldFail"/> is set.
    /// </summary>
    private sealed class TrackingChangeEventHandler : IChangeEventHandler<TestEntity>
    {
        public List<(string Method, TestEntity? Entity, TestEntity? Before, TestEntity? After)> Invocations { get; } = [];
        public bool ShouldFail { get; set; }

        public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TestEntity entity, ChangeContext context)
        {
            Invocations.Add(("Insert", entity, null, null));
            return ShouldFail
                ? new(Left<EncinaError, Unit>(EncinaError.New("Insert failed")))
                : new(Right<EncinaError, Unit>(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TestEntity before, TestEntity after, ChangeContext context)
        {
            Invocations.Add(("Update", null, before, after));
            return ShouldFail
                ? new(Left<EncinaError, Unit>(EncinaError.New("Update failed")))
                : new(Right<EncinaError, Unit>(unit));
        }

        public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TestEntity entity, ChangeContext context)
        {
            Invocations.Add(("Delete", entity, null, null));
            return ShouldFail
                ? new(Left<EncinaError, Unit>(EncinaError.New("Delete failed")))
                : new(Right<EncinaError, Unit>(unit));
        }
    }

    private static ChangeContext CreateContext() =>
        new("TestTable",
            new ChangeMetadata(new TestCdcPosition(1), DateTime.UtcNow, null, null, null),
            CancellationToken.None);

    #endregion

    #region HandleInsertAsync Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleInsertAsync"/> must
    /// return <c>Right(unit)</c> on successful processing.
    /// </summary>
    [Fact]
    public async Task Contract_HandleInsertAsync_ReturnsRight_OnSuccess()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler();
        var entity = new TestEntity(1, "Test");
        var context = CreateContext();

        // Act
        var result = await handler.HandleInsertAsync(entity, context);

        // Assert
        result.IsRight.ShouldBeTrue(
            "HandleInsertAsync must return Right(unit) on success");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleInsertAsync"/> must
    /// return <c>Left(error)</c> when processing fails.
    /// </summary>
    [Fact]
    public async Task Contract_HandleInsertAsync_ReturnsLeft_OnFailure()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler { ShouldFail = true };
        var entity = new TestEntity(1, "Test");
        var context = CreateContext();

        // Act
        var result = await handler.HandleInsertAsync(entity, context);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "HandleInsertAsync must return Left(error) on failure");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleInsertAsync"/> must
    /// receive the correct entity passed by the dispatcher.
    /// </summary>
    [Fact]
    public async Task Contract_HandleInsertAsync_ReceivesCorrectEntity()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler();
        var entity = new TestEntity(42, "InsertedEntity");
        var context = CreateContext();

        // Act
        await handler.HandleInsertAsync(entity, context);

        // Assert
        handler.Invocations.Count.ShouldBe(1, "Exactly one invocation must be recorded");
        handler.Invocations[0].Method.ShouldBe("Insert");
        handler.Invocations[0].Entity.ShouldBe(entity,
            "HandleInsertAsync must receive the exact entity passed to it");
    }

    #endregion

    #region HandleUpdateAsync Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleUpdateAsync"/> must
    /// return <c>Right(unit)</c> on successful processing.
    /// </summary>
    [Fact]
    public async Task Contract_HandleUpdateAsync_ReturnsRight_OnSuccess()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler();
        var before = new TestEntity(1, "Before");
        var after = new TestEntity(1, "After");
        var context = CreateContext();

        // Act
        var result = await handler.HandleUpdateAsync(before, after, context);

        // Assert
        result.IsRight.ShouldBeTrue(
            "HandleUpdateAsync must return Right(unit) on success");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleUpdateAsync"/> must
    /// return <c>Left(error)</c> when processing fails.
    /// </summary>
    [Fact]
    public async Task Contract_HandleUpdateAsync_ReturnsLeft_OnFailure()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler { ShouldFail = true };
        var before = new TestEntity(1, "Before");
        var after = new TestEntity(1, "After");
        var context = CreateContext();

        // Act
        var result = await handler.HandleUpdateAsync(before, after, context);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "HandleUpdateAsync must return Left(error) on failure");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleUpdateAsync"/> must
    /// receive both the before and after entity states from the dispatcher.
    /// </summary>
    [Fact]
    public async Task Contract_HandleUpdateAsync_ReceivesBeforeAndAfter()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler();
        var before = new TestEntity(1, "OldName");
        var after = new TestEntity(1, "NewName");
        var context = CreateContext();

        // Act
        await handler.HandleUpdateAsync(before, after, context);

        // Assert
        handler.Invocations.Count.ShouldBe(1, "Exactly one invocation must be recorded");
        handler.Invocations[0].Method.ShouldBe("Update");
        handler.Invocations[0].Before.ShouldBe(before,
            "HandleUpdateAsync must receive the correct before state");
        handler.Invocations[0].After.ShouldBe(after,
            "HandleUpdateAsync must receive the correct after state");
    }

    #endregion

    #region HandleDeleteAsync Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleDeleteAsync"/> must
    /// return <c>Right(unit)</c> on successful processing.
    /// </summary>
    [Fact]
    public async Task Contract_HandleDeleteAsync_ReturnsRight_OnSuccess()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler();
        var entity = new TestEntity(1, "Deleted");
        var context = CreateContext();

        // Act
        var result = await handler.HandleDeleteAsync(entity, context);

        // Assert
        result.IsRight.ShouldBeTrue(
            "HandleDeleteAsync must return Right(unit) on success");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleDeleteAsync"/> must
    /// return <c>Left(error)</c> when processing fails.
    /// </summary>
    [Fact]
    public async Task Contract_HandleDeleteAsync_ReturnsLeft_OnFailure()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler { ShouldFail = true };
        var entity = new TestEntity(1, "Deleted");
        var context = CreateContext();

        // Act
        var result = await handler.HandleDeleteAsync(entity, context);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "HandleDeleteAsync must return Left(error) on failure");
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}.HandleDeleteAsync"/> must
    /// receive the correct entity passed by the dispatcher.
    /// </summary>
    [Fact]
    public async Task Contract_HandleDeleteAsync_ReceivesCorrectEntity()
    {
        // Arrange
        var handler = new TrackingChangeEventHandler();
        var entity = new TestEntity(99, "DeletedEntity");
        var context = CreateContext();

        // Act
        await handler.HandleDeleteAsync(entity, context);

        // Assert
        handler.Invocations.Count.ShouldBe(1, "Exactly one invocation must be recorded");
        handler.Invocations[0].Method.ShouldBe("Delete");
        handler.Invocations[0].Entity.ShouldBe(entity,
            "HandleDeleteAsync must receive the exact entity passed to it");
    }

    #endregion

    #region Interface Shape Contract

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}"/> must declare exactly
    /// three methods: HandleInsertAsync, HandleUpdateAsync, HandleDeleteAsync.
    /// </summary>
    [Fact]
    public void Contract_IChangeEventHandler_HasExactlyThreeMethods()
    {
        // Arrange
        var iface = typeof(IChangeEventHandler<>);
        var methods = iface.GetMethods(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        // Assert
        methods.Length.ShouldBe(3,
            "IChangeEventHandler<TEntity> must declare exactly 3 methods");
    }

    /// <summary>
    /// Contract: All <see cref="IChangeEventHandler{TEntity}"/> methods must
    /// return <see cref="ValueTask{T}"/> of <see cref="Either{EncinaError, Unit}"/>.
    /// </summary>
    [Fact]
    public void Contract_IChangeEventHandler_AllMethodsReturnValueTaskEither()
    {
        // Arrange
        var iface = typeof(IChangeEventHandler<>);
        var methods = iface.GetMethods(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.DeclaredOnly);

        var expectedReturnType = typeof(ValueTask<Either<EncinaError, Unit>>);

        // Assert
        foreach (var method in methods)
        {
            method.ReturnType.ShouldBe(expectedReturnType,
                $"Method '{method.Name}' must return ValueTask<Either<EncinaError, Unit>>");
        }
    }

    /// <summary>
    /// Contract: <see cref="IChangeEventHandler{TEntity}"/> must be a generic
    /// interface with <c>TEntity</c> as a contravariant (<c>in</c>) type parameter.
    /// </summary>
    [Fact]
    public void Contract_IChangeEventHandler_IsGenericInterface()
    {
        var iface = typeof(IChangeEventHandler<>);

        iface.IsInterface.ShouldBeTrue("IChangeEventHandler must be an interface");
        iface.IsGenericTypeDefinition.ShouldBeTrue(
            "IChangeEventHandler must be a generic type definition");
        iface.GetGenericArguments().Length.ShouldBe(1,
            "IChangeEventHandler must have exactly one generic type parameter");
    }

    #endregion
}
