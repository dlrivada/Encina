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
    private static ApplicationBuilder CreateAppBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddEncinaAspNetCore();
        var provider = services.BuildServiceProvider();
        return new ApplicationBuilder(provider);
    }

    [Fact]
    public void UseEncinaContext_ReturnsApplicationBuilder()
    {
        var app = CreateAppBuilder();

        var result = app.UseEncinaContext();

        result.ShouldNotBeNull();
        result.ShouldBeAssignableTo<IApplicationBuilder>();
    }

    [Fact]
    public void UseEncinaContext_RegistersMiddlewareInPipeline()
    {
        var app = CreateAppBuilder();

        app.UseEncinaContext();
        var pipeline = app.Build();

        pipeline.ShouldNotBeNull();
    }

    [Fact]
    public async Task UseEncinaContext_PipelineExecutesMiddlewareWithoutThrowing()
    {
        var app = CreateAppBuilder();
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
