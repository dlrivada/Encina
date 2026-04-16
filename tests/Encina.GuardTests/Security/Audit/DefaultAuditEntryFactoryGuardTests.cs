using Encina.Security.Audit;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Security.Audit;

/// <summary>
/// Additional guard clause tests for <see cref="DefaultAuditEntryFactory"/>.
/// Tests the overloaded Create method with timing parameters.
/// </summary>
public class DefaultAuditEntryFactoryGuardTests
{
    private readonly DefaultAuditEntryFactory _factory;

    public DefaultAuditEntryFactoryGuardTests()
    {
        var piiMasker = Substitute.For<IPiiMasker>();
        piiMasker.MaskForAudit(Arg.Any<object>()).Returns(x => x[0]);
        var options = Options.Create(new AuditOptions());
        _factory = new DefaultAuditEntryFactory(piiMasker, options);
    }

    [Fact]
    public void Create_WithTiming_NullRequest_ThrowsArgumentNullException()
    {
        var context = RequestContext.CreateForTest();
        var now = DateTimeOffset.UtcNow;

        var act = () => _factory.Create<TestCommand, string>(
            null!, "response", context, AuditOutcome.Success, null, now, now);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("request");
    }

    [Fact]
    public void Create_WithTiming_NullContext_ThrowsArgumentNullException()
    {
        var request = new TestCommand();
        var now = DateTimeOffset.UtcNow;

        var act = () => _factory.Create<TestCommand, string>(
            request, "response", null!, AuditOutcome.Success, null, now, now);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("context");
    }

    [Fact]
    public void Create_WithTiming_ValidParameters_DoesNotThrow()
    {
        var request = new TestCommand();
        var context = RequestContext.CreateForTest();
        var now = DateTimeOffset.UtcNow;

        var act = () => _factory.Create<TestCommand, string>(
            request, "response", context, AuditOutcome.Success, null, now, now);

        Should.NotThrow(act);
    }

    [Fact]
    public void Create_Simple_ValidParameters_DoesNotThrow()
    {
        var request = new TestCommand();
        var context = RequestContext.CreateForTest();

        var act = () => _factory.Create(request, context, AuditOutcome.Success, null);

        Should.NotThrow(act);
    }

    public sealed class TestCommand : ICommand<Unit> { }
}
