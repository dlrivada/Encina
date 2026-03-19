using Encina.Audit.Marten;
using Encina.Audit.Marten.Crypto;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.GuardTests.Marten.Audit;

/// <summary>
/// Guard clause tests for <see cref="InMemoryTemporalKeyProvider"/>.
/// Verifies null/empty/whitespace checks on constructor parameters and public methods.
/// </summary>
[Trait("Category", "Guard")]
[Trait("Provider", "Marten")]
public sealed class InMemoryTemporalKeyProviderGuardTests
{
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InMemoryTemporalKeyProvider(null!, NullLogger<InMemoryTemporalKeyProvider>.Instance));
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            new InMemoryTemporalKeyProvider(TimeProvider.System, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_NullPeriod_ThrowsArgumentException()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetOrCreateKeyAsync(null!).AsTask());
        ex.ParamName.ShouldBe("period");
    }

    [Fact]
    public async Task GetOrCreateKeyAsync_EmptyPeriod_ThrowsArgumentException()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetOrCreateKeyAsync("").AsTask());
        ex.ParamName.ShouldBe("period");
    }

    [Fact]
    public async Task GetKeyAsync_NullPeriod_ThrowsArgumentException()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetKeyAsync(null!).AsTask());
        ex.ParamName.ShouldBe("period");
    }

    [Fact]
    public async Task GetKeyAsync_EmptyPeriod_ThrowsArgumentException()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.GetKeyAsync("").AsTask());
        ex.ParamName.ShouldBe("period");
    }

    [Fact]
    public async Task IsKeyDestroyedAsync_NullPeriod_ThrowsArgumentException()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.IsKeyDestroyedAsync(null!).AsTask());
        ex.ParamName.ShouldBe("period");
    }

    [Fact]
    public async Task IsKeyDestroyedAsync_EmptyPeriod_ThrowsArgumentException()
    {
        var sut = new InMemoryTemporalKeyProvider(TimeProvider.System, NullLogger<InMemoryTemporalKeyProvider>.Instance);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            sut.IsKeyDestroyedAsync("").AsTask());
        ex.ParamName.ShouldBe("period");
    }
}
