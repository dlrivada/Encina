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

    public ActivityStatusCapture()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name.StartsWith("Encina", StringComparison.Ordinal),
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
        Activity[] snapshot;
        lock (_gate)
        {
            snapshot = [.. _stopped];
        }

        snapshot.Any(a => a.Status == ActivityStatusCode.Error && a.StatusDescription == expectedDescription)
            .ShouldBeTrue($"no error activity with status description '{expectedDescription}'");

        foreach (var activity in snapshot)
        {
            (activity.StatusDescription ?? string.Empty).ShouldNotContain(secret);
            foreach (var tag in activity.TagObjects)
            {
                (tag.Value?.ToString() ?? string.Empty).ShouldNotContain(secret);
            }
        }
    }

    public void Dispose() => _listener.Dispose();
}
