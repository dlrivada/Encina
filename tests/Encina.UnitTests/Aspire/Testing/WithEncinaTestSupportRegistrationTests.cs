using Encina.Aspire.Testing;
using Encina.Testing.Fakes.Stores;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Aspire.Testing;

/// <summary>
/// Runs <see cref="DistributedApplicationTestingBuilderExtensions.WithEncinaTestSupport"/> against a
/// hand-written <see cref="FakeTestingBuilder"/> whose service collection is real.
/// </summary>
public sealed class WithEncinaTestSupportRegistrationTests
{
    [Fact]
    public void WithEncinaTestSupport_ReturnsTheSameBuilder()
    {
        using var builder = new FakeTestingBuilder();

        var result = builder.WithEncinaTestSupport();

        result.ShouldBeSameAs(builder);
    }

    [Fact]
    public void WithEncinaTestSupport_RegistersOptionsAsSingleton()
    {
        using var builder = new FakeTestingBuilder();

        builder.WithEncinaTestSupport();

        var descriptor = builder.Services.Single(sd => sd.ServiceType == typeof(EncinaTestSupportOptions));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
        using var provider = builder.Services.BuildServiceProvider();
        provider.GetRequiredService<EncinaTestSupportOptions>().ClearOutboxBeforeTest.ShouldBeTrue();
    }

    [Fact]
    public void WithEncinaTestSupport_AppliesTheConfigureActionToTheRegisteredOptions()
    {
        using var builder = new FakeTestingBuilder();

        builder.WithEncinaTestSupport(options =>
        {
            options.ClearOutboxBeforeTest = false;
            options.DefaultWaitTimeout = TimeSpan.FromSeconds(7);
        });

        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<EncinaTestSupportOptions>();
        options.ClearOutboxBeforeTest.ShouldBeFalse();
        options.DefaultWaitTimeout.ShouldBe(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void WithEncinaTestSupport_RegistersTheFakeStores()
    {
        using var builder = new FakeTestingBuilder();

        builder.WithEncinaTestSupport();

        using var provider = builder.Services.BuildServiceProvider();
        provider.GetService<FakeOutboxStore>().ShouldNotBeNull();
        provider.GetService<FakeInboxStore>().ShouldNotBeNull();
        provider.GetService<FakeSagaStore>().ShouldNotBeNull();
        provider.GetService<FakeScheduledMessageStore>().ShouldNotBeNull();
        provider.GetService<FakeDeadLetterStore>().ShouldNotBeNull();
    }

    [Fact]
    public void WithEncinaTestSupport_RegistersTheTestContextOverTheRegisteredOptionsAndStores()
    {
        using var builder = new FakeTestingBuilder();
        builder.WithEncinaTestSupport();

        using var provider = builder.Services.BuildServiceProvider();
        var context = provider.GetRequiredService<EncinaTestContext>();

        context.OutboxStore.ShouldBeSameAs(provider.GetRequiredService<FakeOutboxStore>());
        context.InboxStore.ShouldBeSameAs(provider.GetRequiredService<FakeInboxStore>());
        context.SagaStore.ShouldBeSameAs(provider.GetRequiredService<FakeSagaStore>());
        context.ScheduledMessageStore.ShouldBeSameAs(provider.GetRequiredService<FakeScheduledMessageStore>());
        context.DeadLetterStore.ShouldBeSameAs(provider.GetRequiredService<FakeDeadLetterStore>());
        context.Options.ShouldBeSameAs(provider.GetRequiredService<EncinaTestSupportOptions>());
    }

    [Fact]
    public void WithEncinaTestSupport_CalledTwice_ThrowsWithTheDocumentedMessage()
    {
        using var builder = new FakeTestingBuilder();
        builder.WithEncinaTestSupport();

        var exception = Should.Throw<InvalidOperationException>(() => builder.WithEncinaTestSupport());

        exception.Message.ShouldBe(
            "WithEncinaTestSupport may only be called once per builder. " +
            "Encina test support has already been configured.");
    }

    [Fact]
    public void WithEncinaTestSupport_CalledTwice_DoesNotInvokeTheSecondConfigureActionOrRegisterAgain()
    {
        using var builder = new FakeTestingBuilder();
        builder.WithEncinaTestSupport();
        var registrations = builder.Services.Count;
        var configureCalled = false;

        Should.Throw<InvalidOperationException>(() => builder.WithEncinaTestSupport(_ => configureCalled = true));

        configureCalled.ShouldBeFalse();
        builder.Services.Count.ShouldBe(registrations);
    }

    [Fact]
    public void WithEncinaTestSupport_OnSeparateBuilders_RegistersIndependently()
    {
        using var first = new FakeTestingBuilder();
        using var second = new FakeTestingBuilder();

        first.WithEncinaTestSupport();

        Should.NotThrow(() => second.WithEncinaTestSupport());
    }
}
