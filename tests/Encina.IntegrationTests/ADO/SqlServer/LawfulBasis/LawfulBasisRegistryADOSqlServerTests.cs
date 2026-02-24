using Encina.ADO.SqlServer.LawfulBasis;
using Encina.Compliance.GDPR;
using Encina.TestInfrastructure.Fixtures;
using FluentAssertions;
using LanguageExt;

namespace Encina.IntegrationTests.ADO.SqlServer.LawfulBasis;

[Collection("ADO-SqlServer")]
[Trait("Category", "Integration")]
[Trait("Provider", "ADO.SqlServer")]
public sealed class LawfulBasisRegistryADOSqlServerTests : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private LawfulBasisRegistryADO _store = null!;

    public LawfulBasisRegistryADOSqlServerTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ClearAllDataAsync();
        _store = new LawfulBasisRegistryADO(_fixture.ConnectionString);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static LawfulBasisRegistration CreateRegistration(
        Type? requestType = null,
        global::Encina.Compliance.GDPR.LawfulBasis basis = global::Encina.Compliance.GDPR.LawfulBasis.Contract) => new()
    {
        RequestType = requestType ?? typeof(LawfulBasisRegistryADOSqlServerTests),
        Basis = basis,
        Purpose = "Integration test purpose",
        RegisteredAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task RegisterAsync_ValidRegistration_ShouldPersist()
    {
        var registration = CreateRegistration();
        var result = await _store.RegisterAsync(registration);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateRequestType_ShouldUpsert()
    {
        var reg1 = CreateRegistration(typeof(string));
        var reg2 = CreateRegistration(typeof(string));
        await _store.RegisterAsync(reg1);

        // Persistence stores use upsert semantics - duplicate should succeed
        var result = await _store.RegisterAsync(reg2);
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_Registered_ShouldReturnSome()
    {
        var registration = CreateRegistration(typeof(int));
        await _store.RegisterAsync(registration);

        var result = await _store.GetByRequestTypeAsync(typeof(int));
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeAsync_NotRegistered_ShouldReturnNone()
    {
        var result = await _store.GetByRequestTypeAsync(typeof(double));
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsNone.Should().BeTrue();
    }

    [Fact]
    public async Task GetByRequestTypeNameAsync_Registered_ShouldReturnSome()
    {
        var registration = CreateRegistration(typeof(long));
        await _store.RegisterAsync(registration);

        var result = await _store.GetByRequestTypeNameAsync(typeof(long).AssemblyQualifiedName!);
        result.IsRight.Should().BeTrue();
        var option = (Option<LawfulBasisRegistration>)result;
        option.IsSome.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithRegistrations_ShouldReturnAll()
    {
        await _store.RegisterAsync(CreateRegistration(typeof(byte)));
        await _store.RegisterAsync(CreateRegistration(typeof(short)));

        var result = await _store.GetAllAsync();
        result.IsRight.Should().BeTrue();
        var registrations = result.Match(
            Right: r => r,
            Left: _ => (IReadOnlyList<LawfulBasisRegistration>)[]);
        registrations.Should().HaveCountGreaterThanOrEqualTo(2);
    }
}
