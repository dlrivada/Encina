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

// ===========================================================================
// #3672 options: where the error lives inside the custom union.
// ===========================================================================

// ---------------------------------------------------------------------------
// Option B1: T inline, error by reference through a private box allocated only on failure.
// EncinaError stays the 40-byte struct, so call sites keep matching on EncinaError.
// ---------------------------------------------------------------------------
/// <summary>Non-generic box so a failure keeps the same instance when it crosses Bind/Map into another T.</summary>
public sealed class ErrorBox(EncinaError error) { public readonly EncinaError Error = error; }

[Union]
public readonly struct BoxResult<T> : IUnion
{
    private readonly T _value;
    private readonly ErrorBox? _box; // non-null = failure
    private readonly byte _state;    // 0 = default, 1 = success, 2 = error

    public BoxResult(T value) { _value = value; _box = null; _state = 1; }
    public BoxResult(EncinaError error) { _value = default!; _box = new ErrorBox(error); _state = 2; }

    public object? Value => _state switch { 1 => _value, 2 => _box!.Error, _ => null };
    public bool HasValue => _state != 0;
    public bool IsSuccess => _state == 1;

    public bool TryGetValue([MaybeNullWhen(false)] out T value) { value = _value; return _state == 1; }
    public bool TryGetValue(out EncinaError error) { error = _box is null ? default : _box.Error; return _state == 2; }

    public static implicit operator BoxResult<T>(T value) => new(value);
    public static implicit operator BoxResult<T>(EncinaError error) => new(error);

    // Failure propagation reuses the existing box: no new allocation when the error crosses a Bind or Map.
    // A factory, not a constructor: a public single-argument constructor would make ErrorBox a union case type.
    private BoxResult(ErrorBox box, bool _) { _value = default!; _box = box; _state = 2; }
    private static BoxResult<TOut> Propagate<TOut>(ErrorBox box) => new(box, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Match<TOut>(Func<T, TOut> success, Func<EncinaError, TOut> failure)
        => _state == 1 ? success(_value) : failure(_box!.Error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoxResult<TOut> Map<TOut>(Func<T, TOut> f)
        => _state == 1 ? new BoxResult<TOut>(f(_value)) : Propagate<TOut>(_box!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoxResult<TOut> Bind<TOut>(Func<T, BoxResult<TOut>> f)
        => _state == 1 ? f(_value) : Propagate<TOut>(_box!);
}

// ---------------------------------------------------------------------------
// Option B2: EncinaError becomes a sealed class; the union holds T inline and the class reference.
// ---------------------------------------------------------------------------
public sealed class EncinaErrorRef
{
    private EncinaErrorRef(string message, Option<Exception> exception, Option<Exception> metadata)
    {
        Message = message;
        Exception = exception;
        MetadataException = metadata;
    }

    public string Message { get; }
    public Option<Exception> Exception { get; }
    internal Option<Exception> MetadataException { get; }

    public static EncinaErrorRef New(string message) => new(message, Option<Exception>.None, Option<Exception>.None);
}

[Union]
public readonly struct RefResult<T> : IUnion
{
    private readonly T _value;
    private readonly EncinaErrorRef? _error; // non-null = failure
    private readonly byte _state;            // 0 = default, 1 = success, 2 = error

    public RefResult(T value) { _value = value; _error = null; _state = 1; }
    public RefResult(EncinaErrorRef error) { _value = default!; _error = error; _state = 2; }

    public object? Value => _state switch { 1 => _value, 2 => _error, _ => null };
    public bool HasValue => _state != 0;
    public bool IsSuccess => _state == 1;

    public bool TryGetValue([MaybeNullWhen(false)] out T value) { value = _value; return _state == 1; }
    public bool TryGetValue([MaybeNullWhen(false)] out EncinaErrorRef error) { error = _error; return _state == 2; }

    public static implicit operator RefResult<T>(T value) => new(value);
    public static implicit operator RefResult<T>(EncinaErrorRef error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Match<TOut>(Func<T, TOut> success, Func<EncinaErrorRef, TOut> failure)
        => _state == 1 ? success(_value) : failure(_error!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefResult<TOut> Map<TOut>(Func<T, TOut> f)
        => _state == 1 ? new RefResult<TOut>(f(_value)) : new RefResult<TOut>(_error!);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RefResult<TOut> Bind<TOut>(Func<T, RefResult<TOut>> f)
        => _state == 1 ? f(_value) : new RefResult<TOut>(_error!);
}

// ---------------------------------------------------------------------------
// Option C: shrunk inline error: a string plus one nullable reference (exception or metadata object).
// ---------------------------------------------------------------------------
public readonly record struct SlimError
{
    private SlimError(string message, object? detail) { Message = message; Detail = detail; }

    public string Message { get; }
    public object? Detail { get; }

    public static SlimError New(string message) => new(message, null);
}

[Union]
public readonly struct SlimResult<T> : IUnion
{
    private readonly T _value;
    private readonly SlimError _error;
    private readonly byte _state; // 0 = default, 1 = success, 2 = error

    public SlimResult(T value) { _value = value; _error = default; _state = 1; }
    public SlimResult(SlimError error) { _value = default!; _error = error; _state = 2; }

    public object? Value => _state switch { 1 => _value, 2 => _error, _ => null };
    public bool HasValue => _state != 0;
    public bool IsSuccess => _state == 1;

    public bool TryGetValue([MaybeNullWhen(false)] out T value) { value = _value; return _state == 1; }
    public bool TryGetValue(out SlimError error) { error = _error; return _state == 2; }

    public static implicit operator SlimResult<T>(T value) => new(value);
    public static implicit operator SlimResult<T>(SlimError error) => new(error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TOut Match<TOut>(Func<T, TOut> success, Func<SlimError, TOut> failure)
        => _state == 1 ? success(_value) : failure(_error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SlimResult<TOut> Map<TOut>(Func<T, TOut> f)
        => _state == 1 ? new SlimResult<TOut>(f(_value)) : new SlimResult<TOut>(_error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SlimResult<TOut> Bind<TOut>(Func<T, SlimResult<TOut>> f)
        => _state == 1 ? f(_value) : new SlimResult<TOut>(_error);
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
            null => throw new UnreachableException(),
        };

        public StdResult<TOut> Map<TOut>(Func<T, TOut> f) => r switch
        {
            T v => new StdResult<TOut>(f(v)),
            EncinaError e => new StdResult<TOut>(e),
            null => throw new UnreachableException(),
        };

        public StdResult<TOut> Bind<TOut>(Func<T, StdResult<TOut>> f) => r switch
        {
            T v => f(v),
            EncinaError e => new StdResult<TOut>(e),
            null => throw new UnreachableException(),
        };
    }
}
