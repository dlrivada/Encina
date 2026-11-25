using System.Collections.Concurrent;
using System.Diagnostics;

namespace Encina.UnitTests.Support;

/// <summary>
/// Captures the activities a component under test stops on one <see cref="ActivitySource"/>. The capture starts
/// a root activity of its own and keeps only the activities of that trace, so activities that tests running in
/// parallel create on the same source never reach the assertions.
/// </summary>
internal sealed class ActivityCapture : IDisposable
{
    // The listener matches the root source by this constant: creating RootSource asks every existing listener
    // whether it listens, so a listener must never read RootSource itself.
    private const string RootSourceName = "Encina.UnitTests.ActivityCapture";

    private static readonly ActivitySource RootSource = new(RootSourceName);

    private readonly ConcurrentQueue<Activity> _stopped = new();
    private readonly ActivityListener _listener;
    private readonly Activity _root;

    /// <summary>
    /// Starts listening to <paramref name="sourceName"/> and starts the root activity; create it with a
    /// <c>using</c> declaration in the test method so the root flows into the calls under test.
    /// </summary>
    public ActivityCapture(string sourceName)
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName || source.Name == RootSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = Record,
        };
        ActivitySource.AddActivityListener(_listener);
        _root = RootSource.StartActivity("activity-capture-root")!;
    }

    /// <summary>Gets the stopped activities of the captured trace with the given operation name.</summary>
    public IReadOnlyList<Activity> Stopped(string operationName)
        => _stopped.Where(activity => activity.OperationName == operationName).ToList();

    /// <inheritdoc />
    public void Dispose()
    {
        _root.Dispose();
        _listener.Dispose();
    }

    private void Record(Activity activity)
    {
        if (_root is not null && activity != _root && activity.TraceId == _root.TraceId)
        {
            _stopped.Enqueue(activity);
        }
    }
}
