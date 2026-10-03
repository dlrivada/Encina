using Encina.Security.ABAC;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using ISecurityContextAccessor = global::Encina.Security.ISecurityContextAccessor;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Registration completeness of <c>AddEncinaABAC</c>: the provider builds with
/// <c>ValidateOnBuild</c> and <c>ValidateScopes</c> and resolves the pipeline behavior with every
/// dependency it takes, including the <c>EELCompiler</c> added by #1634.
/// </summary>
public sealed class ABACRegistrationTests
{
    [RequirePolicy("policy-a")]
    [RequireCondition("user.department == \"HR\"")]
    private sealed record GuardedRequest : IRequest<string>;

    [Fact]
    public void AddEncinaABAC_BuildsValidatedProviderAndResolvesPipelineBehavior()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISecurityContextAccessor>());
        services.AddEncinaABAC();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        var behavior = scope.ServiceProvider.GetRequiredService<IPipelineBehavior<GuardedRequest, string>>();

        behavior.ShouldBeOfType<ABACPipelineBehavior<GuardedRequest, string>>();
    }
}
