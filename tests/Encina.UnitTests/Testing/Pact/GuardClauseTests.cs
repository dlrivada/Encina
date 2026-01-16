using Encina.Testing.Pact;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

namespace Encina.UnitTests.Testing.Pact;

/// <summary>
/// Guard clause tests for null and invalid argument validation.
/// </summary>
public sealed class GuardClauseTests
{
    #region EncinaPactConsumerBuilder Guard Clauses

    [Fact]
    public void ConsumerBuilder_Constructor_NullConsumerName_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder(null!, "Provider"));
        ex.ParamName.ShouldBe("consumerName");
    }

    [Fact]
    public void ConsumerBuilder_Constructor_EmptyConsumerName_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("", "Provider"));
        ex.ParamName.ShouldBe("consumerName");
    }

    [Fact]
    public void ConsumerBuilder_Constructor_WhitespaceConsumerName_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("   ", "Provider"));
        ex.ParamName.ShouldBe("consumerName");
    }

    [Fact]
    public void ConsumerBuilder_Constructor_NullProviderName_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("Consumer", null!));
        ex.ParamName.ShouldBe("providerName");
    }

    [Fact]
    public void ConsumerBuilder_Constructor_EmptyProviderName_Throws()
    {
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("Consumer", ""));
        ex.ParamName.ShouldBe("providerName");
    }

    [Fact]
    public void ConsumerBuilder_WithCommandExpectation_NullCommand_Throws()
    {
        using var builder = new EncinaPactConsumerBuilder("Consumer", "Provider");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto());

        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.WithCommandExpectation<TestCreateOrderCommand, TestOrderDto>(null!, response));
        ex.ParamName.ShouldBe("command");
    }

    [Fact]
    public void ConsumerBuilder_WithQueryExpectation_NullQuery_Throws()
    {
        using var builder = new EncinaPactConsumerBuilder("Consumer", "Provider");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto());

        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.WithQueryExpectation<TestGetOrderByIdQuery, TestOrderDto>(null!, response));
        ex.ParamName.ShouldBe("query");
    }

    [Fact]
    public void ConsumerBuilder_WithNotificationExpectation_NullNotification_Throws()
    {
        using var builder = new EncinaPactConsumerBuilder("Consumer", "Provider");

        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.WithNotificationExpectation<TestOrderCreatedNotification>(null!));
        ex.ParamName.ShouldBe("notification");
    }

    [Fact]
    public void ConsumerBuilder_WithCommandFailureExpectation_NullCommand_Throws()
    {
        using var builder = new EncinaPactConsumerBuilder("Consumer", "Provider");
        var error = EncinaErrors.Create("test", "error");

        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.WithCommandFailureExpectation<TestCreateOrderCommand, TestOrderDto>(null!, error));
        ex.ParamName.ShouldBe("command");
    }

    [Fact]
    public void ConsumerBuilder_WithQueryFailureExpectation_NullQuery_Throws()
    {
        using var builder = new EncinaPactConsumerBuilder("Consumer", "Provider");
        var error = EncinaErrors.Create("test", "error");

        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.WithQueryFailureExpectation<TestGetOrderByIdQuery, TestOrderDto>(null!, error));
        ex.ParamName.ShouldBe("query");
    }

    #endregion

    #region EncinaPactProviderVerifier Guard Clauses

    [Fact]
    public void ProviderVerifier_Constructor_NullEncina_Throws()
    {
        using var services = new ServiceCollection().BuildServiceProvider();

        var ex = Should.Throw<ArgumentNullException>(() =>
            new EncinaPactProviderVerifier(null!, services));
        ex.ParamName.ShouldBe("encina");
    }

    [Fact]
    public void ProviderVerifier_WithProviderName_NullName_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var verifier = new EncinaPactProviderVerifier(encina);

        var ex = Should.Throw<ArgumentException>(() => verifier.WithProviderName(null!));
        ex.ParamName.ShouldBe("providerName");
    }

    [Fact]
    public void ProviderVerifier_WithProviderState_NullStateName_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var verifier = new EncinaPactProviderVerifier(encina);

        var ex = Should.Throw<ArgumentException>(() =>
            verifier.WithProviderState(null!, () => Task.CompletedTask));
        ex.ParamName.ShouldBe("stateName");
    }

    [Fact]
    public void ProviderVerifier_WithProviderState_NullAction_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var verifier = new EncinaPactProviderVerifier(encina);

        var ex = Should.Throw<ArgumentNullException>(() =>
            verifier.WithProviderState("state", (Func<Task>)null!));
        ex.ParamName.ShouldBe("setupAction");
    }

    [Fact]
    public void ProviderVerifier_WithProviderStateSync_NullAction_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var verifier = new EncinaPactProviderVerifier(encina);

        var ex = Should.Throw<ArgumentNullException>(() =>
            verifier.WithProviderState("state", (Action)null!));
        ex.ParamName.ShouldBe("setupAction");
    }

    [Fact]
    public async Task ProviderVerifier_VerifyAsync_NullPath_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var verifier = new EncinaPactProviderVerifier(encina);

        var ex = await Should.ThrowAsync<ArgumentException>(() => verifier.VerifyAsync(null!));
        ex.ParamName.ShouldBe("pactFilePath");
    }

    [Fact]
    public async Task ProviderVerifier_VerifyAsync_EmptyPath_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var verifier = new EncinaPactProviderVerifier(encina);

        var ex = await Should.ThrowAsync<ArgumentException>(() => verifier.VerifyAsync(""));
        ex.ParamName.ShouldBe("pactFilePath");
    }

    #endregion

    #region EncinaPactFixture Guard Clauses

    [Fact]
    public void PactFixture_CreateConsumer_NullConsumerName_Throws()
    {
        using var fixture = new EncinaPactFixture();

        var ex = Should.Throw<ArgumentException>(() => fixture.CreateConsumer(null!, "Provider"));
        ex.ParamName.ShouldBe("consumerName");
    }

    [Fact]
    public void PactFixture_CreateConsumer_NullProviderName_Throws()
    {
        using var fixture = new EncinaPactFixture();

        var ex = Should.Throw<ArgumentException>(() => fixture.CreateConsumer("Consumer", null!));
        ex.ParamName.ShouldBe("providerName");
    }

    [Fact]
    public void PactFixture_CreateVerifier_NullProviderName_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var services = new ServiceCollection().BuildServiceProvider();
        using var fixture = new EncinaPactFixture();
        fixture.WithEncina(encina, services);

        var ex = Should.Throw<ArgumentException>(() => fixture.CreateVerifier(null!));
        ex.ParamName.ShouldBe("providerName");
    }

    [Fact]
    public void PactFixture_WithEncina_NullEncina_Throws()
    {
        using var fixture = new EncinaPactFixture();
        using var services = new ServiceCollection().BuildServiceProvider();

        var ex = Should.Throw<ArgumentNullException>(() => fixture.WithEncina(null!, services));
        ex.ParamName.ShouldBe("encina");
    }

    [Fact]
    public void PactFixture_WithEncina_NullServiceProvider_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var fixture = new EncinaPactFixture();

        var ex = Should.Throw<ArgumentNullException>(() => fixture.WithEncina(encina, null!));
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void PactFixture_WithServices_NullAction_Throws()
    {
        using var fixture = new EncinaPactFixture();

        var ex = Should.Throw<ArgumentNullException>(() => fixture.WithServices(null!));
        ex.ParamName.ShouldBe("configureServices");
    }

    [Fact]
    public async Task PactFixture_VerifyAsync_NullConsumer_Throws()
    {
        using var fixture = new EncinaPactFixture();

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            fixture.VerifyAsync(null!, async (Uri _) => await Task.CompletedTask));
        ex.ParamName.ShouldBe("consumer");
    }

    [Fact]
    public async Task PactFixture_VerifyAsync_NullAsyncAction_Throws()
    {
        using var fixture = new EncinaPactFixture();
        var consumer = fixture.CreateConsumer("Consumer", "Provider");

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            fixture.VerifyAsync(consumer, (Func<Uri, Task>)null!));
        ex.ParamName.ShouldBe("testAction");
    }

    [Fact]
    public async Task PactFixture_VerifyAsync_NullSyncAction_Throws()
    {
        using var fixture = new EncinaPactFixture();
        var consumer = fixture.CreateConsumer("Consumer", "Provider");

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            fixture.VerifyAsync(consumer, (Action<Uri>)null!));
        ex.ParamName.ShouldBe("testAction");
    }

    [Fact]
    public async Task PactFixture_VerifyProviderAsync_NullProviderName_Throws()
    {
        var encina = Substitute.For<IEncina>();
        using var services = new ServiceCollection().BuildServiceProvider();
        using var fixture = new EncinaPactFixture();
        fixture.WithEncina(encina, services);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            fixture.VerifyProviderAsync(null!));
        ex.ParamName.ShouldBe("providerName");
    }

    #endregion

    #region PactExtensions Guard Clauses

    [Fact]
    public void CreatePactHttpClient_NullUri_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() => PactExtensions.CreatePactHttpClient(null!));
        ex.ParamName.ShouldBe("mockServerUri");
    }

    #endregion
}
