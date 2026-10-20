using System.Diagnostics;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// Provides the activity source for the shared transaction (<c>Encina.Messaging.Transactions</c>).
/// </summary>
/// <remarks>
/// One <c>encina.transaction</c> activity spans the life of a transaction, from begin to commit or rollback,
/// and carries only the tags <c>encina.transaction.owner</c>, <c>encina.transaction.outcome</c> and
/// <c>db.system</c>; never a payload or an error message. The methods guard with
/// <see cref="ActivitySource.HasListeners()"/> so they cost nothing when no listener is attached.
/// </remarks>
internal static class TransactionActivitySource
{
    /// <summary>The activity source name for external registration (for example in an OpenTelemetry builder).</summary>
    internal const string SourceName = "Encina.Messaging.Transactions";

    /// <summary>The tag naming the participant that owns the transaction.</summary>
    internal const string OwnerTag = "encina.transaction.owner";

    /// <summary>The tag carrying how the transaction ended.</summary>
    internal const string OutcomeTag = "encina.transaction.outcome";

    /// <summary>The transaction was committed.</summary>
    internal const string OutcomeCommitted = "committed";

    /// <summary>The transaction was rolled back.</summary>
    internal const string OutcomeRolledBack = "rolled_back";

    /// <summary>The owner's commit was refused because a participant rolled back; the transaction was rolled back.</summary>
    internal const string OutcomeRollbackOnly = "rollback_only";

    /// <summary>The commit failed and the transaction was rolled back.</summary>
    internal const string OutcomeCommitFailed = "commit_failed";

    internal static readonly ActivitySource ActivitySource = new(SourceName, "1.0");

    /// <summary>
    /// Starts the activity of a transaction that was just begun.
    /// </summary>
    /// <param name="dbSystem">The <c>db.system</c> value of the connection.</param>
    /// <returns>The started activity, or <see langword="null"/> when no listener is active.</returns>
    internal static Activity? StartTransaction(string dbSystem)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        var activity = ActivitySource.StartActivity("encina.transaction", ActivityKind.Internal);
        activity?.SetTag(OwnerTag, true);
        activity?.SetTag("db.system", dbSystem);
        return activity;
    }

    /// <summary>
    /// Ends a transaction's activity with its outcome.
    /// </summary>
    /// <param name="activity">The activity to end.</param>
    /// <param name="outcome">One of the <c>Outcome*</c> constants.</param>
    internal static void Complete(Activity? activity, string outcome)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(OutcomeTag, outcome);
        activity.SetStatus(outcome is OutcomeCommitFailed or OutcomeRollbackOnly ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
        activity.Dispose();
    }
}
