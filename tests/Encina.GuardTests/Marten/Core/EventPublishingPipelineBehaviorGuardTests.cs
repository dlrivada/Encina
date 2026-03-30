using Encina.Marten;
using Marten;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Marten.Core;

public class EventPublishingPipelineBehaviorGuardTests
{
    [Fact]
    public void Constructor_NullSession_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestRequest, string>(
                null!, Substitute.For<IEncina>(),
                NullLogger<EventPublishingPipelineBehavior<TestRequest, string>>.Instance,
                Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void Constructor_NullEncina_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestRequest, string>(
                Substitute.For<IDocumentSession>(), null!,
                NullLogger<EventPublishingPipelineBehavior<TestRequest, string>>.Instance,
                Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void Constructor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestRequest, string>(
                Substitute.For<IDocumentSession>(), Substitute.For<IEncina>(),
                null!, Options.Create(new EncinaMartenOptions())));

    [Fact]
    public void Constructor_NullOptions_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new EventPublishingPipelineBehavior<TestRequest, string>(
                Substitute.For<IDocumentSession>(), Substitute.For<IEncina>(),
                NullLogger<EventPublishingPipelineBehavior<TestRequest, string>>.Instance, null!));

    public sealed class TestRequest : ICommand<string> { }
}
