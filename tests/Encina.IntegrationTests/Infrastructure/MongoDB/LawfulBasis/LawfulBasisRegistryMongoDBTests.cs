using Encina.Compliance.GDPR;
using Encina.MongoDB.LawfulBasis;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.Infrastructure.MongoDB.LawfulBasis;

[Collection("MongoDB")]
[Trait("Category", "Integration")]
[Trait("Database", "MongoDB")]
public sealed class LawfulBasisRegistryMongoDBTests : IAsyncLifetime
{
    private readonly MongoDbFixture _fixture;

    public LawfulBasisRegistryMongoDBTests(MongoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        // MongoDB collections are auto-created; clearing is done per-test if needed
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private LawfulBasisRegistryMongoDB CreateStore() =>
        new(_fixture.ConnectionString, MongoDbFixture.DatabaseName);

    private static LawfulBasisRegistration CreateRegistration(
        Type? requestType = null,
        global::Encina.Compliance.GDPR.LawfulBasis basis = global::Encina.Compliance.GDPR.LawfulBasis.Contract) => new()
    {
        RequestType = requestType ?? typeof(LawfulBasisRegistryMongoDBTests),
        Basis = basis,
        Purpose = "Integration test purpose",
        RegisteredAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task RegisterAsync_ValidRegistration_ShouldPersist()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var registration = CreateRegistration();
        var result = await store.RegisterAsync(registration);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();
        await store.RegisterAsync(CreateRegistration(typeof(int)));

        var result = await store.GetByRequestTypeAsync(typeof(int));
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();

        var result = await store.GetByRequestTypeAsync(typeof(double));
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithRegistrations_ShouldReturnAll()
    {
        if (!_fixture.IsAvailable) return;
        var store = CreateStore();
        await store.RegisterAsync(CreateRegistration(typeof(byte)));
        await store.RegisterAsync(CreateRegistration(typeof(short)));

        var result = await store.GetAllAsync();
        result.IsRight.Should().BeTrue();
        var registrations = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
        registrations.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
