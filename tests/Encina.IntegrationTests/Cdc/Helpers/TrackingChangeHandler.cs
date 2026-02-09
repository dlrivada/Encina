using Encina.Cdc;
using Encina.Cdc.Abstractions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Cdc.Helpers;

/// <summary>
/// Change handler that tracks all invocations for integration test assertions.
/// Records each operation with before/after entity data and the change context.
/// </summary>
internal sealed class TrackingChangeHandler : IChangeEventHandler<TestEntity>
{
    private readonly List<HandlerInvocation> _invocations = [];
    private readonly object _lock = new();
    private Func<Either<EncinaError, Unit>>? _failureFactory;

    /// <summary>
    /// Gets a snapshot of all handler invocations.
    /// </summary>
    public IReadOnlyList<HandlerInvocation> Invocations
    {
        get { lock (_lock) { return [.. _invocations]; } }
    }

    /// <summary>
    /// Configures the handler to fail with the given error factory.
    /// </summary>
    public void SetFailure(Func<Either<EncinaError, Unit>> factory) => _failureFactory = factory;

    /// <summary>
    /// Clears the failure configuration.
    /// </summary>
    public void ClearFailure() => _failureFactory = null;

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> HandleInsertAsync(TestEntity entity, ChangeContext context)
    {
        if (_failureFactory is not null)
        {
            return new(_failureFactory());
        }

        lock (_lock)
        {
            _invocations.Add(new HandlerInvocation("Insert", null, entity, context));
        }
        return new(Right(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> HandleUpdateAsync(TestEntity before, TestEntity after, ChangeContext context)
    {
        if (_failureFactory is not null)
        {
            return new(_failureFactory());
        }

        lock (_lock)
        {
            _invocations.Add(new HandlerInvocation("Update", before, after, context));
        }
        return new(Right(unit));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Unit>> HandleDeleteAsync(TestEntity entity, ChangeContext context)
    {
        if (_failureFactory is not null)
        {
            return new(_failureFactory());
        }

        lock (_lock)
        {
            _invocations.Add(new HandlerInvocation("Delete", entity, null, context));
        }
        return new(Right(unit));
    }

    /// <summary>
    /// Represents a single handler invocation with all relevant data.
    /// </summary>
    internal sealed record HandlerInvocation(
        string Operation,
        TestEntity? Before,
        TestEntity? After,
        ChangeContext Context);
}
