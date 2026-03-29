using Encina.Testing.Pact;

namespace Encina.GuardTests.Testing.Pact;

public class EncinaPactFixtureGuardTests
{
    [Fact]
    public void WithEncina_NullEncina_Throws()
    {
        var fixture = new EncinaPactFixture();
        var sp = Substitute.For<IServiceProvider>();

        Should.Throw<ArgumentNullException>(() => fixture.WithEncina(null!, sp));
    }

    [Fact]
    public void WithEncina_NullServiceProvider_Throws()
    {
        var fixture = new EncinaPactFixture();
        var encina = Substitute.For<IEncina>();

        Should.Throw<ArgumentNullException>(() => fixture.WithEncina(encina, null!));
    }

    [Fact]
    public void WithServices_NullConfigureServices_Throws()
    {
        var fixture = new EncinaPactFixture();

        Should.Throw<ArgumentNullException>(() => fixture.WithServices(null!));
    }

    [Fact]
    public void CreateConsumer_NullConsumerName_Throws()
    {
        var fixture = new EncinaPactFixture();

        Should.Throw<ArgumentException>(() => fixture.CreateConsumer(null!, "provider"));
    }

    [Fact]
    public void CreateConsumer_EmptyProviderName_Throws()
    {
        var fixture = new EncinaPactFixture();

        Should.Throw<ArgumentException>(() => fixture.CreateConsumer("consumer", ""));
    }
}
