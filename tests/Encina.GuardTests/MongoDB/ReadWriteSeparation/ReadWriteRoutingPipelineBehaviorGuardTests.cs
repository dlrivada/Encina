using Encina.MongoDB.ReadWriteSeparation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.MongoDB.ReadWriteSeparation;

public class ReadWriteRoutingPipelineBehaviorGuardTests
{
    #region Constructor

    [Fact]
    public void Ctor_NullLogger_Throws()
        => Should.Throw<ArgumentNullException>(() =>
            new ReadWriteRoutingPipelineBehavior<TestCommand, string>(null!));

    #endregion

    #region Handle method guards

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>();
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(null!, Substitute.For<IRequestContext>(), () => throw new NotImplementedException(), default));
    }

    [Fact]
    public async Task Handle_NullContext_Throws()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>();
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(new TestCommand(), null!, () => throw new NotImplementedException(), default));
    }

    [Fact]
    public async Task Handle_NullNextStep_Throws()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ReadWriteRoutingPipelineBehavior<TestCommand, string>>();
        var behavior = new ReadWriteRoutingPipelineBehavior<TestCommand, string>(logger);
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await behavior.Handle(new TestCommand(), Substitute.For<IRequestContext>(), null!, default));
    }

    #endregion

    public sealed record TestCommand : ICommand<string>;
}
