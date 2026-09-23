using Encina.Security.AntiTampering;
using Encina.Security.AntiTampering.Abstractions;
using Encina.Security.AntiTampering.Pipeline;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Security.AntiTampering;

/// <summary>
/// Spike reproduction for finding A7 (verification spike, branch spike/verify-request-context):
/// <see cref="HMACValidationPipelineBehavior{TRequest,TResponse}.Handle"/>
/// (src/Encina.Security.AntiTampering/Pipeline/HMACValidationPipelineBehavior.cs, ~lines 124-130)
/// skips ALL signature/timestamp/nonce validation and calls straight through to <c>nextStep()</c>
/// whenever <see cref="IHttpContextAccessor.HttpContext"/> is <c>null</c>, even for a request type
/// decorated with <see cref="RequireSignatureAttribute"/>. This is a documented, intentional design
/// choice - the type's own remarks (same file, ~lines 32-36) state it is meant "to allow the same
/// request types to be used in background jobs or tests" - and it is already covered by a passing
/// test, <c>HMACValidationPipelineBehaviorTests.Handle_NoHttpContext_PassesThrough</c>, which asserts
/// the pass-through as the expected outcome. That said, it is a fail-open behavior for a security
/// feature the caller explicitly opted into via <c>[RequireSignature]</c>: a background job, a
/// message-bus consumer, or any non-HTTP host that sends the same <c>[RequireSignature]</c> request
/// gets zero integrity/replay protection, silently. This test reproduces the gap against a fail-closed
/// expectation (reject when the attribute demands a signature but no HttpContext is available to
/// supply one) to make the trade-off visible; it fails against the current, intentional implementation.
/// </summary>
public sealed class HMACValidationPipelineBehaviorNoHttpContextSpikeTests
{
    /// <summary>
    /// CONFIRMED, DOCUMENTED/INTENTIONAL (A7): a <c>[RequireSignature]</c> request with no
    /// <see cref="HttpContext"/> available passes straight through to the handler instead of being
    /// rejected. The behavior matches the type's own XML doc remarks and an existing passing test
    /// (<c>Handle_NoHttpContext_PassesThrough</c>), so this is not a hidden defect - it is a
    /// fail-open-by-design choice that this test documents against a fail-closed expectation.
    /// </summary>
    [Fact]
    public async Task Handle_RequireSignatureAttribute_NoHttpContext_ShouldRejectInsteadOfPassingThrough()
    {
        // Arrange
        var requestSigner = Substitute.For<IRequestSigner>();
        var nonceStore = Substitute.For<INonceStore>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var options = Options.Create(new AntiTamperingOptions());
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<HMACValidationPipelineBehavior<SpikeSignedCommand, Unit>>>();
        var context = RequestContext.CreateForTest(userId: "user-1");

        var sut = new HMACValidationPipelineBehavior<SpikeSignedCommand, Unit>(
            requestSigner, nonceStore, httpContextAccessor, options, timeProvider, logger);

        var nextCalled = false;
        RequestHandlerCallback<Unit> nextStep = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, Unit>>(Right(Unit.Default));
        };

        // Act
        var result = await sut.Handle(new SpikeSignedCommand(), context, nextStep, CancellationToken.None);

        // Assert
        // Expected (fail-closed) behavior: a request that requires a signature but arrives with no
        // HttpContext to extract headers from cannot be verified, so it should be rejected rather
        // than silently allowed through.
        // Actual (current, documented) behavior: the behavior passes the request straight to the
        // handler, exactly as HMACValidationPipelineBehaviorTests.Handle_NoHttpContext_PassesThrough
        // already asserts.
        result.IsLeft.ShouldBeTrue();
        nextCalled.ShouldBeFalse();
    }

    [RequireSignature]
    public sealed record SpikeSignedCommand : ICommand;
}
