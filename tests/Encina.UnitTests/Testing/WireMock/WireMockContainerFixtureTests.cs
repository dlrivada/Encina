using Encina.Testing.WireMock;

namespace Encina.UnitTests.Testing.WireMock;

/// <summary>
/// Tests for the parts of <see cref="WireMockContainerFixture"/> that run without starting a Docker
/// container; the container lifecycle itself is covered by the integration flag.
/// </summary>
public sealed class WireMockContainerFixtureTests
{
    [Fact]
    public void Container_BeforeInitialization_Throws()
    {
        var fixture = new WireMockContainerFixture();

        var ex = Should.Throw<InvalidOperationException>(() => fixture.Container);

        ex.Message.ShouldContain("Container not initialized");
    }

    [Fact]
    public void BaseUrl_BeforeInitialization_Throws()
    {
        var fixture = new WireMockContainerFixture();

        Should.Throw<InvalidOperationException>(() => fixture.BaseUrl);
    }

    [Fact]
    public void CreateClient_BeforeInitialization_Throws()
    {
        var fixture = new WireMockContainerFixture();

        Should.Throw<InvalidOperationException>(() => fixture.CreateClient());
    }

    [Fact]
    public void CreateAdminClient_BeforeInitialization_Throws()
    {
        var fixture = new WireMockContainerFixture();

        Should.Throw<InvalidOperationException>(() => fixture.CreateAdminClient());
    }

    [Fact]
    public async Task ResetAsync_BeforeInitialization_Throws()
    {
        var fixture = new WireMockContainerFixture();

        await Should.ThrowAsync<InvalidOperationException>(() => fixture.ResetAsync());
    }

    [Fact]
    public async Task DisposeAsync_BeforeInitialization_DoesNotThrow()
    {
        var fixture = new WireMockContainerFixture();

        await Should.NotThrowAsync(async () => await fixture.DisposeAsync());
    }
}
