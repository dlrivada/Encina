using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

using static LanguageExt.Prelude;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

#pragma warning disable CA1034 // Nested types for test requests
#pragma warning disable CA2012 // ValueTask

namespace Encina.ContractTests.Compliance.LawfulBasis;

/// <summary>
/// Contract tests for <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>
/// that execute the REAL behavior code (not reflection) to verify core contract invariants.
/// </summary>
[Trait("Category", "Contract")]
public class LawfulBasisValidationPipelineBehaviorContractTests
{
    public sealed record PlainRequest : IRequest<string>;

    [LawfulBasis(GDPRLawfulBasis.Contract, Purpose = "Test")]
    public sealed record AnnotatedRequest : IRequest<string>;

    private static LawfulBasisValidationPipelineBehavior<TRequest, string> CreateBehavior<TRequest>(
        ILawfulBasisService service,
        LawfulBasisOptions options)
        where TRequest : IRequest<string>
    {
        var extractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        return new LawfulBasisValidationPipelineBehavior<TRequest, string>(
            service,
            extractor,
            Options.Create(options),
            NullLogger<LawfulBasisValidationPipelineBehavior<TRequest, string>>.Instance);
    }

    private static RequestHandlerCallback<string> SuccessCallback(string value = "handled") =>
        () => new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>(value));

    private static string? GetRight(Either<EncinaError, string> either)
    {
        return either.Match(Right: v => v, Left: _ => (string?)null);
    }

    [Fact]
    public async Task Handle_DisabledMode_ShouldSkipValidationAndCallNext()
    {
        // Arrange
        var service = Substitute.For<ILawfulBasisService>();
        var options = new LawfulBasisOptions
        {
            EnforcementMode = LawfulBasisEnforcementMode.Disabled
        };
        var behavior = CreateBehavior<AnnotatedRequest>(service, options);
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("handled"));
        };

        // Act
        var result = await behavior.Handle(
            new AnnotatedRequest(),
            Substitute.For<IRequestContext>(),
            next,
            CancellationToken.None);

        // Assert: disabled mode should call next, return success, not touch the service at all
        nextCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("handled");
        await service.DidNotReceiveWithAnyArgs().GetRegistrationByRequestTypeAsync(default!, default);
        await service.DidNotReceiveWithAnyArgs().HasApprovedLIAAsync(default!, default);
    }

    [Fact]
    public async Task Handle_NoGdprAttributes_ShouldSkipAndCallNext()
    {
        // Arrange
        var service = Substitute.For<ILawfulBasisService>();
        var options = new LawfulBasisOptions();
        var behavior = CreateBehavior<PlainRequest>(service, options);

        // Act
        var result = await behavior.Handle(
            new PlainRequest(),
            Substitute.For<IRequestContext>(),
            SuccessCallback(),
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("handled");
        await service.DidNotReceiveWithAnyArgs().GetRegistrationByRequestTypeAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ValidContractBasis_ShouldCallNextAndReturnResult()
    {
        // Arrange
        var service = Substitute.For<ILawfulBasisService>();
        var options = new LawfulBasisOptions
        {
            EnforcementMode = LawfulBasisEnforcementMode.Block
        };
        var behavior = CreateBehavior<AnnotatedRequest>(service, options);

        // Act
        var result = await behavior.Handle(
            new AnnotatedRequest(),
            Substitute.For<IRequestContext>(),
            SuccessCallback(),
            CancellationToken.None);

        // Assert: Contract basis requires no extra validation - should call next
        result.IsRight.ShouldBeTrue();
        GetRight(result).ShouldBe("handled");
    }
}
