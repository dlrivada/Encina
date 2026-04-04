using Encina.Sharding.Shadow;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Observability.OpenTelemetry;

[Trait("Category", "Integration")]
public class OpenTelemetryIntegrationTests
{
    [Fact]
    public async Task AddEncinaInstrumentation_Should_Register_ActivitySource()
    {
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<PingQuery, string>, PingHandler>();
        // Register mock for shadow sharding dependency (required by ShadowReadPipelineBehavior)
        services.AddSingleton(Substitute.For<IShadowShardRouter>());
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var sp = services.BuildServiceProvider();
        var Encina = sp.GetRequiredService<IEncina>();

        var result = await Encina.Send(new PingQuery(), default);
        result.ShouldBeSuccess();
    }

    public record PingQuery : IQuery<string>;

    public class PingHandler : IRequestHandler<PingQuery, string>
    {
        public async Task<Either<EncinaError, string>> Handle(PingQuery request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return "Pong";
        }
    }
}
