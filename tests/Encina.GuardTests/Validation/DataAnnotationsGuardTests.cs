using System.ComponentModel.DataAnnotations;
using Encina.DataAnnotations;
using Encina.Testing.Identity;
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

    private sealed class CapturingRequest : IValidatableObject
    {
        public Dictionary<object, object?>? Items { get; private set; }

        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext validationContext)
        {
            Items = new Dictionary<object, object?>(validationContext.Items);
            return [];
        }
    }

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
    public async Task ValidateAsync_InvalidRequestWithTenant_ReturnsFailure()
    {
        var sut = new DataAnnotationsValidationProvider();
        var context = RequestContext.CreateForTest(tenantId: "tenant-1");

        var result = await sut.ValidateAsync(new SampleRequest(null), context, CancellationToken.None);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_AuthenticatedIdentityAndTenant_PassesUserAndTenantToValidationContext()
    {
        var sut = new DataAnnotationsValidationProvider();
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("corr-1");
        context.TenantId.Returns("tenant-1");
        context.Identity.Returns(TestIdentity.User("user-1"));
        var request = new CapturingRequest();

        var result = await sut.ValidateAsync(request, context, CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        request.Items.ShouldNotBeNull();
        request.Items!["UserId"].ShouldBe("user-1");
        request.Items["TenantId"].ShouldBe("tenant-1");
        request.Items["CorrelationId"].ShouldBe("corr-1");
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
        services.ShouldContain(d => d.ServiceType == typeof(ValidationOrchestrator));
        services.ShouldContain(d => d.ServiceType == typeof(IPipelineBehavior<,>)
            && d.ImplementationType == typeof(ValidationPipelineBehavior<,>));
    }
}
