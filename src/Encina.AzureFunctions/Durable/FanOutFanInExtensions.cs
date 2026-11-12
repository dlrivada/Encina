using System.Text.Json;
using LanguageExt;
using Microsoft.DurableTask;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Extension methods for fan-out/fan-in patterns in Durable Functions.
/// </summary>
/// <remarks>
/// <para>
/// These extensions execute multiple activities in parallel (fan-out) and aggregate their results
/// (fan-in) while maintaining ROP error handling.
/// </para>
/// <para>
/// <b>Fan-out activities return <see cref="ActivityResult{T}"/>.</b> Every method here reads each activity
/// through <see cref="OrchestrationContextExtensions.CallEncinaActivityWithResultAsync{TInput, TResult}"/>, so an
/// activity must return <c>ActivityResult&lt;T&gt;</c> (use <c>ToActivityResult()</c> on its
/// <c>Either</c>). A failure reported by the activity keeps its error code; an activity that throws, or that
/// returns a plain value instead of an <c>ActivityResult&lt;T&gt;</c>, becomes a <c>Left</c> with code
/// <c>durable.activity_failed</c>. Nothing is read as a success by accident.
/// </para>
/// <para>
/// <b>Activities that return plain values.</b> Run them yourself with
/// <see cref="OrchestrationContextExtensions.CallEncinaActivityAsync{TInput, TResult}"/>, wait with
/// <c>Task.WhenAll</c> and split the results with <see cref="Partition{T}"/>.
/// </para>
/// <para>
/// <b>Aggregate error.</b> <see cref="FanOutAllAsync{TInput, TResult}"/> and, when every activity fails,
/// <see cref="FanOutFirstSuccessAsync{TInput, TResult}"/> return one error shape: code
/// <c>durable.fanout_failed</c>, a fixed message, and these <c>details</c> (read them with
/// <c>GetDetails()</c>):
/// <list type="bullet">
/// <item><description><c>total</c> (<see cref="int"/>): the number of activities started.</description></item>
/// <item><description><c>failedCount</c> (<see cref="int"/>): the number of activities that failed.</description></item>
/// <item><description><c>failures</c> (<c>IReadOnlyList&lt;IReadOnlyDictionary&lt;string, object?&gt;&gt;</c>):
/// one entry per failure, ordered by input index, each with <c>index</c> (<see cref="int"/>, the position of
/// the input) and <c>code</c> (<see cref="string"/>, the failure's error code). Messages are never included.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Fan-out: process all items in parallel
/// var results = await context.FanOutAsync&lt;string, OrderDetails&gt;(
///     "GetOrderDetails",
///     items);
///
/// // All succeeded
/// if (results.All(r =&gt; r.IsRight))
/// {
///     var values = results.Rights().ToList();
/// }
/// </code>
/// </example>
public static class FanOutFanInExtensions
{
    private const string FanOutFailedCode = "durable.fanout_failed";
    private const string FanOutFailedMessage = "One or more fan-out activities failed.";

    /// <summary>
    /// Executes an activity for each input in parallel (fan-out) and collects results (fan-in).
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function; it must return <c>ActivityResult&lt;TResult&gt;</c>.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>A list of Either results, one for each input, in input order.</returns>
    /// <remarks>
    /// An activity that throws, or that returns a plain value instead of <c>ActivityResult&lt;TResult&gt;</c>,
    /// yields a <c>Left</c> with code <c>durable.activity_failed</c>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var orderIds = new[] { "order-1", "order-2", "order-3" };
    /// var results = await context.FanOutAsync&lt;string, OrderDetails&gt;(
    ///     "GetOrderDetails",
    ///     orderIds);
    /// </code>
    /// </example>
    public static async Task<IReadOnlyList<Either<EncinaError, TResult>>> FanOutAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options = null)
    {
        var tasks = StartActivities<TInput, TResult>(context, activityName, inputs, options);
        if (tasks.Count == 0)
        {
            return [];
        }

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an activity for each input and returns only if all succeed.
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function; it must return <c>ActivityResult&lt;TResult&gt;</c>.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>
    /// Every result in input order, or one aggregate error with code <c>durable.fanout_failed</c> when at least
    /// one activity failed (even a single one); see the type remarks for its <c>details</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await context.FanOutAllAsync&lt;string, OrderDetails&gt;(
    ///     "GetOrderDetails",
    ///     orderIds);
    ///
    /// result.Match(
    ///     Right: allDetails =&gt; ProcessAllDetails(allDetails),
    ///     Left: error =&gt; HandleError(error));
    /// </code>
    /// </example>
    public static async Task<Either<EncinaError, IReadOnlyList<TResult>>> FanOutAllAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options = null)
    {
        var results = await context.FanOutAsync<TInput, TResult>(activityName, inputs, options);

        var failures = new List<(int Index, EncinaError Error)>();
        var values = new List<TResult>(results.Count);
        for (var index = 0; index < results.Count; index++)
        {
            results[index].Match(
                Right: value => values.Add(value),
                Left: error => failures.Add((index, error)));
        }

        if (failures.Count > 0)
        {
            return CreateAggregateError(results.Count, failures);
        }

        return values;
    }

    /// <summary>
    /// Executes two different activities in parallel and returns when both complete.
    /// </summary>
    /// <typeparam name="T1">Result type of the first activity.</typeparam>
    /// <typeparam name="T2">Result type of the second activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activity1">First activity definition; the activity must return <c>ActivityResult&lt;T1&gt;</c>.</param>
    /// <param name="activity2">Second activity definition; the activity must return <c>ActivityResult&lt;T2&gt;</c>.</param>
    /// <returns>A tuple of Either results.</returns>
    public static async Task<(Either<EncinaError, T1>, Either<EncinaError, T2>)> FanOutMultipleAsync<T1, T2>(
        this TaskOrchestrationContext context,
        (string Name, object Input) activity1,
        (string Name, object Input) activity2)
    {
        ArgumentNullException.ThrowIfNull(context);

        var task1 = CallActivityAsync<object, T1>(context, activity1.Name, activity1.Input, null);
        var task2 = CallActivityAsync<object, T2>(context, activity2.Name, activity2.Input, null);

        await Task.WhenAll(task1, task2);

        return (await task1, await task2);
    }

    /// <summary>
    /// Executes three different activities in parallel and returns when all complete.
    /// </summary>
    /// <typeparam name="T1">Result type of the first activity.</typeparam>
    /// <typeparam name="T2">Result type of the second activity.</typeparam>
    /// <typeparam name="T3">Result type of the third activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activity1">First activity definition; the activity must return <c>ActivityResult&lt;T1&gt;</c>.</param>
    /// <param name="activity2">Second activity definition; the activity must return <c>ActivityResult&lt;T2&gt;</c>.</param>
    /// <param name="activity3">Third activity definition; the activity must return <c>ActivityResult&lt;T3&gt;</c>.</param>
    /// <returns>A tuple of Either results.</returns>
    public static async Task<(Either<EncinaError, T1>, Either<EncinaError, T2>, Either<EncinaError, T3>)>
        FanOutMultipleAsync<T1, T2, T3>(
            this TaskOrchestrationContext context,
            (string Name, object Input) activity1,
            (string Name, object Input) activity2,
            (string Name, object Input) activity3)
    {
        ArgumentNullException.ThrowIfNull(context);

        var task1 = CallActivityAsync<object, T1>(context, activity1.Name, activity1.Input, null);
        var task2 = CallActivityAsync<object, T2>(context, activity2.Name, activity2.Input, null);
        var task3 = CallActivityAsync<object, T3>(context, activity3.Name, activity3.Input, null);

        await Task.WhenAll(task1, task2, task3);

        return (await task1, await task2, await task3);
    }

    /// <summary>
    /// Returns the first successful result to complete among parallel activities.
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function; it must return <c>ActivityResult&lt;TResult&gt;</c>.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options.</param>
    /// <returns>
    /// The first <c>Right</c> to complete, or, when every activity failed, one aggregate error with code
    /// <c>durable.fanout_failed</c> (see the type remarks for its <c>details</c>).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method waits with <c>Task.WhenAny</c>: a failure that completes first is ignored and the wait continues
    /// until a success completes or no activity remains. "First" is by completion, not by input order. For an
    /// ordered choice call <c>FanOutAsync</c> and take the first <c>Right</c> in the list.
    /// </para>
    /// <para>
    /// The activities that are still running when a success returns are not cancelled; Durable Functions cannot
    /// cancel a running activity, so the orchestration completes only when they finish too.
    /// </para>
    /// </remarks>
    public static async Task<Either<EncinaError, TResult>> FanOutFirstSuccessAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options = null)
    {
        var pending = StartActivities<TInput, TResult>(context, activityName, inputs, options);
        var inputIndexes = Enumerable.Range(0, pending.Count).ToList();
        var total = pending.Count;
        var failures = new List<(int Index, EncinaError Error)>();

        while (pending.Count > 0)
        {
            var completed = await Task.WhenAny(pending);
            var position = pending.IndexOf(completed);
            var result = await completed;

            if (result.IsRight)
            {
                return result;
            }

            failures.Add((inputIndexes[position], result.Match(Right: _ => default!, Left: error => error)));
            pending.RemoveAt(position);
            inputIndexes.RemoveAt(position);
        }

        return CreateAggregateError(total, failures);
    }

    /// <summary>
    /// Partitions results into successes and failures.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="results">The Either results to partition.</param>
    /// <returns>A tuple of (successes, failures).</returns>
    public static (IReadOnlyList<T> Successes, IReadOnlyList<EncinaError> Failures) Partition<T>(
        this IEnumerable<Either<EncinaError, T>> results)
    {
        var successes = new List<T>();
        var failures = new List<EncinaError>();

        foreach (var result in results)
        {
            result.Match(
                Right: value => successes.Add(value),
                Left: error => failures.Add(error));
        }

        return (successes, failures);
    }

    private static List<Task<Either<EncinaError, TResult>>> StartActivities<TInput, TResult>(
        TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(activityName);
        ArgumentNullException.ThrowIfNull(inputs);

        return inputs
            .Select(input => CallActivityAsync<TInput, TResult>(context, activityName, input, options))
            .ToList();
    }

    /// <summary>
    /// Reads one activity as an <see cref="ActivityResult{T}"/>. An output that is not an
    /// <c>ActivityResult&lt;T&gt;</c> (a plain value) fails to deserialize; that is reported as a
    /// <c>durable.activity_failed</c> <c>Left</c>, never as a success and never as a failed orchestration.
    /// </summary>
    private static async Task<Either<EncinaError, TResult>> CallActivityAsync<TInput, TResult>(
        TaskOrchestrationContext context,
        string activityName,
        TInput input,
        TaskOptions? options)
    {
        try
        {
            return await context.CallEncinaActivityWithResultAsync<TInput, TResult>(activityName, input, options);
        }
        catch (JsonException ex)
        {
            return EncinaErrors.Create(
                "durable.activity_failed",
                $"Activity '{activityName}' did not return an ActivityResult.",
                ex);
        }
    }

    private static EncinaError CreateAggregateError(int total, List<(int Index, EncinaError Error)> failures)
    {
        var entries = failures
            .OrderBy(failure => failure.Index)
            .Select(failure => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["index"] = failure.Index,
                ["code"] = failure.Error.GetCode().IfNone(string.Empty)
            })
            .ToList();

        return EncinaErrors.Create(
            FanOutFailedCode,
            FanOutFailedMessage,
            details: new Dictionary<string, object?>
            {
                ["total"] = total,
                ["failedCount"] = failures.Count,
                ["failures"] = entries
            });
    }
}
