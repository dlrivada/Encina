using Encina.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.UnitTests.AspNetCore;

/// <summary>
/// Unit tests for <see cref="ApplicationBuilderExtensions"/>.
/// </summary>
public sealed class ApplicationBuilderExtensionsTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddEncinaAspNetCore();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void UseEncinaContext_WhenCalled_ReturnsApplicationBuilder()
    {
        using var provider = BuildProvider();
        var app = new ApplicationBuilder(provider);

        var result = app.UseEncinaContext();

        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IApplicationBuilder>();
    }

    [Fact]
    public void UseEncinaContext_WhenBuilt_RegistersMiddlewareInPipeline()
    {
        using var provider = BuildProvider();
        var app = new ApplicationBuilder(provider);

        app.UseEncinaContext();
        var pipeline = app.Build();

        pipeline.ShouldNotBeNull();
    }

    [Fact]
    public async Task UseEncinaContext_WhenPipelineExecuted_DoesNotThrow()
    {
        using var provider = BuildProvider();
        var app = new ApplicationBuilder(provider);
        app.UseEncinaContext();
        var pipeline = app.Build();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = app.ApplicationServices
        };

        // The middleware should execute and pass through; the pipeline ends with no
        // terminal handler so the response status remains 404 (default for unhandled).
        var act = async () => await pipeline(httpContext);

        await Should.NotThrowAsync(act);
    }
}
