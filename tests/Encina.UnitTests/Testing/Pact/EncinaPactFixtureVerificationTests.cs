using Encina.Testing.Pact;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.Testing.Pact;

/// <summary>
/// Exercises <see cref="EncinaPactFixture"/> provider verification and Pact file housekeeping with
/// real Pact files in a per-test temporary folder.
/// </summary>
public sealed class EncinaPactFixtureVerificationTests : IDisposable
{
    private const string GetOrderPath = "/api/queries/PactVerifyGetOrderQuery";

    private static readonly Guid OrderId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly PactVerifyHarness _harness = new();
    private readonly EncinaPactFixture _sut;

    public EncinaPactFixtureVerificationTests()
    {
        _sut = new EncinaPactFixture { PactDirectory = _harness.PactDirectory };
    }

    public void Dispose()
    {
        _sut.Dispose();
        _harness.Dispose();
    }

    private static string OrderRequest(string scenario) =>
        $$"""{"orderId":"{{OrderId}}","scenario":"{{scenario}}"}""";

    [Fact]
    public async Task VerifyProviderAsync_VerifiesEveryJsonFileInPactDirectory()
    {
        _sut.WithServices(PactVerifyHarness.Register);
        await _harness.WritePactAsync(PactVerifyHarness.Interaction("passes", GetOrderPath, OrderRequest("ok"), 200));
        await _harness.WritePactAsync(PactVerifyHarness.Interaction("fails", GetOrderPath, OrderRequest("ok"), 404));
        await File.WriteAllTextAsync(Path.Combine(_harness.PactDirectory, "notes.txt"), "not a pact");

        var results = await _sut.VerifyProviderAsync("OrdersApi");

        results.Count.ShouldBe(2);
        results.Count(r => r.Success).ShouldBe(1);
        results.Count(r => !r.Success).ShouldBe(1);
    }

    [Fact]
    public async Task VerifyProviderAsync_CalledTwice_ReusesTheProviderVerifier()
    {
        _sut.WithServices(PactVerifyHarness.Register);
        await _harness.WritePactAsync(PactVerifyHarness.Interaction("passes", GetOrderPath, OrderRequest("ok"), 200));

        var first = await _sut.VerifyProviderAsync("OrdersApi");
        var second = await _sut.VerifyProviderAsync("OrdersApi");

        first.ShouldHaveSingleItem().Success.ShouldBeTrue();
        second.ShouldHaveSingleItem().Success.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyProviderAsync_UsesVerifierCreatedBeforehand()
    {
        _sut.WithServices(PactVerifyHarness.Register);
        var verifier = _sut.CreateVerifier("OrdersApi");
        verifier.ProviderName.ShouldBe("OrdersApi");
        await _harness.WritePactAsync(PactVerifyHarness.Interaction("passes", GetOrderPath, OrderRequest("ok"), 200));

        var results = await _sut.VerifyProviderAsync("OrdersApi");

        results.ShouldHaveSingleItem().Success.ShouldBeTrue();
    }

    [Fact]
    public async Task VerifyProviderAsync_EmptyDirectory_ReturnsNoResults()
    {
        _sut.WithServices(PactVerifyHarness.Register);

        var results = await _sut.VerifyProviderAsync("OrdersApi");

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task VerifyProviderAsync_WithoutEncina_ThrowsInvalidOperation()
    {
        await Should.ThrowAsync<InvalidOperationException>(() => _sut.VerifyProviderAsync("OrdersApi"));
    }

    [Fact]
    public void CreateVerifier_ServicesWithoutEncina_ThrowsInvalidOperation()
    {
        _sut.WithServices(_ => { });

        var exception = Should.Throw<InvalidOperationException>(() => _sut.CreateVerifier("OrdersApi"));

        exception.Message.ShouldContain("Encina is not configured");
    }

    [Fact]
    public void ClearPactFiles_DeletesOnlyJsonFiles()
    {
        File.WriteAllText(Path.Combine(_harness.PactDirectory, "one.json"), "{}");
        File.WriteAllText(Path.Combine(_harness.PactDirectory, "two.json"), "{}");
        var keep = Path.Combine(_harness.PactDirectory, "keep.txt");
        File.WriteAllText(keep, "keep");

        _sut.ClearPactFiles();

        _sut.GetPactFiles().ShouldBeEmpty();
        File.Exists(keep).ShouldBeTrue();
    }

    [Fact]
    public void ClearPactFiles_MissingDirectory_DoesNothing()
    {
        using var fixture = new EncinaPactFixture
        {
            PactDirectory = Path.Combine(_harness.PactDirectory, "missing")
        };

        Should.NotThrow(fixture.ClearPactFiles);
        fixture.GetPactFiles().ShouldBeEmpty();
    }

    [Fact]
    public void ClearPactFiles_LockedFile_ContinuesWithRemainingFiles()
    {
        var locked = Path.Combine(_harness.PactDirectory, "locked.json");
        File.WriteAllText(locked, "{}");
        File.WriteAllText(Path.Combine(_harness.PactDirectory, "free.json"), "{}");
        using var handle = new FileStream(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Should.NotThrow(_sut.ClearPactFiles);

        // Windows refuses to delete the locked file and the warning branch runs; other platforms unlink it.
        _sut.GetPactFiles().ShouldNotContain(f => Path.GetFileName(f) == "free.json");
    }

    [Fact]
    public void Reset_DisposesVerifiersCreatedByTheFixture()
    {
        _sut.WithServices(PactVerifyHarness.Register);
        var verifier = _sut.CreateVerifier("OrdersApi");

        _sut.Reset();

        Should.Throw<ObjectDisposedException>(() => verifier.VerifyAsync("any.json").GetAwaiter().GetResult());
    }

    [Fact]
    public async Task DisposeAsync_DisposesTheServiceProvider()
    {
        _sut.WithServices(PactVerifyHarness.Register);
        var provider = _sut.ServiceProvider.ShouldNotBeNull();

        await _sut.DisposeAsync();

        Should.Throw<ObjectDisposedException>(() => provider.GetRequiredService<IEncina>());
    }
}
