using Encina.Testing.Pact;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Testing.Pact;

/// <summary>Sample command used only to call the generic Pact members with valid type arguments.</summary>
public sealed record PactGuardCommand(Guid Id) : ICommand<PactGuardDto>;

/// <summary>Sample query used only to call the generic Pact members with valid type arguments.</summary>
public sealed record PactGuardQuery(Guid Id) : IQuery<PactGuardDto>;

/// <summary>Sample notification used only to call the generic Pact members with valid type arguments.</summary>
public sealed record PactGuardNotification(Guid Id) : INotification;

/// <summary>Sample response used only to call the generic Pact members with valid type arguments.</summary>
public sealed record PactGuardDto(Guid Id);

/// <summary>
/// Argument guards of the remaining public members of <see cref="EncinaPactConsumerBuilder"/>,
/// <see cref="EncinaPactProviderVerifier"/>, <see cref="EncinaPactFixture"/> and <see cref="PactExtensions"/>.
/// Each test calls the real member with one invalid argument and valid values for the rest.
/// </summary>
public sealed class PactArgumentGuardTests : IDisposable
{
    private static readonly Guid Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private readonly string _pactDirectory = Path.Combine(Path.GetTempPath(), $"pact-guard-{Guid.NewGuid():N}");
    private readonly EncinaPactConsumerBuilder _builder;
    private readonly EncinaPactProviderVerifier _verifier;
    private readonly EncinaPactFixture _fixture;

    public PactArgumentGuardTests()
    {
        _builder = new EncinaPactConsumerBuilder("WebApp", "OrdersApi", _pactDirectory);
        _verifier = new EncinaPactProviderVerifier(Substitute.For<IEncina>(), new ServiceCollection().BuildServiceProvider());
        _fixture = new EncinaPactFixture { PactDirectory = _pactDirectory };
    }

    public void Dispose()
    {
        _builder.Dispose();
        _verifier.Dispose();
        _fixture.Dispose();
        if (Directory.Exists(_pactDirectory))
        {
            Directory.Delete(_pactDirectory, recursive: true);
        }
    }

    // EncinaPactConsumerBuilder

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ConsumerBuilder_Constructor_InvalidPactDirectory_Throws(string? pactDirectory)
    {
        Should.Throw<ArgumentException>(() => new EncinaPactConsumerBuilder("WebApp", "OrdersApi", pactDirectory!));
    }

    [Fact]
    public void ConsumerBuilder_WithCommandExpectation_NullCommand_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _builder.WithCommandExpectation<PactGuardCommand, PactGuardDto>(
                null!, Either<EncinaError, PactGuardDto>.Right(new PactGuardDto(Id))));
    }

    [Fact]
    public void ConsumerBuilder_WithQueryExpectation_NullQuery_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _builder.WithQueryExpectation<PactGuardQuery, PactGuardDto>(
                null!, Either<EncinaError, PactGuardDto>.Right(new PactGuardDto(Id))));
    }

    [Fact]
    public void ConsumerBuilder_WithNotificationExpectation_NullNotification_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _builder.WithNotificationExpectation<PactGuardNotification>(null!));
    }

    [Fact]
    public void ConsumerBuilder_WithCommandFailureExpectation_NullCommand_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _builder.WithCommandFailureExpectation<PactGuardCommand, PactGuardDto>(
                null!, EncinaErrors.Create("encina.validation.invalid", "invalid")));
    }

    [Fact]
    public void ConsumerBuilder_WithQueryFailureExpectation_NullQuery_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _builder.WithQueryFailureExpectation<PactGuardQuery, PactGuardDto>(
                null!, EncinaErrors.Create("encina.validation.invalid", "invalid")));
    }

    [Fact]
    public async Task ConsumerBuilder_VerifyAsync_NullTestAction_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _builder.VerifyAsync(null!));
    }

    [Fact]
    public void ConsumerBuilder_Verify_NullTestAction_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _builder.Verify(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ConsumerBuilder_Constructor_InvalidConsumerName_Throws(string? consumerName)
    {
        Should.Throw<ArgumentException>(() => new EncinaPactConsumerBuilder(consumerName!, "OrdersApi", _pactDirectory));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ConsumerBuilder_Constructor_InvalidProviderName_Throws(string? providerName)
    {
        Should.Throw<ArgumentException>(() => new EncinaPactConsumerBuilder("WebApp", providerName!, _pactDirectory));
    }

    // EncinaPactProviderVerifier

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Verifier_WithProviderState_AsyncAction_InvalidName_Throws(string? stateName)
    {
        Should.Throw<ArgumentException>(() => _verifier.WithProviderState(stateName!, () => Task.CompletedTask));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Verifier_WithProviderState_ParameterAction_InvalidName_Throws(string? stateName)
    {
        Should.Throw<ArgumentException>(() =>
            _verifier.WithProviderState(stateName!, (IDictionary<string, object> _) => Task.CompletedTask));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Verifier_WithProviderState_SyncAction_InvalidName_Throws(string? stateName)
    {
        Should.Throw<ArgumentException>(() => _verifier.WithProviderState(stateName!, () => { }));
    }

    [Fact]
    public void Verifier_WithProviderState_NullAsyncAction_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _verifier.WithProviderState("state", (Func<Task>)null!));
    }

    [Fact]
    public void Verifier_WithProviderState_NullParameterAction_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            _verifier.WithProviderState("state", (Func<IDictionary<string, object>, Task>)null!));
    }

    [Fact]
    public void Verifier_WithProviderState_NullSyncAction_Throws()
    {
        Should.Throw<ArgumentNullException>(() => _verifier.WithProviderState("state", (Action)null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Verifier_VerifyAsync_InvalidPath_Throws(string? path)
    {
        await Should.ThrowAsync<ArgumentException>(() => _verifier.VerifyAsync(path!));
    }


    // EncinaPactFixture

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Fixture_CreateVerifier_InvalidProviderName_Throws(string? providerName)
    {
        Should.Throw<ArgumentException>(() => _fixture.CreateVerifier(providerName!));
    }

    [Fact]
    public async Task Fixture_VerifyAsync_AsyncAction_NullConsumer_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _fixture.VerifyAsync(null!, _ => Task.CompletedTask));
    }

    [Fact]
    public async Task Fixture_VerifyAsync_AsyncAction_NullTestAction_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _fixture.VerifyAsync(_builder, (Func<Uri, Task>)null!));
    }

    [Fact]
    public async Task Fixture_VerifyAsync_SyncAction_NullConsumer_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _fixture.VerifyAsync(null!, _ => { }));
    }

    [Fact]
    public async Task Fixture_VerifyAsync_SyncAction_NullTestAction_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() => _fixture.VerifyAsync(_builder, (Action<Uri>)null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Fixture_VerifyProviderAsync_InvalidProviderName_Throws(string? providerName)
    {
        await Should.ThrowAsync<ArgumentException>(() => _fixture.VerifyProviderAsync(providerName!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Fixture_CreateConsumer_InvalidConsumerName_Throws(string? consumerName)
    {
        Should.Throw<ArgumentException>(() => _fixture.CreateConsumer(consumerName!, "OrdersApi"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Fixture_CreateConsumer_InvalidProviderName_Throws(string? providerName)
    {
        Should.Throw<ArgumentException>(() => _fixture.CreateConsumer("WebApp", providerName!));
    }

    [Fact]
    public async Task Fixture_ConfiguredWithServices_InvalidProviderNames_Throw()
    {
        _fixture.WithServices(services => services.AddEncina());

        Should.Throw<ArgumentException>(() => _fixture.CreateVerifier(" "));
        await Should.ThrowAsync<ArgumentException>(() => _fixture.VerifyProviderAsync(""));
    }

    [Fact]
    public void Fixture_ConfiguredWithEncina_NullTestActionStillThrows()
    {
        _fixture.WithEncina(Substitute.For<IEncina>(), new ServiceCollection().BuildServiceProvider());
        _fixture.CreateConsumer("WebApp", "OrdersApi");

        Should.Throw<ArgumentException>(() => _fixture.CreateVerifier(null!));
    }

    [Fact]
    public void Verifier_ValidRegistrationsFollowedByInvalidOnes_Throw()
    {
        _verifier
            .WithProviderName("OrdersApi")
            .WithStrictProviderStates()
            .OnMissingProviderState(_ => { })
            .WithProviderState("async", () => Task.CompletedTask)
            .WithProviderState("params", (IDictionary<string, object> _) => Task.CompletedTask)
            .WithProviderState("sync", () => { });

        Should.Throw<ArgumentException>(() => _verifier.WithProviderState(" ", () => { }));
        Should.Throw<ArgumentNullException>(() => _verifier.WithProviderState("other", (Action)null!));
    }

    // PactExtensions

    [Fact]
    public async Task Extensions_SendCommandAsync_NullClient_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            PactExtensions.SendCommandAsync<PactGuardCommand, PactGuardDto>(null!, new PactGuardCommand(Id)));
    }

    [Fact]
    public async Task Extensions_SendQueryAsync_NullQuery_Throws()
    {
        using var client = new HttpClient();

        await Should.ThrowAsync<ArgumentNullException>(() =>
            client.SendQueryAsync<PactGuardQuery, PactGuardDto>(null!));
    }

    [Fact]
    public async Task Extensions_PublishNotificationAsync_NullClient_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            PactExtensions.PublishNotificationAsync<PactGuardNotification>(null!, new PactGuardNotification(Id)));
    }

    [Fact]
    public async Task Extensions_ReadAsEitherAsync_NullResponse_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(() =>
            PactExtensions.ReadAsEitherAsync<PactGuardDto>(null!));
    }
}
