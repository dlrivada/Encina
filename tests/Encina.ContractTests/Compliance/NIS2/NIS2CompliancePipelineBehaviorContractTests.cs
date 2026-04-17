#pragma warning disable CA1859 // Contract tests intentionally use interface types to verify contracts

using Encina.Compliance.NIS2;
using Encina.Compliance.NIS2.Abstractions;
using Encina.Compliance.NIS2.Model;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.NIS2;

/// <summary>
/// Contract tests verifying that <see cref="NIS2CompliancePipelineBehavior{TRequest, TResponse}"/>
/// correctly implements the pipeline behavior contract under different enforcement modes and
/// attribute configurations.
/// </summary>
public sealed class NIS2CompliancePipelineBehaviorContractTests
{
    private static NIS2CompliancePipelineBehavior<TestCommand, string> CreateBehavior(
        NIS2Options? options = null,
        IMFAEnforcer? mfaEnforcer = null,
        ISupplyChainSecurityValidator? supplyChainValidator = null,
        IServiceProvider? serviceProvider = null)
    {
        var opts = options ?? new NIS2Options();
        var mfa = mfaEnforcer ?? CreatePassThroughMFAEnforcer();
        var chain = supplyChainValidator ?? CreatePassThroughSupplyChainValidator();
        var sp = serviceProvider ?? new ServiceCollection().BuildServiceProvider();

        return new NIS2CompliancePipelineBehavior<TestCommand, string>(
            mfa,
            chain,
            Options.Create(opts),
            sp,
            NullLogger<NIS2CompliancePipelineBehavior<TestCommand, string>>.Instance);
    }

    private static IMFAEnforcer CreatePassThroughMFAEnforcer()
    {
        var enforcer = Substitute.For<IMFAEnforcer>();
        enforcer.RequireMFAAsync(Arg.Any<TestCommand>(), Arg.Any<IRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        return enforcer;
    }

    private static ISupplyChainSecurityValidator CreatePassThroughSupplyChainValidator()
    {
        var validator = Substitute.For<ISupplyChainSecurityValidator>();
        validator.ValidateSupplierForOperationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        return validator;
    }

    private static IRequestContext CreateRequestContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");
        return context;
    }

    /// <summary>
    /// Contract: When enforcement mode is Disabled, the behavior skips all checks and calls next.
    /// </summary>
    [Fact]
    public async Task DisabledMode_SkipsValidation_CallsNext()
    {
        // Arrange
        var options = new NIS2Options { EnforcementMode = NIS2EnforcementMode.Disabled };
        var behavior = CreateBehavior(options);
        var request = new TestCommand();
        var context = CreateRequestContext();
        var nextCalled = false;

        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue("the handler should be called when enforcement is disabled");
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("success"));
    }

    /// <summary>
    /// Contract: When request has no NIS2 attributes, the behavior skips checks and calls next.
    /// TestCommand has no [NIS2Critical], [RequireMFA], or [NIS2SupplyChainCheck] attributes.
    /// </summary>
    [Fact]
    public async Task NoAttribute_SkipsValidation_CallsNext()
    {
        // Arrange
        var options = new NIS2Options { EnforcementMode = NIS2EnforcementMode.Block };
        var behavior = CreateBehavior(options);
        var request = new TestCommand();
        var context = CreateRequestContext();
        var nextCalled = false;

        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue("the handler should be called when request has no NIS2 attributes");
        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: When all compliance checks pass, the behavior calls next and returns its result.
    /// Uses MFATestCommand which has [RequireMFA], with a pass-through MFA enforcer.
    /// </summary>
    [Fact]
    public async Task Compliant_CallsNext()
    {
        // Arrange
        var options = new NIS2Options
        {
            EnforcementMode = NIS2EnforcementMode.Block,
            EnforceMFA = true
        };

        var mfaEnforcer = CreatePassThroughMFAEnforcer();

        // Need to create the behavior for the MFA-decorated type
        var behavior = new NIS2CompliancePipelineBehavior<MFATestCommand, string>(
            mfaEnforcer,
            CreatePassThroughSupplyChainValidator(),
            Options.Create(options),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<NIS2CompliancePipelineBehavior<MFATestCommand, string>>.Instance);

        var request = new MFATestCommand();
        var context = CreateRequestContext();
        var nextCalled = false;

        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("compliant-result"));
        };

        // Act
        var result = await behavior.Handle(request, context, next, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue("the handler should be called when all checks pass");
        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("compliant-result"));
    }

    /// <summary>
    /// A plain request with no NIS2 attributes.
    /// </summary>
    public sealed record TestCommand : IRequest<string>;

    /// <summary>
    /// A request decorated with [RequireMFA] for testing MFA enforcement.
    /// </summary>
    [RequireMFA(Reason = "Test MFA enforcement")]
    public sealed record MFATestCommand : IRequest<string>;
}
