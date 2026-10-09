using Encina.Messaging.DeadLetter;
using Encina.Messaging.Health;
using Encina.Testing.Fakes.Stores;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Encina.GuardTests.Messaging.DeadLetter;

/// <summary>
/// Guard clause tests for <see cref="DeadLetterServiceCollectionExtensions"/>.
/// </summary>
public sealed class DeadLetterServiceCollectionExtensionsGuardTests
{
    private sealed class StubFactory : IDeadLetterMessageFactory
    {
        public IDeadLetterMessage Create(DeadLetterData data) => Substitute.For<IDeadLetterMessage>();
    }

    [Fact]
    public void AddEncinaDeadLetterQueue_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaDeadLetterQueue_WithHealthCheckOptions_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaDeadLetterQueue<FakeDeadLetterStore, StubFactory>(
            null, new DeadLetterHealthCheckOptions());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }
}
