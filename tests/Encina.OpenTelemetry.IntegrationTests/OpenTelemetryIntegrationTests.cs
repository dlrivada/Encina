using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Xunit;

namespace Encina.OpenTelemetry.Tests.Integration;

[Trait("Category", "Integration")]
public class OpenTelemetryIntegrationTests
{
    [Fact]
    public async Task AddEncinaInstrumentation_Should_Register_ActivitySource()
    {
        var services = new ServiceCollection();
        services.AddEncina(config => { });
        services.AddSingleton<IRequestHandler<PingQuery, string>, PingHandler>();
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
                .AddEncinaInstrumentation()
                .AddConsoleExporter());

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PingQuery(), default);
        result.IsRight.Should().BeTrue();
    }

    public record PingQuery : IQuery<string>;

    public class PingHandler : IRequestHandler<PingQuery, string>
    {
        public async Task<Either<MediatorError, string>> Handle(PingQuery request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return "Pong";
        }
    }
}
