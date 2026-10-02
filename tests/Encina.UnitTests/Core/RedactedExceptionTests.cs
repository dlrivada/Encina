using Encina.Diagnostics;

namespace Encina.UnitTests.Core;

/// <summary>
/// Unit tests for <see cref="RedactedException"/> and <see cref="ExceptionLoggingExtensions.ForLogging"/>.
/// </summary>
public sealed class RedactedExceptionTests
{
    private const string Sentinel = "patient-123-secret";

    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception caught)
        {
            return caught;
        }
    }

    [Fact]
    public void ForLogging_KeepsTheTypeAndDropsTheMessage()
    {
        var original = Thrown(new InvalidOperationException(Sentinel));

        var redacted = original.ForLogging();

        redacted.ShouldBeOfType<RedactedException>();
        redacted.Message.ShouldBe(typeof(InvalidOperationException).FullName);
        redacted.Message.ShouldNotContain(Sentinel);
    }

    [Fact]
    public void ForLogging_PreservesTheStackTrace()
    {
        var original = Thrown(new InvalidOperationException(Sentinel));

        var redacted = original.ForLogging();

        redacted.StackTrace.ShouldNotBeNull();
        redacted.StackTrace.ShouldBe(original.StackTrace);
        redacted.StackTrace.ShouldContain(nameof(Thrown));
    }

    [Fact]
    public void ForLogging_WithoutStackTrace_HasNullStackTrace()
    {
        var redacted = new InvalidOperationException(Sentinel).ForLogging();

        redacted.StackTrace.ShouldBeNull();
        redacted.ToString().ShouldNotContain(Sentinel);
    }

    [Fact]
    public void ForLogging_NestedInnerExceptions_AreRedactedRecursively()
    {
        var original = Thrown(new InvalidOperationException(
            $"outer {Sentinel}",
            Thrown(new ArgumentException($"middle {Sentinel}", new TimeoutException($"inner {Sentinel}")))));

        var redacted = original.ForLogging();

        var middle = redacted.InnerException.ShouldBeOfType<RedactedException>();
        middle.Message.ShouldBe(typeof(ArgumentException).FullName);
        var inner = middle.InnerException.ShouldBeOfType<RedactedException>();
        inner.Message.ShouldBe(typeof(TimeoutException).FullName);
        inner.InnerException.ShouldBeNull();
        redacted.ToString().ShouldNotContain(Sentinel);
        redacted.ToString().ShouldContain(typeof(TimeoutException).FullName!);
    }

    [Fact]
    public void ForLogging_AggregateException_RedactsEveryInnerException()
    {
        var original = new AggregateException(
            $"aggregate {Sentinel}",
            Thrown(new InvalidOperationException($"first {Sentinel}")),
            Thrown(new FormatException($"second {Sentinel}")));

        var redacted = original.ForLogging().ShouldBeOfType<RedactedException>();

        redacted.Message.ShouldBe(typeof(AggregateException).FullName);
        redacted.InnerExceptions.Count.ShouldBe(2);
        redacted.InnerExceptions[0].Message.ShouldBe(typeof(InvalidOperationException).FullName);
        redacted.InnerExceptions[1].Message.ShouldBe(typeof(FormatException).FullName);
        redacted.InnerException.ShouldBeSameAs(redacted.InnerExceptions[0]);
        redacted.ToString().ShouldNotContain(Sentinel);
        redacted.ToString().ShouldContain(typeof(FormatException).FullName!);
    }

    [Fact]
    public void ForLogging_DoesNotCopyTheDataDictionary()
    {
        var original = new InvalidOperationException("boom");
        original.Data["secret"] = Sentinel;

        var redacted = original.ForLogging();

        redacted.Data.Count.ShouldBe(0);
        redacted.ToString().ShouldNotContain(Sentinel);
    }

    [Fact]
    public void ForLogging_ToStringNeverContainsTheSentinel()
    {
        var original = Thrown(new InvalidOperationException($"outer {Sentinel}", new ArgumentException(Sentinel)));

        var text = original.ForLogging().ToString();

        text.ShouldNotContain(Sentinel);
        text.ShouldContain(typeof(InvalidOperationException).FullName!);
    }

    [Fact]
    public void ForLogging_NestedAggregateException_RedactsEveryLevel()
    {
        var nested = new AggregateException(
            $"nested {Sentinel}",
            new InvalidOperationException($"a {Sentinel}"),
            new FormatException($"b {Sentinel}"));
        var original = new AggregateException($"outer {Sentinel}", nested, new TimeoutException($"c {Sentinel}"));

        var redacted = original.ForLogging().ShouldBeOfType<RedactedException>();

        redacted.InnerExceptions.Count.ShouldBe(2);
        var nestedRedacted = redacted.InnerExceptions[0].ShouldBeOfType<RedactedException>();
        nestedRedacted.Message.ShouldBe(typeof(AggregateException).FullName);
        nestedRedacted.InnerExceptions.Count.ShouldBe(2);
        redacted.InnerExceptions[1].Message.ShouldBe(typeof(TimeoutException).FullName);
        redacted.ToString().ShouldNotContain(Sentinel);
        redacted.ToString().ShouldContain(typeof(FormatException).FullName!);
    }

    [Fact]
    public void ForLogging_HundredDeepInnerChain_IsRedactedWithoutOverflow()
    {
        Exception original = new InvalidOperationException(Sentinel);
        for (var i = 0; i < 100; i++)
        {
            original = new ArgumentException($"level {i} {Sentinel}", original);
        }

        var redacted = original.ForLogging();

        var depth = 0;
        for (var current = redacted; current is not null; current = current.InnerException)
        {
            depth++;
            current.Message.ShouldNotContain(Sentinel);
        }

        depth.ShouldBe(101);
        redacted.ToString().ShouldNotContain(Sentinel);
    }

    [Fact]
    public void ForLogging_StackTraceGetterThrows_FallsBackToTheTypeName()
    {
        var original = new ThrowingStackTraceException(Sentinel);

        var redacted = original.ForLogging().ShouldBeOfType<RedactedException>();

        redacted.Message.ShouldBe(typeof(ThrowingStackTraceException).FullName);
        redacted.StackTrace.ShouldBeNull();
        redacted.InnerExceptions.ShouldBeEmpty();
        redacted.ToString().ShouldNotContain(Sentinel);
    }

    [Fact]
    public void ForLogging_InnerStackTraceGetterThrows_FallsBackToTheTypeName()
    {
        var original = new InvalidOperationException(Sentinel, new ThrowingStackTraceException(Sentinel));

        var redacted = original.ForLogging().ShouldBeOfType<RedactedException>();

        redacted.Message.ShouldBe(typeof(InvalidOperationException).FullName);
        redacted.ToString().ShouldNotContain(Sentinel);
    }

    private sealed class ThrowingStackTraceException(string message) : Exception(message)
    {
        public override string? StackTrace => throw new InvalidOperationException("stack trace unavailable");
    }

    [Fact]
    public void ForLogging_AlreadyRedacted_ReturnsTheSameInstance()
    {
        var redacted = new InvalidOperationException(Sentinel).ForLogging();

        redacted.ForLogging().ShouldBeSameAs(redacted);
    }
}
