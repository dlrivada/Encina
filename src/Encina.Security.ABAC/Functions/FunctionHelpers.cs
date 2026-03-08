using System.Globalization;

namespace Encina.Security.ABAC;

/// <summary>
/// Shared helper methods for XACML function argument validation and type coercion.
/// </summary>
internal static class FunctionHelpers
{
    /// <summary>
    /// Validates that the argument list has exactly <paramref name="expected"/> elements.
    /// </summary>
    internal static void ValidateArgCount(
        IReadOnlyList<object?> args,
        int expected,
        string functionName)
    {
        if (args.Count != expected)
        {
            throw new InvalidOperationException(
                $"'{functionName}' requires exactly {expected} argument(s), but received {args.Count}.");
        }
    }

    /// <summary>
    /// Validates that the argument list has at least <paramref name="minimum"/> elements.
    /// </summary>
    internal static void ValidateMinArgCount(
        IReadOnlyList<object?> args,
        int minimum,
        string functionName)
    {
        if (args.Count < minimum)
        {
            throw new InvalidOperationException(
                $"'{functionName}' requires at least {minimum} argument(s), but received {args.Count}.");
        }
    }

    /// <summary>
    /// Coerces an argument to string, returning an empty string for null.
    /// </summary>
    internal static string CoerceToString(object? arg)
    {
        return arg?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Coerces an argument to a non-null string, throwing if null.
    /// </summary>
    internal static string CoerceToStringStrict(object? arg, string functionName, int argIndex)
    {
        if (arg is null)
        {
            throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null.");
        }

        return arg is string s ? s : arg.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Coerces an argument to <see cref="bool"/>.
    /// </summary>
    internal static bool CoerceToBool(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var result) => result,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null."),
            _ => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex}: cannot convert '{arg}' ({arg.GetType().Name}) to Boolean.")
        };
    }

    /// <summary>
    /// Coerces an argument to <see cref="int"/>.
    /// </summary>
    internal static int CoerceToInt(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            int i => i,
            long l => checked((int)l),
            double d => checked((int)d),
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) => result,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null."),
            _ => TryConvert<int>(arg, functionName, argIndex)
        };
    }

    /// <summary>
    /// Coerces an argument to <see cref="double"/>.
    /// </summary>
    internal static double CoerceToDouble(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            double d => d,
            int i => i,
            long l => l,
            float f => f,
            string s when double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture, out var result) => result,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null."),
            _ => TryConvert<double>(arg, functionName, argIndex)
        };
    }

    /// <summary>
    /// Coerces an argument to <see cref="DateTime"/>.
    /// </summary>
    internal static DateTime CoerceToDateTime(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var result) => result,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null."),
            _ => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex}: cannot convert '{arg}' ({arg.GetType().Name}) to DateTime.")
        };
    }

    /// <summary>
    /// Coerces an argument to <see cref="TimeSpan"/> (for XACML time type).
    /// </summary>
    internal static TimeSpan CoerceToTime(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            TimeSpan ts => ts,
            DateTime dt => dt.TimeOfDay,
            string s when TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var result) => result,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null."),
            _ => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex}: cannot convert '{arg}' ({arg.GetType().Name}) to Time.")
        };
    }

    /// <summary>
    /// Coerces an argument to <see cref="DateOnly"/> (for XACML date type).
    /// </summary>
    internal static DateOnly CoerceToDate(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            DateTimeOffset dto => DateOnly.FromDateTime(dto.UtcDateTime),
            string s when DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                => result,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null."),
            _ => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex}: cannot convert '{arg}' ({arg.GetType().Name}) to Date.")
        };
    }

    /// <summary>
    /// Extracts an <see cref="AttributeBag"/> from a function argument.
    /// </summary>
    internal static AttributeBag CoerceToBag(object? arg, string functionName, int argIndex)
    {
        return arg switch
        {
            AttributeBag bag => bag,
            null => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex} must not be null (expected AttributeBag)."),
            _ => throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex}: expected AttributeBag but received {arg.GetType().Name}.")
        };
    }

    /// <summary>
    /// Extracts the raw values from an <see cref="AttributeBag"/> as a list of objects.
    /// </summary>
    internal static IReadOnlyList<object?> GetBagValues(AttributeBag bag)
    {
        return bag.Values.Select(v => v.Value).ToArray();
    }

    /// <summary>
    /// Compares two values of the same type using <see cref="IComparable{T}"/>.
    /// Returns a negative, zero, or positive value.
    /// </summary>
    internal static int CompareValues<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b);
    }

    /// <summary>
    /// Fallback type conversion using <see cref="Convert"/>.ChangeType.
    /// </summary>
    private static T TryConvert<T>(object arg, string functionName, int argIndex)
    {
        try
        {
            return (T)Convert.ChangeType(arg, typeof(T), CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            throw new InvalidOperationException(
                $"'{functionName}' argument {argIndex}: cannot convert '{arg}' ({arg.GetType().Name}) to {typeof(T).Name}.",
                ex);
        }
    }
}
