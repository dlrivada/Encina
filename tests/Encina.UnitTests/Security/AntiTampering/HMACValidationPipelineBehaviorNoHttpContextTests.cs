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
/// Regression coverage for issue #1155: a <c>[RequireSignature]</c> request with no
/// <see cref="HttpContext"/> available must fail closed by default instead of passing
/// through unverified. Ported from the verification spike
/// (branch <c>spike/verify-request-context</c>, commit <c>30b8a218</c>,
/// <c>HMACValidationPipelineBehaviorNoHttpContextSpikeTests</c>) once the fail-closed
/// fix landed in <see cref="HMACValidationPipelineBehavior{TRequest,TResponse}"/>.
/// </summary>
public sealed class HMACValidationPipelineBehaviorNoHttpContextTests
{
    /// <summary>
    /// A <c>[RequireSignature]</c> request with no <see cref="HttpContext"/> available and no
    /// opt-out configured is rejected instead of passing straight through to the handler.
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
        result.IsLeft.ShouldBeTrue();
        nextCalled.ShouldBeFalse();
        var error = (EncinaError)result;
        error.GetCode().IfNone("").ShouldBe(AntiTamperingErrors.NoHttpContextCode);
    }

    [RequireSignature]
    public sealed record SpikeSignedCommand : ICommand;
}
