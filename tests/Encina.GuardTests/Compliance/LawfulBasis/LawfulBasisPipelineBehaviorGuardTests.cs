using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using LanguageExt;

using static LanguageExt.Prelude;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

#pragma warning disable CA1034 // Nested test types

namespace Encina.GuardTests.Compliance.LawfulBasis;

/// <summary>
/// Guard tests for <see cref="LawfulBasisValidationPipelineBehavior{TRequest, TResponse}"/>
/// Handle method argument validation.
/// </summary>
public class LawfulBasisPipelineBehaviorGuardTests
{
    public sealed record GuardRequest : IRequest<string>;

    private static LawfulBasisValidationPipelineBehavior<GuardRequest, string> CreateBehavior()
    {
        var service = Substitute.For<ILawfulBasisService>();
        var extractor = Substitute.For<ILawfulBasisSubjectIdExtractor>();
        var options = Options.Create(new LawfulBasisOptions());
        return new LawfulBasisValidationPipelineBehavior<GuardRequest, string>(
            service,
            extractor,
            options,
            NullLogger<LawfulBasisValidationPipelineBehavior<GuardRequest, string>>.Instance);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("ok"));

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await behavior.Handle(null!, context, next, CancellationToken.None));
    }
}
