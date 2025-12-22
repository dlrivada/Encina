using System.Security.Claims;
using Encina.AspNetCore.Tests;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.AspNetCore.ContractTests;

/// <summary>
/// Contract tests for <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>.
/// Verifies that authorization behavior follows the IPipelineBehavior contract.
/// </summary>
[Trait("Category", "Contract")]
public sealed class AuthorizationPipelineBehaviorContractTests
{
    [Fact]
    public async Task Contract_MustImplementIPipelineBehavior()
    {
        // Arrange
        var behavior = CreateBehavior<TestRequest, string>();

        // Act & Assert
        behavior.ShouldBeAssignableTo<IPipelineBehavior<TestRequest, string>>();
    }

    [Fact]
    public async Task Contract_HandleMustAcceptRequestContextAndCallback()
    {
        // Arrange
        var behavior = CreateBehavior<TestRequest, string>();
        var request = new TestRequest();
        var context = RequestContext.CreateForTest();
        var callbackExecuted = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            callbackExecuted = true;
            return ValueTask.FromResult(Right<MediatorError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        callbackExecuted.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_HandleMustRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var behavior = CreateBehavior<TestRequest, string>();
        var request = new TestRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = async () =>
        {
            await Task.Delay(100, cts.Token); // Should throw
            return Right<MediatorError, string>("success");
        };

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await behavior.Handle(request, context, nextStep, cts.Token);
        });
    }

    [Fact]
    public async Task Contract_HandleMustReturnEitherMediatorErrorOrResponse()
    {
        // Arrange
        var behavior = CreateBehavior<TestRequest, string>();
        var request = new TestRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, string>("result"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<Either<MediatorError, string>>();
    }

    [Fact]
    public async Task Contract_HandleMustPropagateNextStepResult()
    {
        // Arrange
        var behavior = CreateBehavior<TestRequest, string>();
        var request = new TestRequest();
        var context = RequestContext.CreateForTest();
        var expectedResult = "expected-value";

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, string>(expectedResult));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.Match(
            Left: _ => Assert.Fail("Expected Right"),
            Right: actual => actual.ShouldBe(expectedResult));
    }

    [Fact]
    public async Task Contract_HandleMustPropagateNextStepError()
    {
        // Arrange
        var behavior = CreateBehavior<TestRequest, string>();
        var request = new TestRequest();
        var context = RequestContext.CreateForTest();
        var expectedError = MediatorErrors.Create("test.error", "Test error");

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Left<MediatorError, string>(expectedError));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.Match(
            Left: error => error.Message.ShouldBe("Test error"),
            Right: _ => Assert.Fail("Expected Left"));
    }

    [Fact]
    public async Task Contract_UnauthenticatedUser_MustReturnMediatorError()
    {
        // Arrange
        var httpContext = new DefaultHttpContext(); // No authenticated user
        var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, string>("success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: error =>
            {
                error.Message.ShouldContain("requires authentication");
                error.GetCode().Match(
                    Some: code => code.ShouldBe("authorization.unauthenticated"),
                    None: () => Assert.Fail("Expected error code"));
            },
            Right: _ => Assert.Fail("Expected Left"));
    }

    [Fact]
    public async Task Contract_AuthenticatedUser_MustCallNextStep()
    {
        // Arrange
        var httpContext = CreateAuthenticatedContext("user-123");
        var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();
        var nextStepCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepCalled = true;
            return ValueTask.FromResult(Right<MediatorError, string>("success"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextStepCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_NoHttpContext_MustReturnMediatorError()
    {
        // Arrange
        var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext: null);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, string>("success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: error =>
            {
                error.Message.ShouldContain("Authorization requires HTTP context");
                error.GetCode().Match(
                    Some: code => code.ShouldBe("authorization.no_http_context"),
                    None: () => Assert.Fail("Expected error code"));
            },
            Right: _ => Assert.Fail("Expected Left"));
    }

    [Fact]
    public async Task Contract_ErrorCodeMustBeWellFormed()
    {
        // Arrange
        var behavior = CreateBehavior<AuthorizedRequest, string>(httpContext: null);
        var request = new AuthorizedRequest();
        var context = RequestContext.CreateForTest();

        RequestHandlerCallback<string> nextStep = () =>
            ValueTask.FromResult(Right<MediatorError, string>("success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.Match(
            Left: error =>
            {
                error.GetCode().Match(
                    Some: code =>
                    {
                        code.ShouldNotBeNullOrWhiteSpace();
                        code.ShouldStartWith("authorization.");
                        code.ShouldNotContain(" ");
                    },
                    None: () => Assert.Fail("Expected error code"));
            },
            Right: _ => Assert.Fail("Expected Left"));
    }

    // Test types
    private sealed record TestRequest : IRequest<string>;

    [Authorize]
    private sealed record AuthorizedRequest : IRequest<string>;

    // Helper methods
    private static AuthorizationPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        HttpContext? httpContext = null)
        where TRequest : IRequest<TResponse>
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var authorizationService = new TestAuthorizationService(shouldSucceed: true);

        return new AuthorizationPipelineBehavior<TRequest, TResponse>(
            authorizationService,
            httpContextAccessor);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new DefaultHttpContext
        {
            User = principal
        };
    }
}
