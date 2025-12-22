using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Ardalis.GuardClauses;

namespace Encina.GuardClauses;

/// <summary>
/// Provides guard clause methods for domain validation and defensive programming.
/// </summary>
/// <remarks>
/// <para>
/// Guards follow Encina's ROP conventions by returning validation results via the Try-pattern:
/// methods return <c>bool</c> for success/failure and provide error details via <c>out</c> parameters.
/// </para>
/// <para>
/// <b>When to Use Guards</b>:
/// <list type="bullet">
/// <item><description><b>Domain Invariants</b>: Protect class invariants in constructors and methods</description></item>
/// <item><description><b>State Validation</b>: Validate object state before operations (e.g., entity not null after query)</description></item>
/// <item><description><b>Defensive Programming</b>: Fail-fast on invalid preconditions in business logic</description></item>
/// </list>
/// </para>
/// <para>
/// <b>When NOT to Use Guards</b>:
/// <list type="bullet">
/// <item><description><b>Input Validation</b>: Use FluentValidation, DataAnnotations, or MiniValidator instead</description></item>
/// <item><description><b>Pipeline Behaviors</b>: Input validation happens BEFORE handler execution</description></item>
/// <item><description><b>Redundant Validation</b>: Don't re-validate already-validated input in handlers</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // ✅ DO - Guard domain invariants in constructors
/// public class User
/// {
///     public User(string email, string password)
///     {
///         if (!Guards.TryValidateNotEmpty(email, nameof(email), out var emailError))
///             throw new InvalidOperationException(emailError.Message);
///
///         if (!Guards.TryValidateNotEmpty(password, nameof(password), out var pwdError))
///             throw new InvalidOperationException(pwdError.Message);
///
///         Email = email;
///         Password = password;
///     }
/// }
///
/// // ✅ DO - Guard state validation in handlers
/// public async Task&lt;Either&lt;MediatorError, OrderId&gt;&gt; Handle(CancelOrder cmd, CancellationToken ct)
/// {
///     var order = await _orders.FindById(cmd.OrderId, ct);
///
///     if (!Guards.TryValidateNotNull(order, nameof(order), out var error))
///         return Left&lt;MediatorError, OrderId&gt;(error);
///
///     order.Cancel();
///     await _orders.Save(order, ct);
///     return Right&lt;MediatorError, OrderId&gt;(order.Id);
/// }
///
/// // ❌ DON'T - Redundant input validation (use FluentValidation/DataAnnotations/MiniValidator instead)
/// public Task&lt;Either&lt;MediatorError, UserId&gt;&gt; Handle(CreateUser cmd, CancellationToken ct)
/// {
///     // WRONG: Input already validated by validation behaviors
///     if (!Guards.TryValidateNotEmpty(cmd.Email, nameof(cmd.Email), out var error))
///         return Task.FromResult(Left&lt;MediatorError, UserId&gt;(error));
///
///     // Handler logic...
/// }
/// </code>
/// </example>
public static class Guards
{
    /// <summary>
    /// Error code for guard clause validation failures.
    /// </summary>
    public const string GuardValidationFailed = "mediator.guard.validation_failed";

    /// <summary>
    /// Validates that a value is not null.
    /// </summary>
    /// <typeparam name="T">The type of the value to validate.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the value is not null; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// var user = await _users.FindById(userId, ct);
    /// if (!Guards.TryValidateNotNull(user, nameof(user), out var error))
    ///     return Left&lt;MediatorError, Unit&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateNotNull<T>(T? value, string paramName, out MediatorError error, string? message = null)
    {
        if (value is not null)
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} cannot be null.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(T).FullName,
            ["stage"] = "guard_validation",
            ["guard"] = "NotNull"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the string is not null or empty; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidateNotEmpty(email, nameof(email), out var error))
    ///     return Left&lt;MediatorError, UserId&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateNotEmpty(string? value, string paramName, out MediatorError error, string? message = null)
    {
        if (!string.IsNullOrEmpty(value))
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} cannot be null or empty.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(string).FullName,
            ["stage"] = "guard_validation",
            ["guard"] = "NotEmpty"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a string is not null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the string is not null, empty, or whitespace; otherwise, <c>false</c>.</returns>
    public static bool TryValidateNotWhiteSpace(string? value, string paramName, out MediatorError error, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} cannot be null, empty, or whitespace.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(string).FullName,
            ["stage"] = "guard_validation",
            ["guard"] = "NotWhiteSpace"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a collection is not null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="value">The collection to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the collection is not null or empty; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidateNotEmpty(items, nameof(items), out var error))
    ///     return Left&lt;MediatorError, Unit&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateNotEmpty<T>(IEnumerable<T>? value, string paramName, out MediatorError error, string? message = null)
    {
        if (value is not null && value.Any())
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} cannot be null or empty.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(IEnumerable<T>).FullName,
            ["stage"] = "guard_validation",
            ["guard"] = "NotEmpty"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a numeric value is positive (greater than zero).
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the value is positive; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidatePositive(quantity, nameof(quantity), out var error))
    ///     return Left&lt;MediatorError, Unit&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidatePositive<T>(T value, string paramName, out MediatorError error, string? message = null)
        where T : IComparable<T>
    {
        var zero = (T)Convert.ChangeType(0, typeof(T), CultureInfo.InvariantCulture);
        if (value.CompareTo(zero) > 0)
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} must be positive (greater than zero).";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(T).FullName,
            ["value"] = value,
            ["stage"] = "guard_validation",
            ["guard"] = "Positive"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a numeric value is negative (less than zero).
    /// </summary>
    /// <typeparam name="T">The numeric type.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the value is negative; otherwise, <c>false</c>.</returns>
    public static bool TryValidateNegative<T>(T value, string paramName, out MediatorError error, string? message = null)
        where T : IComparable<T>
    {
        var zero = (T)Convert.ChangeType(0, typeof(T), CultureInfo.InvariantCulture);
        if (value.CompareTo(zero) < 0)
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} must be negative (less than zero).";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(T).FullName,
            ["value"] = value,
            ["stage"] = "guard_validation",
            ["guard"] = "Negative"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a value is within an inclusive range.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="min">The minimum value (inclusive).</param>
    /// <param name="max">The maximum value (inclusive).</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the value is within range; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidateInRange(age, nameof(age), 18, 120, out var error))
    ///     return Left&lt;MediatorError, UserId&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateInRange<T>(T value, string paramName, T min, T max, out MediatorError error, string? message = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0)
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} must be between {min} and {max} (inclusive).";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(T).FullName,
            ["value"] = value,
            ["min"] = min,
            ["max"] = max,
            ["stage"] = "guard_validation",
            ["guard"] = "InRange"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a string has a valid email format.
    /// </summary>
    /// <param name="value">The email string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the email format is valid; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidateEmail(email, nameof(email), out var error))
    ///     return Left&lt;MediatorError, UserId&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateEmail(string? value, string paramName, out MediatorError error, string? message = null)
    {
        // Basic email validation using regex
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        if (!string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, emailPattern))
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} must be a valid email address.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["value"] = value,
            ["stage"] = "guard_validation",
            ["guard"] = "ValidEmail"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a string has a valid URL format.
    /// </summary>
    /// <param name="value">The URL string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the URL format is valid; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidateUrl(websiteUrl, nameof(websiteUrl), out var error))
    ///     return Left&lt;MediatorError, CompanyId&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateUrl(string? value, string paramName, out MediatorError error, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} must be a valid HTTP or HTTPS URL.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["value"] = value,
            ["stage"] = "guard_validation",
            ["guard"] = "ValidUrl"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a GUID is not empty.
    /// </summary>
    /// <param name="value">The GUID to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the GUID is not empty; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidateNotEmpty(userId, nameof(userId), out var error))
    ///     return Left&lt;MediatorError, User&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidateNotEmpty(Guid value, string paramName, out MediatorError error, string? message = null)
    {
        if (value != Guid.Empty)
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} cannot be an empty GUID.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["type"] = typeof(Guid).FullName,
            ["stage"] = "guard_validation",
            ["guard"] = "NotEmptyGuid"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a condition is true.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the condition is true; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidate(order.Status == OrderStatus.Pending, nameof(order), out var error,
    ///     message: "Order must be in Pending status to be cancelled"))
    ///     return Left&lt;MediatorError, Unit&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidate(bool condition, string paramName, out MediatorError error, string? message = null)
    {
        if (condition)
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"Validation failed for {paramName}.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["stage"] = "guard_validation",
            ["guard"] = "Custom"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }

    /// <summary>
    /// Validates that a string matches a regular expression pattern.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <param name="pattern">The regex pattern to match.</param>
    /// <param name="error">The error if validation fails.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns><c>true</c> if the string matches the pattern; otherwise, <c>false</c>.</returns>
    /// <example>
    /// <code>
    /// if (!Guards.TryValidatePattern(phoneNumber, nameof(phoneNumber), @"^\d{3}-\d{3}-\d{4}$", out var error,
    ///     message: "Phone number must be in format: 555-123-4567"))
    ///     return Left&lt;MediatorError, ContactId&gt;(error);
    /// </code>
    /// </example>
    public static bool TryValidatePattern(string? value, string paramName, string pattern, out MediatorError error, string? message = null)
    {
        if (!string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, pattern))
        {
            error = default;
            return true;
        }

        var errorMessage = message ?? $"{paramName} does not match the required pattern.";
        var metadata = new Dictionary<string, object?>
        {
            ["parameter"] = paramName,
            ["value"] = value,
            ["pattern"] = pattern,
            ["stage"] = "guard_validation",
            ["guard"] = "Pattern"
        };
        error = MediatorErrors.Create(GuardValidationFailed, errorMessage, details: metadata);
        return false;
    }
}
