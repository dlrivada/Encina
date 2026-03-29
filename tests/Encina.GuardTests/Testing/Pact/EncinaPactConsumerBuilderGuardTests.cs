using Encina.Testing.Pact;

namespace Encina.GuardTests.Testing.Pact;

public class EncinaPactConsumerBuilderGuardTests
{
    [Fact]
    public void Constructor_NullConsumerName_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder(null!, "provider"));
    }

    [Fact]
    public void Constructor_EmptyConsumerName_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("", "provider"));
    }

    [Fact]
    public void Constructor_WhitespaceConsumerName_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("  ", "provider"));
    }

    [Fact]
    public void Constructor_NullProviderName_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("consumer", null!));
    }

    [Fact]
    public void Constructor_EmptyProviderName_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("consumer", ""));
    }

    [Fact]
    public void Constructor_WhitespaceProviderName_Throws()
    {
        Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("consumer", "  "));
    }
}
