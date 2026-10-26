using System.Data;

namespace Encina.Messaging.Transactions;

/// <summary>
/// Error codes and factories of the shared transaction (<see cref="ITransactionLeaseSource"/>).
/// </summary>
/// <remarks>
/// The errors carry the code, and for exceptions the exception type; callers log the code, never
/// <see cref="EncinaError"/> messages.
/// </remarks>
public static class TransactionErrors
{
    /// <summary>The transaction could not be begun (the connection could not open or the begin failed).</summary>
    public const string BeginFailedCode = "transaction.begin_failed";

    /// <summary>The commit failed; the transaction was rolled back.</summary>
    public const string CommitFailedCode = "transaction.commit_failed";

    /// <summary>A participant rolled back, so the owner's commit rolled the transaction back instead.</summary>
    public const string RollbackOnlyCode = "transaction.rollback_only";

    /// <summary>A joiner asked for an isolation level different from the owner's.</summary>
    public const string IsolationLevelMismatchCode = "transaction.isolation_level_mismatch";

    /// <summary>The lease was used after it was disposed or after its transaction ended.</summary>
    public const string LeaseDisposedCode = "transaction.lease_disposed";

    /// <summary>The scope's connection cannot take part in an asynchronous transaction.</summary>
    public const string NotSupportedCode = "transaction.not_supported";

    /// <summary>Creates the error for a transaction that could not be begun.</summary>
    /// <param name="exception">The exception raised by the connection.</param>
    /// <returns>The error, with code <see cref="BeginFailedCode"/>.</returns>
    public static EncinaError BeginFailed(Exception exception)
        => EncinaErrors.FromException(BeginFailedCode, exception, "The transaction could not be begun.");

    /// <summary>Creates the error for a commit that failed.</summary>
    /// <param name="exception">The exception raised by the commit.</param>
    /// <returns>The error, with code <see cref="CommitFailedCode"/>.</returns>
    public static EncinaError CommitFailed(Exception exception)
        => EncinaErrors.FromException(CommitFailedCode, exception, "The transaction could not be committed.");

    /// <summary>The detail key under which <see cref="CommitFailed(Exception, string?)"/> carries the provider's error label.</summary>
    public const string ErrorLabelDetailKey = "errorLabel";

    /// <summary>
    /// Creates the error for a commit that failed, carrying the provider's error label (for MongoDB
    /// <c>TransientTransactionError</c> or <c>UnknownTransactionCommitResult</c>) so the caller can decide whether
    /// retrying the request is safe. The label is metadata only; the exception message is never copied.
    /// </summary>
    /// <param name="exception">The exception raised by the commit.</param>
    /// <param name="errorLabel">The provider's error label, or <see langword="null"/> when it has none.</param>
    /// <returns>The error, with code <see cref="CommitFailedCode"/> and the label under <see cref="ErrorLabelDetailKey"/>.</returns>
    public static EncinaError CommitFailed(Exception exception, string? errorLabel)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var details = errorLabel is null
            ? null
            : new Dictionary<string, object?> { [ErrorLabelDetailKey] = errorLabel };

        return EncinaErrors.FromException(CommitFailedCode, exception, "The transaction could not be committed.", details);
    }

    /// <summary>Creates the error for a commit refused because a participant rolled back.</summary>
    /// <returns>The error, with code <see cref="RollbackOnlyCode"/>.</returns>
    public static EncinaError RollbackOnly()
        => EncinaErrors.Create(RollbackOnlyCode, "A participant rolled the shared transaction back, so it was not committed.");

    /// <summary>Creates the error for a joiner asking for another isolation level than the owner's.</summary>
    /// <param name="requested">The isolation level the joiner asked for.</param>
    /// <param name="active">The isolation level of the active transaction.</param>
    /// <returns>The error, with code <see cref="IsolationLevelMismatchCode"/>.</returns>
    public static EncinaError IsolationLevelMismatch(IsolationLevel requested, IsolationLevel active)
        => EncinaErrors.Create(
            IsolationLevelMismatchCode,
            $"The active transaction runs at {active}; a participant cannot join it at {requested}.");

    /// <summary>Creates the error for a lease used after it was disposed or its transaction ended.</summary>
    /// <returns>The error, with code <see cref="LeaseDisposedCode"/>.</returns>
    public static EncinaError LeaseDisposed()
        => EncinaErrors.Create(LeaseDisposedCode, "The transaction lease was already disposed or its transaction has ended.");

    /// <summary>
    /// Gets the exception behind a transaction error, or an exception named after its code when the error was
    /// not raised by an exception, for callers that map the error to their own error type.
    /// </summary>
    /// <param name="error">The transaction error.</param>
    /// <returns>The causing exception, or an <see cref="InvalidOperationException"/> whose message is the error code.</returns>
    internal static Exception AsException(EncinaError error)
        => error.GetCause().IfNone(() => new InvalidOperationException(error.GetCode().IfNone("transaction.failed")));

    /// <summary>Creates the error for a connection that cannot take part in an asynchronous transaction.</summary>
    /// <param name="connectionType">The connection type.</param>
    /// <returns>The error, with code <see cref="NotSupportedCode"/>.</returns>
    public static EncinaError NotSupported(Type connectionType)
    {
        ArgumentNullException.ThrowIfNull(connectionType);
        return EncinaErrors.Create(
            NotSupportedCode,
            $"{connectionType.Name} is not a System.Data.Common.DbConnection, so it cannot begin a transaction asynchronously.");
    }
}
