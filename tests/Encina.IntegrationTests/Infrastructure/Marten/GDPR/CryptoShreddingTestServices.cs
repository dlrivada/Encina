using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// Builds the <see cref="IServiceScopeFactory"/> the crypto-shredding serializer resolves its key provider and
/// forgotten-subject handler from, for tests that build a Marten store by hand.
/// </summary>
internal static class CryptoShreddingTestServices
{
    internal static IServiceScopeFactory ScopeFactory(ISubjectKeyProvider keyProvider, IForgottenSubjectHandler? handler = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(keyProvider);
        services.AddSingleton(handler ?? new DefaultForgottenSubjectHandler(NullLogger<DefaultForgottenSubjectHandler>.Instance));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }
}
