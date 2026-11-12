using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using LanguageExt;

namespace RopUnions.Spike;

/// <summary>
/// Layout mirror of src/Encina/Errors/EncinaError.cs (readonly record struct: string + two Option&lt;Exception&gt;).
/// </summary>
public readonly record struct EncinaError
{
    private EncinaError(string message, Option<Exception> exception, Option<Exception> metadata)
    {
        Message = message;
        Exception = exception;
        MetadataException = metadata;
    }

    public string Message { get; }
    public Option<Exception> Exception { get; }
    internal Option<Exception> MetadataException { get; }

    public static EncinaError New(string message) => new(message, Option<Exception>.None, Option<Exception>.None);
}

// ---------------------------------------------------------------------------
// (b) Standard C# 15 union: compiler-generated struct, case values stored in object? Value (boxing).
// ---------------------------------------------------------------------------
public union StdResult<T>(EncinaError, T);

// ---------------------------------------------------------------------------
// (c) Custom non-boxing union: [Union] struct with typed fields, HasValue and TryGetValue.
// ---------------------------------------------------------------------------
[Union]
public readonly struct Result<T> : IUnion
{
    private readonly T _value;
    private readonly EncinaError _error;
    private readonly byte _state; // 0 = default (no value), 1 = success, 2 = error

    public Result(T value) { _value = value; _error = default; _state = 1; }
    public Result(EncinaError error) { _value = default!; _error = error; _state = 2; }

    // Boxing view, used only by the generic IUnion consumers (serializers, reflection).
    public object? Value => _state switch { 1 => _value, 2 => _error, _ => null };

    public bool HasValue => _state != 0;
    public bool IsSuccess => _state == 1;

    public bool TryGetValue([MaybeNullWhen(false)] out T value) { value = _value; return _state == 1; }
    public bool TryGetValue(out EncinaError error) { error = _error; return _state == 2; }

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(EncinaError error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Match<TOut>(Func<T, TOut> success, Func<EncinaError, TOut> failure)
        => _state == 1 ? success(_value) : failure(_error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<TOut> Map<TOut>(Func<T, TOut> f)
        => _state == 1 ? new Result<TOut>(f(_value)) : new Result<TOut>(_error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> f)
        => _state == 1 ? f(_value) : new Result<TOut>(_error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<T> MapError(Func<EncinaError, EncinaError> f)
        => _state == 2 ? new Result<T>(f(_error)) : this;
}

// ---------------------------------------------------------------------------
// (d) Closed hierarchy: abstract record with two sealed cases (one allocation per result).
// ---------------------------------------------------------------------------
public abstract record HResult<T>
{
    private HResult() { }

    public sealed record Ok(T Value) : HResult<T>;
    public sealed record Fail(EncinaError Error) : HResult<T>;

    public TOut Match<TOut>(Func<T, TOut> success, Func<EncinaError, TOut> failure) => this switch
    {
        Ok ok => success(ok.Value),
        Fail f => failure(f.Error),
        _ => throw new UnreachableException(),
    };

    public HResult<TOut> Map<TOut>(Func<T, TOut> f) => this switch
    {
        Ok ok => new HResult<TOut>.Ok(f(ok.Value)),
        Fail e => new HResult<TOut>.Fail(e.Error),
        _ => throw new UnreachableException(),
    };

    public HResult<TOut> Bind<TOut>(Func<T, HResult<TOut>> f) => this switch
    {
        Ok ok => f(ok.Value),
        Fail e => new HResult<TOut>.Fail(e.Error),
        _ => throw new UnreachableException(),
    };
}

/// <summary>Combinators for the standard union: they must live outside because the union body is compiler-generated.</summary>
public static class StdResultExtensions
{
    extension<T>(StdResult<T> r)
    {
        public TOut Match<TOut>(Func<T, TOut> success, Func<EncinaError, TOut> failure) => r switch
        {
            T v => success(v),
            EncinaError e => failure(e),
        };

        public StdResult<TOut> Map<TOut>(Func<T, TOut> f) => r switch
        {
            T v => new StdResult<TOut>(f(v)),
            EncinaError e => new StdResult<TOut>(e),
        };

        public StdResult<TOut> Bind<TOut>(Func<T, StdResult<TOut>> f) => r switch
        {
            T v => f(v),
            EncinaError e => new StdResult<TOut>(e),
        };
    }
}
