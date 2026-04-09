#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Attributes;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Pipeline;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Contract tests for <see cref="TransferBlockingPipelineBehavior{TRequest, TResponse}"/>
/// verifying real pipeline instantiation and Handle behavior contracts.
/// </summary>
[Trait("Category", "Contract")]
public class TransferBlockingPipelineBehaviorContractTests
{
    private readonly ITransferValidator _validator = Substitute.For<ITransferValidator>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    #region Disabled Mode Skips Validation

    [Fact]
    public async Task Handle_DisabledMode_SkipsValidation_CallsNext()
    {
        var options = Options.Create(new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Disabled
        });

        var sut = CreateSut<TestAnnotatedRequest, TestResponse>(options);
        var expectedResponse = new TestResponse("ok");
        var nextCalled = false;

        var result = await sut.Handle(
            new TestAnnotatedRequest(),
            _context,
            () =>
            {
                nextCalled = true;
                return ValueTask.FromResult(Right<EncinaError, TestResponse>(expectedResponse));
            },
            CancellationToken.None);

        nextCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        var response = (TestResponse)result;
        response.Value.ShouldBe("ok");

        // Validator should NOT have been called
        await _validator.DidNotReceive().ValidateAsync(
            Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attribute Skips Validation

    [Fact]
    public async Task Handle_NoAttribute_SkipsValidation_CallsNext()
    {
        var options = Options.Create(new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Block
        });

        var sut = CreateSut<TestUnannotatedRequest, TestResponse>(options);
        var expectedResponse = new TestResponse("ok");

        var result = await sut.Handle(
            new TestUnannotatedRequest(),
            _context,
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(expectedResponse)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();

        // Validator should NOT have been called — no attribute
        await _validator.DidNotReceive().ValidateAsync(
            Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Valid Transfer Calls Next

    [Fact]
    public async Task Handle_ValidTransfer_CallsNext()
    {
        var options = Options.Create(new CrossBorderTransferOptions
        {
            EnforcementMode = CrossBorderTransferEnforcementMode.Block,
            DefaultSourceCountryCode = "DE"
        });

        _validator.ValidateAsync(Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, TransferValidationOutcome>(
                    TransferValidationOutcome.Allow(TransferBasis.AdequacyDecision))));

        var sut = CreateSut<TestAnnotatedRequest, TestResponse>(options);
        var expectedResponse = new TestResponse("success");

        var result = await sut.Handle(
            new TestAnnotatedRequest(),
            _context,
            () => ValueTask.FromResult(Right<EncinaError, TestResponse>(expectedResponse)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var response = (TestResponse)result;
        response.Value.ShouldBe("success");

        // Validator SHOULD have been called
        await _validator.Received(1).ValidateAsync(
            Arg.Any<TransferRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helpers

    private TransferBlockingPipelineBehavior<TRequest, TResponse> CreateSut<TRequest, TResponse>(
        IOptions<CrossBorderTransferOptions> options)
        where TRequest : IRequest<TResponse>
    {
        var logger = NullLogger<TransferBlockingPipelineBehavior<TRequest, TResponse>>.Instance;
        return new TransferBlockingPipelineBehavior<TRequest, TResponse>(
            _validator, options, logger, _serviceProvider);
    }

    #endregion

    #region Test Types

    [RequiresCrossBorderTransfer(Destination = "US", DataCategory = "personal-data")]
    public sealed record TestAnnotatedRequest : IRequest<TestResponse>;

    public sealed record TestUnannotatedRequest : IRequest<TestResponse>;

    public sealed record TestResponse(string Value);

    #endregion
}
