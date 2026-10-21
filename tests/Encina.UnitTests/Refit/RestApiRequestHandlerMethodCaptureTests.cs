using Encina.Recoverability;
using Encina.Refit;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Encina.UnitTests.Refit;

/// <summary>
/// The method capture handler added by <c>AddEncinaRefitClient</c> lets the handler judge a timeout ambiguous only
/// for a non-idempotent method, and the registration resolves under a validating provider (#1223).
/// </summary>
public sealed class RestApiRequestHandlerMethodCaptureTests
{
    public interface ICaptureApi
    {
        [Get("/items")]
        Task Fetch(CancellationToken cancellationToken);

        [Post("/messages")]
        Task Send(CancellationToken cancellationToken);
    }

    public sealed class FetchRequest : IRestApiRequest<ICaptureApi, string>
    {
        public async Task<string> ExecuteAsync(ICaptureApi apiClient, CancellationToken cancellationToken)
        {
            await apiClient.Fetch(cancellationToken);
            return "got";
        }
    }

    public sealed class SendRequest : IRestApiRequest<ICaptureApi, string>
    {
        public async Task<string> ExecuteAsync(ICaptureApi apiClient, CancellationToken cancellationToken)
        {
            await apiClient.Send(cancellationToken);
            return "sent";
        }
    }

    private sealed class TimingOutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("timed out", new TimeoutException());
    }

    private static ServiceProvider Provider(bool validate)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaRefitClient<ICaptureApi>(client => client.BaseAddress = new Uri("http://test.example"))
            .ConfigurePrimaryHttpMessageHandler(() => new TimingOutHandler());
        services.AddTransient<RestApiRequestHandler<FetchRequest, ICaptureApi, string>>();
        services.AddTransient<RestApiRequestHandler<SendRequest, ICaptureApi, string>>();

        return validate
            ? services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true })
            : services.BuildServiceProvider();
    }

    [Fact]
    public async Task Handle_PostTimeout_IsAmbiguous()
    {
        using var provider = Provider(validate: false);
        var handler = provider.GetRequiredService<RestApiRequestHandler<SendRequest, ICaptureApi, string>>();

        var result = await handler.Handle(new SendRequest(), CancellationToken.None);

        var error = (EncinaError)result;
        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.Timeout);
        error.GetMetadata()[OutboundErrorMetadataKeys.Ambiguous].ShouldBe(true);
    }

    [Fact]
    public async Task Handle_GetTimeout_IsNotAmbiguous()
    {
        using var provider = Provider(validate: false);
        var handler = provider.GetRequiredService<RestApiRequestHandler<FetchRequest, ICaptureApi, string>>();

        var result = await handler.Handle(new FetchRequest(), CancellationToken.None);

        var error = (EncinaError)result;
        error.GetCode().IfNone(string.Empty).ShouldBe(OutboundErrorCodes.Timeout);
        error.GetMetadata().ContainsKey(OutboundErrorMetadataKeys.Ambiguous).ShouldBeFalse();
    }

    [Fact]
    public void AddEncinaRefitClient_WithValidateOnBuildAndScopes_ResolvesTheClientAndTheRequestHandler()
    {
        Should.NotThrow(() =>
        {
            using var provider = Provider(validate: true);

            provider.GetRequiredService<ICaptureApi>().ShouldNotBeNull();
            provider.GetRequiredService<RestApiRequestHandler<SendRequest, ICaptureApi, string>>().ShouldNotBeNull();
        });
    }
}
