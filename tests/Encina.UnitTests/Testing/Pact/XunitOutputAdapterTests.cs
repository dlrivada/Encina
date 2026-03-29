using Encina.Testing.Pact;
using Xunit.v3;

namespace Encina.UnitTests.Testing.Pact;

/// <summary>
/// Unit tests for <see cref="XunitOutputAdapter"/> (internal class, accessible via InternalsVisibleTo).
/// </summary>
public sealed class XunitOutputAdapterTests
{
    [Fact]
    public void Constructor_NullOutput_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new XunitOutputAdapter(null!));
    }

    [Fact]
    public void WriteLine_ValidOutput_WritesToHelper()
    {
        var output = Substitute.For<ITestOutputHelper>();
        var adapter = new XunitOutputAdapter(output);

        adapter.WriteLine("test message");

        output.Received(1).WriteLine("test message");
    }

    [Fact]
    public void WriteLine_OutputThrowsNoLongerAvailable_DoesNotRethrow()
    {
        var output = Substitute.For<ITestOutputHelper>();
        output.When(o => o.WriteLine(Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("There is no currently active test"));

        var adapter = new XunitOutputAdapter(output);

        // Should not throw
        Should.NotThrow(() => adapter.WriteLine("test"));
    }

    [Fact]
    public void WriteLine_OutputThrowsNoLongerAvailableAlt_DoesNotRethrow()
    {
        var output = Substitute.For<ITestOutputHelper>();
        output.When(o => o.WriteLine(Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("Output is no longer available"));

        var adapter = new XunitOutputAdapter(output);

        Should.NotThrow(() => adapter.WriteLine("test"));
    }

    [Fact]
    public void WriteLine_OutputThrowsOtherInvalidOperation_Rethrows()
    {
        var output = Substitute.For<ITestOutputHelper>();
        output.When(o => o.WriteLine(Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("Something completely different"));

        var adapter = new XunitOutputAdapter(output);

        Should.Throw<InvalidOperationException>(() => adapter.WriteLine("test"));
    }

    [Fact]
    public void WriteLine_MultipleMessages_AllWritten()
    {
        var output = Substitute.For<ITestOutputHelper>();
        var adapter = new XunitOutputAdapter(output);

        adapter.WriteLine("line 1");
        adapter.WriteLine("line 2");
        adapter.WriteLine("line 3");

        output.Received(3).WriteLine(Arg.Any<string>());
    }
}
