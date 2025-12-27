using LanguageExt;
using Microsoft.DurableTask;

namespace Encina.AzureFunctions.Durable;

/// <summary>
/// Extension methods for fan-out/fan-in patterns in Durable Functions.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide convenient methods for executing multiple activities in parallel
/// (fan-out) and aggregating their results (fan-in) while maintaining ROP error handling.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Fan-out: process all items in parallel
/// var results = await context.FanOutAsync(
///     "ProcessItem",
///     items,
///     maxConcurrency: 10);
///
/// // All succeeded
/// if (results.All(r => r.IsRight))
/// {
///     var values = results.Rights().ToList();
/// }
/// </code>
/// </example>
public static class FanOutFanInExtensions
{
    /// <summary>
    /// Executes an activity for each input in parallel (fan-out) and collects results (fan-in).
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>A list of Either results, one for each input.</returns>
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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(activityName);
        ArgumentNullException.ThrowIfNull(inputs);

        var inputList = inputs.ToList();
        if (inputList.Count == 0)
        {
            return [];
        }

        var tasks = inputList.Select(input =>
            context.CallEncinaActivityAsync<TInput, TResult>(activityName, input, options));

        var results = await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Executes an activity for each input and returns only if all succeed.
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>Either all successful results or the first error encountered.</returns>
    /// <example>
    /// <code>
    /// var result = await context.FanOutAllAsync&lt;string, OrderDetails&gt;(
    ///     "GetOrderDetails",
    ///     orderIds);
    ///
    /// result.Match(
    ///     Right: allDetails => ProcessAllDetails(allDetails),
    ///     Left: error => HandleError(error));
    /// </code>
    /// </example>
    public static async Task<Either<EncinaError, IReadOnlyList<TResult>>> FanOutAllAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options = null)
    {
        var results = await context.FanOutAsync<TInput, TResult>(activityName, inputs, options);

        var errors = results.Where(r => r.IsLeft).ToList();
        if (errors.Count > 0)
        {
            var firstError = errors[0].Match(Left: e => e, Right: _ => default!);
            if (errors.Count == 1)
            {
                return firstError;
            }

            return EncinaErrors.Create(
                "durable.fanout_failed",
                $"Fan-out failed: {errors.Count} of {results.Count} activities failed. First error: {firstError.Message}");
        }

        var successValues = results.Select(r => r.Match(Right: v => v, Left: _ => default!)).ToList();
        return successValues;
    }

    /// <summary>
    /// Executes an activity for each input with activities returning Either results.
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function that returns ActivityResult.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options for retry configuration.</param>
    /// <returns>A list of Either results.</returns>
    public static async Task<IReadOnlyList<Either<EncinaError, TResult>>> FanOutWithResultAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrEmpty(activityName);
        ArgumentNullException.ThrowIfNull(inputs);

        var inputList = inputs.ToList();
        if (inputList.Count == 0)
        {
            return [];
        }

        var tasks = inputList.Select(input =>
            context.CallEncinaActivityWithResultAsync<TInput, TResult>(activityName, input, options));

        var results = await Task.WhenAll(tasks);
        return results;
    }

    /// <summary>
    /// Executes multiple different activities in parallel and returns when all complete.
    /// </summary>
    /// <typeparam name="T1">Result type of the first activity.</typeparam>
    /// <typeparam name="T2">Result type of the second activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activity1">First activity definition.</param>
    /// <param name="activity2">Second activity definition.</param>
    /// <returns>A tuple of Either results.</returns>
    public static async Task<(Either<EncinaError, T1>, Either<EncinaError, T2>)> FanOutMultipleAsync<T1, T2>(
        this TaskOrchestrationContext context,
        (string Name, object Input) activity1,
        (string Name, object Input) activity2)
    {
        ArgumentNullException.ThrowIfNull(context);

        var task1 = context.CallEncinaActivityAsync<object, T1>(activity1.Name, activity1.Input);
        var task2 = context.CallEncinaActivityAsync<object, T2>(activity2.Name, activity2.Input);

        await Task.WhenAll(task1, task2);

        return (await task1, await task2);
    }

    /// <summary>
    /// Executes three different activities in parallel.
    /// </summary>
    public static async Task<(Either<EncinaError, T1>, Either<EncinaError, T2>, Either<EncinaError, T3>)>
        FanOutMultipleAsync<T1, T2, T3>(
            this TaskOrchestrationContext context,
            (string Name, object Input) activity1,
            (string Name, object Input) activity2,
            (string Name, object Input) activity3)
    {
        ArgumentNullException.ThrowIfNull(context);

        var task1 = context.CallEncinaActivityAsync<object, T1>(activity1.Name, activity1.Input);
        var task2 = context.CallEncinaActivityAsync<object, T2>(activity2.Name, activity2.Input);
        var task3 = context.CallEncinaActivityAsync<object, T3>(activity3.Name, activity3.Input);

        await Task.WhenAll(task1, task2, task3);

        return (await task1, await task2, await task3);
    }

    /// <summary>
    /// Selects the first successful result from parallel activities, or returns all errors.
    /// </summary>
    /// <typeparam name="TInput">The input type for each activity.</typeparam>
    /// <typeparam name="TResult">The result type from each activity.</typeparam>
    /// <param name="context">The orchestration context.</param>
    /// <param name="activityName">The name of the activity function.</param>
    /// <param name="inputs">The collection of inputs to process.</param>
    /// <param name="options">Optional task options.</param>
    /// <returns>The first successful result or all errors combined.</returns>
    /// <remarks>
    /// Useful for scenarios like trying multiple service instances and taking the first response.
    /// </remarks>
    public static async Task<Either<EncinaError, TResult>> FanOutFirstSuccessAsync<TInput, TResult>(
        this TaskOrchestrationContext context,
        string activityName,
        IEnumerable<TInput> inputs,
        TaskOptions? options = null)
    {
        var results = await context.FanOutAsync<TInput, TResult>(activityName, inputs, options);

        var successResult = results.FirstOrDefault(r => r.IsRight);
        if (successResult.IsRight)
        {
            return successResult;
        }

        var errors = results.Select(r => r.Match(Left: e => e.Message, Right: _ => "")).ToList();
        return EncinaErrors.Create(
            "durable.fanout_all_failed",
            $"All {results.Count} activities failed: {string.Join("; ", errors)}");
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
}
