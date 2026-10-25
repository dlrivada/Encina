using System.Threading.Channels;

using Microsoft.Extensions.Time.Testing;

namespace Encina.TestInfrastructure.Time;

/// <summary>
/// A fake clock that signals every timer created on it, so a test advances time only once the
/// timers it means to fire exist (deterministic, no polling).
/// </summary>
/// <remarks>
/// Code under test usually creates its timers from a background task. Advancing the clock before
/// the timer exists silently does nothing, so a test calls <see cref="WaitForTimersAsync"/> first.
/// <para>
/// Signals accumulate for every timer created between waits, so a timer created earlier (for example in
/// a previous phase of the test) can satisfy a later <see cref="WaitForTimersAsync"/> call; wait for
/// exactly the timers of the phase you are about to advance. Signals consumed by a wait that then times
/// out are lost, so a retry on the same instance sees a lower count.
/// </para>
/// </remarks>
/// <param name="start">The initial time of the clock.</param>
public sealed class TimerSignallingTimeProvider(DateTimeOffset start) : FakeTimeProvider(start)
{
    /// <summary>The longest <see cref="WaitForTimersAsync"/> waits before it fails the test.</summary>
    private static readonly TimeSpan WaitBound = TimeSpan.FromSeconds(5);

    private readonly Channel<bool> _created = Channel.CreateUnbounded<bool>();

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        _created.Writer.TryWrite(true);
        return timer;
    }

    /// <summary>
    /// Waits until <paramref name="count"/> timers have been created on this clock since the previous wait.
    /// </summary>
    /// <param name="count">The number of timers to wait for.</param>
    /// <param name="cancellationToken">The test's cancellation token (<c>TestContext.Current.CancellationToken</c>).</param>
    /// <exception cref="TimeoutException">
    /// Fewer than <paramref name="count"/> timers were created within five seconds; the message reads
    /// "expected N timers, saw M".
    /// </exception>
    public async Task WaitForTimersAsync(int count, CancellationToken cancellationToken)
    {
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bound.CancelAfter(WaitBound);

        var seen = 0;
        try
        {
            for (; seen < count; seen++)
            {
                await _created.Reader.ReadAsync(bound.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"expected {count} timers, saw {seen}");
        }
    }
}
