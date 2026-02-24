using Encina.Compliance.GDPR;
using Encina.EntityFrameworkCore.LawfulBasis;
using Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.LawfulBasis;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.PostgreSQL.LawfulBasis;

[Collection("EFCore-PostgreSQL")]
[Trait("Category", "Integration")]
[Trait("Provider", "EFCore.PostgreSQL")]
public sealed class LawfulBasisRegistryEFPostgreSqlTests : IAsyncLifetime
{
    private readonly EFCorePostgreSqlFixture _fixture;

    public LawfulBasisRegistryEFPostgreSqlTests(EFCorePostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static LawfulBasisRegistration CreateRegistration(
        Type? requestType = null,
        global::Encina.Compliance.GDPR.LawfulBasis basis = global::Encina.Compliance.GDPR.LawfulBasis.Contract) => new()
        {
            RequestType = requestType ?? typeof(LawfulBasisRegistryEFPostgreSqlTests),
            Basis = basis,
            Purpose = "Integration test purpose",
            RegisteredAtUtc = DateTimeOffset.UtcNow
        };

    [Fact]
    public async Task RegisterAsync_ValidRegistration_ShouldPersist()
    {
        await using var context = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store = new LawfulBasisRegistryEF(context);

        var registration = CreateRegistration();
        var result = await store.RegisterAsync(registration);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateRequestType_ShouldUpsert()
    {
        await using var context1 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store1 = new LawfulBasisRegistryEF(context1);
        await store1.RegisterAsync(CreateRegistration(typeof(string)));

        await using var context2 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store2 = new LawfulBasisRegistryEF(context2);
        // Persistence stores use upsert semantics - duplicate should succeed
        var result = await store2.RegisterAsync(CreateRegistration(typeof(string)));
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        await using var context1 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store1 = new LawfulBasisRegistryEF(context1);
        await store1.RegisterAsync(CreateRegistration(typeof(int)));

        await using var context2 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store2 = new LawfulBasisRegistryEF(context2);
        var result = await store2.GetByRequestTypeAsync(typeof(int));
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        await using var context = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store = new LawfulBasisRegistryEF(context);

        var result = await store.GetByRequestTypeAsync(typeof(double));
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithRegistrations_ShouldReturnAll()
    {
        await using var context1 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store1 = new LawfulBasisRegistryEF(context1);
        await store1.RegisterAsync(CreateRegistration(typeof(byte)));
        await store1.RegisterAsync(CreateRegistration(typeof(short)));

        await using var context2 = _fixture.CreateDbContext<LawfulBasisTestDbContext>();
        var store2 = new LawfulBasisRegistryEF(context2);
        var result = await store2.GetAllAsync();
        result.IsRight.Should().BeTrue();
        var registrations = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
        registrations.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
