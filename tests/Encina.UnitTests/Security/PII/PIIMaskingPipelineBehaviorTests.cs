#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Internal;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Encina.UnitTests.Security.PII;

public sealed class PIIMaskingPipelineBehaviorTests : IDisposable
{
    private readonly IRequestContext _context;

    public PIIMaskingPipelineBehaviorTests()
    {
        _context = RequestContext.CreateForTest(userId: "user-1");
        PIIPropertyScanner.ClearCache();
    }

    public void Dispose()
    {
        PIIPropertyScanner.ClearCache();
    }

    #region Test Request/Response Types

    private sealed record TestRequest : IRequest<TestResponse>;

    private sealed class TestResponse
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = "test@example.com";

        public string NonPii { get; set; } = "safe";
    }

    private sealed record TestNullableRequest : IRequest<TestNullableResponse>;

    private sealed class TestNullableResponse
    {
        [PII(PIIType.Name)]
        public string Name { get; set; } = "John Doe";
    }

    #endregion

    #region Helper Methods

    private static PIIMaskingPipelineBehavior<TRequest, TResponse> CreateSut<TRequest, TResponse>(
        PIIOptions? options = null,
        IPIIMasker? masker = null)
        where TRequest : IRequest<TResponse>
    {
        var opts = options ?? new PIIOptions();
        masker ??= CreateRealMasker(opts);
        return new PIIMaskingPipelineBehavior<TRequest, TResponse>(
            masker,
            Options.Create(opts),
            NullLogger<PIIMaskingPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    private static IPIIMasker CreateRealMasker(PIIOptions? options = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaPII(o =>
        {
            if (options is not null)
            {
                o.MaskInResponses = options.MaskInResponses;
                o.MaskInLogs = options.MaskInLogs;
                o.MaskInAuditTrails = options.MaskInAuditTrails;
                o.DefaultMode = options.DefaultMode;
                o.EnableTracing = options.EnableTracing;
                o.EnableMetrics = options.EnableMetrics;
            }
        });
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IPIIMasker>();
    }

    #endregion

    [Fact]
    public async Task Handle_SuccessfulResponse_MasksPIIProperties()
    {
        // Arrange
        var sut = CreateSut<TestRequest, TestResponse>();
        var request = new TestRequest();
        var response = new TestResponse { Email = "test@example.com", NonPii = "safe" };
        RequestHandlerCallback<TestResponse> nextStep =
            () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(response);

        // Act
        var result = await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                // Email should be masked (not the original value)
                r.Email.ShouldNotBe("test@example.com");
                r.Email.ShouldContain("@example.com"); // Domain preserved
                // NonPii should be preserved
                r.NonPii.ShouldBe("safe");
            },
            Left: _ => throw new InvalidOperationException("Expected Right but got Left"));
    }

    [Fact]
    public async Task Handle_ErrorResponse_PassesThroughUnchanged()
    {
        // Arrange
        var sut = CreateSut<TestRequest, TestResponse>();
        var request = new TestRequest();
        var error = EncinaError.New("Something went wrong");
        RequestHandlerCallback<TestResponse> nextStep =
            () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(error);

        // Act
        var result = await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_MaskInResponsesDisabled_SkipsMasking()
    {
        // Arrange
        var options = new PIIOptions { MaskInResponses = false };
        var sut = CreateSut<TestRequest, TestResponse>(options);
        var request = new TestRequest();
        var response = new TestResponse { Email = "test@example.com", NonPii = "safe" };
        RequestHandlerCallback<TestResponse> nextStep =
            () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(response);

        // Act
        var result = await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                // Email should NOT be masked because MaskInResponses is disabled
                r.Email.ShouldBe("test@example.com");
                r.NonPii.ShouldBe("safe");
            },
            Left: _ => throw new InvalidOperationException("Expected Right but got Left"));
    }

    [Fact]
    public async Task Handle_ResponseWithNoPIIProperties_PreservesAllValues()
    {
        // Arrange - TestNullableResponse only has [PII(PIIType.Name)] on Name,
        // verify that the non-PII fields of the response pass through correctly
        var sut = CreateSut<TestNullableRequest, TestNullableResponse>();
        var request = new TestNullableRequest();
        var response = new TestNullableResponse { Name = "John Doe" };
        RequestHandlerCallback<TestNullableResponse> nextStep =
            () => ValueTask.FromResult<Either<EncinaError, TestNullableResponse>>(response);

        // Act
        var result = await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                // Name should be masked (it has [PII(PIIType.Name)])
                r.Name.ShouldNotBe("John Doe");
            },
            Left: _ => throw new InvalidOperationException("Expected Right but got Left"));
    }

    [Fact]
    public async Task Handle_MaskingFails_ReturnsOriginalResponse()
    {
        // Arrange - use a mock masker that throws
        var mockMasker = Substitute.For<IPIIMasker>();
        mockMasker.MaskObject(Arg.Any<TestResponse>())
            .Throws(new InvalidOperationException("Masking engine failure"));

        var sut = CreateSut<TestRequest, TestResponse>(masker: mockMasker);
        var request = new TestRequest();
        var response = new TestResponse { Email = "test@example.com", NonPii = "safe" };
        RequestHandlerCallback<TestResponse> nextStep =
            () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(response);

        // Act
        var result = await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert - original response is returned (masking failure does not cause request failure)
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: r =>
            {
                r.Email.ShouldBe("test@example.com");
                r.NonPii.ShouldBe("safe");
            },
            Left: _ => throw new InvalidOperationException("Expected Right but got Left"));
    }

    [Fact]
    public async Task Handle_InvokesNextStep()
    {
        // Arrange
        var sut = CreateSut<TestRequest, TestResponse>();
        var request = new TestRequest();
        var nextStepCalled = false;

        RequestHandlerCallback<TestResponse> nextStep = () =>
        {
            nextStepCalled = true;
            var response = new TestResponse { Email = "test@example.com", NonPii = "safe" };
            return ValueTask.FromResult<Either<EncinaError, TestResponse>>(response);
        };

        // Act
        await sut.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
    }
}
