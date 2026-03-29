using Encina.Testing.Pact;

namespace Encina.GuardTests.Testing.Pact;

public class EncinaPactProviderVerifierGuardTests
{
    [Fact]
    public void Constructor_NullEncina_Throws()
    {
        var sp = Substitute.For<IServiceProvider>();
        Should.Throw<ArgumentNullException>(() =>
            new EncinaPactProviderVerifier(null!, sp));
    }

    [Fact]
    public void Constructor_NullServiceProvider_Throws()
    {
        var encina = Substitute.For<IEncina>();
        Should.Throw<ArgumentNullException>(() =>
            new EncinaPactProviderVerifier(encina, null!));
    }

    [Fact]
    public void WithProviderName_NullName_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sp = Substitute.For<IServiceProvider>();
        var verifier = new EncinaPactProviderVerifier(encina, sp);

        Should.Throw<ArgumentException>(() => verifier.WithProviderName(null!));
    }

    [Fact]
    public void WithProviderName_EmptyName_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sp = Substitute.For<IServiceProvider>();
        var verifier = new EncinaPactProviderVerifier(encina, sp);

        Should.Throw<ArgumentException>(() => verifier.WithProviderName(""));
    }

    [Fact]
    public void OnMissingProviderState_NullHandler_Throws()
    {
        var encina = Substitute.For<IEncina>();
        var sp = Substitute.For<IServiceProvider>();
        var verifier = new EncinaPactProviderVerifier(encina, sp);

        Should.Throw<ArgumentNullException>(() => verifier.OnMissingProviderState(null!));
    }
}
