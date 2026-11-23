using System.ComponentModel.DataAnnotations;
using Encina.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.Validation;

/// <summary>
/// Guard tests for the Encina.DataAnnotations package: <see cref="DataAnnotationsValidationProvider"/>
/// argument guards and <c>AddDataAnnotationsValidation</c> registration guard.
/// </summary>
[Trait("Category", "Guard")]
public sealed class DataAnnotationsGuardTests
{
    private sealed record SampleRequest([property: Required] string? Name);

    [Fact]
    public async Task ValidateAsync_NullRequest_Throws()
    {
        var sut = new DataAnnotationsValidationProvider();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ValidateAsync<SampleRequest>(null!, context, CancellationToken.None));
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task ValidateAsync_NullContext_Throws()
    {
        var sut = new DataAnnotationsValidationProvider();

        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await sut.ValidateAsync(new SampleRequest("x"), null!, CancellationToken.None));
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_ReturnsSuccess()
    {
        var sut = new DataAnnotationsValidationProvider();
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");

        var result = await sut.ValidateAsync(new SampleRequest("x"), context, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateAsync_InvalidRequestWithUserAndTenant_ReturnsFailure()
    {
        var sut = new DataAnnotationsValidationProvider();
        var context = RequestContext.CreateForTest(tenantId: "tenant-1");

        var result = await sut.ValidateAsync(new SampleRequest(null), context, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void AddDataAnnotationsValidation_NullServices_Throws()
    {
        var ex = Should.Throw<ArgumentNullException>(() =>
            global::Encina.DataAnnotations.ServiceCollectionExtensions.AddDataAnnotationsValidation(null!));
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddDataAnnotationsValidation_ValidServices_RegistersProvider()
    {
        var services = new ServiceCollection();

        var returned = services.AddDataAnnotationsValidation();

        returned.ShouldBeSameAs(services);
        services.ShouldContain(d => d.ServiceType == typeof(IValidationProvider));
    }
}
