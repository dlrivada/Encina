using Encina.Compliance.LawfulBasis;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.ReadModels;
using Encina.Compliance.LawfulBasis.Services;
using LanguageExt;

using static LanguageExt.Prelude;

using GDPRLawfulBasis = global::Encina.Compliance.GDPR.LawfulBasis;

#pragma warning disable CA2012 // ValueTask usage

namespace Encina.UnitTests.Compliance.LawfulBasisModule.Services;

/// <summary>
/// Unit tests for <see cref="DefaultLawfulBasisProvider"/>.
/// </summary>
public class DefaultLawfulBasisProviderTests
{
    private readonly ILawfulBasisService _service = Substitute.For<ILawfulBasisService>();
    private readonly DefaultLawfulBasisProvider _sut;

    public DefaultLawfulBasisProviderTests()
    {
        _sut = new DefaultLawfulBasisProvider(_service);
    }

    public sealed record SampleRequest;

    [Fact]
    public void Constructor_NullService_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new DefaultLawfulBasisProvider(null!));
    }

    [Fact]
    public async Task GetBasisForRequestAsync_NullRequestType_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(
            async () => await _sut.GetBasisForRequestAsync(null!));
    }

    [Fact]
    public async Task GetBasisForRequestAsync_ValidType_CallsService()
    {
        var readModel = new LawfulBasisReadModel
        {
            Id = Guid.NewGuid(),
            Basis = GDPRLawfulBasis.Contract,
            RequestTypeName = typeof(SampleRequest).AssemblyQualifiedName!
        };
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Optional(readModel))));

        var result = await _sut.GetBasisForRequestAsync(typeof(SampleRequest));

        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task GetBasisForRequestAsync_ServiceReturnsError_ReturnsError()
    {
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Left<EncinaError, Option<LawfulBasisReadModel>>(EncinaErrors.Create("err", "failed"))));

        var result = await _sut.GetBasisForRequestAsync(typeof(SampleRequest));

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBasisAsync_NotFound_ReturnsInvalid()
    {
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Option<LawfulBasisReadModel>.None)));

        var result = await _sut.ValidateBasisAsync<SampleRequest>();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.IsValid.ShouldBeFalse();
                r.Errors.ShouldNotBeEmpty();
                return true;
            },
            Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBasisAsync_ContractBasisNoReference_HasWarning()
    {
        var readModel = new LawfulBasisReadModel
        {
            Id = Guid.NewGuid(),
            Basis = GDPRLawfulBasis.Contract,
            ContractReference = null,
            RequestTypeName = typeof(SampleRequest).AssemblyQualifiedName!
        };
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Optional(readModel))));

        var result = await _sut.ValidateBasisAsync<SampleRequest>();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.IsValid.ShouldBeTrue();
                r.Warnings.ShouldNotBeEmpty();
                r.Warnings[0].ShouldContain("Contract");
                return true;
            },
            Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBasisAsync_LegitimateInterestsNoReference_HasWarning()
    {
        var readModel = new LawfulBasisReadModel
        {
            Id = Guid.NewGuid(),
            Basis = GDPRLawfulBasis.LegitimateInterests,
            LIAReference = null,
            RequestTypeName = typeof(SampleRequest).AssemblyQualifiedName!
        };
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Optional(readModel))));

        var result = await _sut.ValidateBasisAsync<SampleRequest>();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.IsValid.ShouldBeTrue();
                r.Warnings.ShouldNotBeEmpty();
                r.Warnings[0].ShouldContain("LIA");
                return true;
            },
            Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBasisAsync_LegalObligationNoReference_HasWarning()
    {
        var readModel = new LawfulBasisReadModel
        {
            Id = Guid.NewGuid(),
            Basis = GDPRLawfulBasis.LegalObligation,
            LegalReference = null,
            RequestTypeName = typeof(SampleRequest).AssemblyQualifiedName!
        };
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Optional(readModel))));

        var result = await _sut.ValidateBasisAsync<SampleRequest>();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.IsValid.ShouldBeTrue();
                r.Warnings.ShouldNotBeEmpty();
                r.Warnings[0].ShouldContain("legal");
                return true;
            },
            Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBasisAsync_ContractBasisWithReference_Valid()
    {
        var readModel = new LawfulBasisReadModel
        {
            Id = Guid.NewGuid(),
            Basis = GDPRLawfulBasis.Contract,
            ContractReference = "Terms v1.0",
            RequestTypeName = typeof(SampleRequest).AssemblyQualifiedName!
        };
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Right<EncinaError, Option<LawfulBasisReadModel>>(Optional(readModel))));

        var result = await _sut.ValidateBasisAsync<SampleRequest>();

        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.IsValid.ShouldBeTrue();
                r.Warnings.ShouldBeEmpty();
                return true;
            },
            Left: _ => false).ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBasisAsync_ServiceReturnsError_ReturnsError()
    {
        _service.GetRegistrationByRequestTypeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Option<LawfulBasisReadModel>>>(
                Left<EncinaError, Option<LawfulBasisReadModel>>(EncinaErrors.Create("err", "failed"))));

        var result = await _sut.ValidateBasisAsync<SampleRequest>();

        result.IsLeft.ShouldBeTrue();
    }
}
