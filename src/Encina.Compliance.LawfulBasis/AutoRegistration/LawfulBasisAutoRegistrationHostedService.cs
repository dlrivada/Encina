using System.Reflection;

using Encina.Compliance.GDPR;
using Encina.Compliance.LawfulBasis.Abstractions;
using Encina.Compliance.LawfulBasis.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Encina.Compliance.LawfulBasis.AutoRegistration;

/// <summary>
/// Hosted service that auto-registers lawful basis from assembly attributes and default bases at startup.
/// </summary>
/// <remarks>
/// <para>
/// This service is registered automatically when <see cref="LawfulBasisOptions.AutoRegisterFromAttributes"/>
/// is <c>true</c> in the <c>AddEncinaLawfulBasis</c> call. At startup, it:
/// </para>
/// <list type="number">
/// <item><description>Scans assemblies from <see cref="LawfulBasisOptions.AssembliesToScan"/> for
/// types decorated with <see cref="LawfulBasisAttribute"/>.</description></item>
/// <item><description>Registers each discovered type in the <see cref="ILawfulBasisService"/>
/// via <see cref="ILawfulBasisService.RegisterAsync"/>.</description></item>
/// <item><description>Registers programmatic default bases from
/// <see cref="LawfulBasisOptions.DefaultBases"/>.</description></item>
/// <item><description>Logs a summary with EventId 8370.</description></item>
/// </list>
/// </remarks>
internal sealed class LawfulBasisAutoRegistrationHostedService : IHostedService
{
    private readonly ILawfulBasisService _service;
    private readonly LawfulBasisAutoRegistrationDescriptor _descriptor;
    private readonly ILogger<LawfulBasisAutoRegistrationHostedService> _logger;

    public LawfulBasisAutoRegistrationHostedService(
        ILawfulBasisService service,
        LawfulBasisAutoRegistrationDescriptor descriptor,
        ILogger<LawfulBasisAutoRegistrationHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(logger);

        _service = service;
        _descriptor = descriptor;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var count = 0;

        // 1. Auto-register from attributes
        foreach (var assembly in _descriptor.Assemblies)
        {
            var types = GetTypesWithLawfulBasisAttribute(assembly);

            foreach (var (requestType, attr) in types)
            {
                var requestTypeName = requestType.AssemblyQualifiedName ?? requestType.FullName ?? requestType.Name;

                await _service.RegisterAsync(
                    Guid.NewGuid(),
                    requestTypeName,
                    attr.Basis,
                    attr.Purpose,
                    attr.LIAReference,
                    attr.LegalReference,
                    attr.ContractReference,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                count++;
            }
        }

        // 2. Register programmatic default bases
        var defaultCount = 0;
        foreach (var (requestType, basis) in _descriptor.DefaultBases)
        {
            var requestTypeName = requestType.AssemblyQualifiedName ?? requestType.FullName ?? requestType.Name;

            await _service.RegisterAsync(
                Guid.NewGuid(),
                requestTypeName,
                basis,
                $"Default basis from {nameof(LawfulBasisOptions)}.{nameof(LawfulBasisOptions.DefaultBases)}",
                liaReference: null,
                legalReference: null,
                contractReference: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            defaultCount++;
        }

        _logger.LawfulBasisAutoRegistrationCompleted(
            count + defaultCount,
            _descriptor.Assemblies.Count,
            defaultCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<(Type RequestType, LawfulBasisAttribute Attribute)> GetTypesWithLawfulBasisAttribute(
        Assembly assembly)
    {
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        foreach (var type in types)
        {
            var attr = type.GetCustomAttribute<LawfulBasisAttribute>();
            if (attr is not null)
            {
                yield return (type, attr);
            }
        }
    }
}
