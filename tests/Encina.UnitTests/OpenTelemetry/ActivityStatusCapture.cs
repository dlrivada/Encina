using System.Diagnostics;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry;

/// <summary>
/// Records every stopped activity of the Encina sources so a test can prove what a decorator wrote
/// to the status description and tags when the wrapped call failed (#2134).
/// </summary>
internal sealed class ActivityStatusCapture : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _stopped = [];
    private readonly Lock _gate = new();

    /// <summary>
    /// Creates a capture for every Encina source, or only for <paramref name="sourceName"/> when given
    /// so that tests running in parallel cannot leak activities into this one.
    /// </summary>
    public ActivityStatusCapture(string? sourceName = null)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => sourceName is null
                ? source.Name.StartsWith("Encina", StringComparison.Ordinal)
                : string.Equals(source.Name, sourceName, StringComparison.Ordinal),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (_gate)
                {
                    _stopped.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Asserts that an error activity ended with exactly <paramref name="expectedDescription"/> and
    /// that no captured activity carries <paramref name="secret"/> in its status description,
    /// display name or tags.
    /// </summary>
    public void ShouldHaveErrorStatus(string expectedDescription, string secret)
    {
        var snapshot = Snapshot();

        snapshot.Any(a => a.Status == ActivityStatusCode.Error && a.StatusDescription == expectedDescription)
            .ShouldBeTrue($"no error activity with status description '{expectedDescription}'");

        ShouldNeverCarry(secret);
    }

    /// <summary>
    /// Asserts that an error activity carries <paramref name="expectedValue"/> in the tag
    /// <paramref name="tagKey"/> and that no captured activity carries <paramref name="secret"/>.
    /// </summary>
    public void ShouldHaveErrorTag(string tagKey, string expectedValue, string secret)
    {
        Snapshot()
            .Any(a => a.Status == ActivityStatusCode.Error && Equals(a.GetTagItem(tagKey), expectedValue))
            .ShouldBeTrue($"no error activity with tag '{tagKey}' = '{expectedValue}'");

        ShouldNeverCarry(secret);
    }

    /// <summary>
    /// Asserts that no captured activity carries <paramref name="secret"/> in its status description,
    /// display name or tags.
    /// </summary>
    public void ShouldNeverCarry(string secret)
    {
        foreach (var activity in Snapshot())
        {
            (activity.StatusDescription ?? string.Empty).ShouldNotContain(secret);
            activity.DisplayName.ShouldNotContain(secret);
            foreach (var tag in activity.TagObjects)
            {
                (tag.Value?.ToString() ?? string.Empty).ShouldNotContain(secret);
            }
        }
    }

    /// <summary>
    /// Gets the status description of every captured error activity.
    /// </summary>
    public IReadOnlyList<string?> ErrorStatusDescriptions() =>
        [.. Snapshot().Where(a => a.Status == ActivityStatusCode.Error).Select(a => a.StatusDescription)];

    /// <summary>
    /// Waits until an error activity with exactly <paramref name="expectedDescription"/> has stopped,
    /// for background services that report asynchronously. An error activity with another description
    /// never ends the wait.
    /// </summary>
    public async Task WaitForErrorActivityAsync(string expectedDescription, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs
            && !Snapshot().Any(a => a.Status == ActivityStatusCode.Error && a.StatusDescription == expectedDescription))
        {
            await Task.Delay(10);
        }
    }

    private Activity[] Snapshot()
    {
        lock (_gate)
        {
            return [.. _stopped];
        }
    }

    public void Dispose() => _listener.Dispose();
}
