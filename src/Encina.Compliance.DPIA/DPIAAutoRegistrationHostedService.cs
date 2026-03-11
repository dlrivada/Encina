using System.Diagnostics;
using System.Reflection;

using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;

using LanguageExt;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Hosted service that auto-registers DPIA draft assessments from assembly attributes at startup.
/// </summary>
/// <remarks>
/// <para>
/// This service scans the configured assemblies for request types decorated with
/// <see cref="RequiresDPIAAttribute"/> and creates draft <see cref="DPIAAssessment"/> records
/// in the <see cref="IDPIAStore"/> for any types that do not already have an assessment.
/// </para>
/// <para>
/// When <see cref="DPIAOptions.AutoDetectHighRisk"/> is enabled, the service additionally uses
/// heuristic analysis (<see cref="DPIAAutoDetector"/>) to discover request types that might
/// require a DPIA even without explicit attribute decoration. This supplements the attribute-based
/// approach per EDPB WP 248 rev.01 guidance.
/// </para>
/// <para>
/// The service runs once at application startup (<see cref="IHostedService.StartAsync"/>)
/// and does not perform background processing.
/// </para>
/// </remarks>
internal sealed class DPIAAutoRegistrationHostedService : IHostedService
{
    private readonly IDPIAStore _store;
    private readonly DPIAOptions _options;
    private readonly DPIAAutoRegistrationDescriptor _descriptor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DPIAAutoRegistrationHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DPIAAutoRegistrationHostedService"/> class.
    /// </summary>
    public DPIAAutoRegistrationHostedService(
        IDPIAStore store,
        IOptions<DPIAOptions> options,
        DPIAAutoRegistrationDescriptor descriptor,
        TimeProvider timeProvider,
        ILogger<DPIAAutoRegistrationHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _store = store;
        _options = options.Value;
        _descriptor = descriptor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var assemblies = _descriptor.Assemblies;
        _logger.AutoRegistrationStarted(assemblies.Count);

        var registeredCount = 0;
        var skippedCount = 0;

        // Step 1: Discover types with [RequiresDPIA] attribute
        var discoveredTypes = new Dictionary<string, Type>();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.GetCustomAttribute<RequiresDPIAAttribute>() is not null)
                    {
                        var fullName = type.FullName ?? type.Name;
                        discoveredTypes.TryAdd(fullName, type);
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Some types in the assembly could not be loaded — skip gracefully.
            }
        }

        // Step 2: Optionally apply auto-detection heuristics
        if (_options.AutoDetectHighRisk)
        {
            var autoDetector = new DPIAAutoDetector(_logger);

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        var fullName = type.FullName ?? type.Name;

                        // Skip types already discovered via attribute
                        if (discoveredTypes.ContainsKey(fullName))
                        {
                            continue;
                        }

                        if (autoDetector.IsHighRisk(type))
                        {
                            discoveredTypes.TryAdd(fullName, type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Some types in the assembly could not be loaded — skip gracefully.
                }
            }
        }

        // Step 3: Create draft assessments for discovered types
        var nowUtc = _timeProvider.GetUtcNow();
        var reviewAtUtc = nowUtc.Add(_options.DefaultReviewPeriod);

        foreach (var (fullTypeName, type) in discoveredTypes)
        {
            try
            {
                // Check if an assessment already exists
                var existingResult = await _store
                    .GetAssessmentAsync(fullTypeName, cancellationToken)
                    .ConfigureAwait(false);

                var exists = existingResult.Match(
                    Right: assessmentOption => assessmentOption.IsSome,
                    Left: _ => false);

                if (exists)
                {
                    _logger.AutoRegistrationSkipped(fullTypeName);
                    skippedCount++;
                    continue;
                }

                // Create a draft assessment
                var attribute = type.GetCustomAttribute<RequiresDPIAAttribute>();
                var assessmentId = Guid.NewGuid();

                var assessment = new DPIAAssessment
                {
                    Id = assessmentId,
                    RequestTypeName = fullTypeName,
                    RequestType = type,
                    Status = DPIAAssessmentStatus.Draft,
                    ProcessingType = attribute?.ProcessingType,
                    Reason = attribute?.Reason ?? "Auto-registered at startup.",
                    CreatedAtUtc = nowUtc,
                    NextReviewAtUtc = reviewAtUtc,
                };

                var saveResult = await _store
                    .SaveAssessmentAsync(assessment, cancellationToken)
                    .ConfigureAwait(false);

                saveResult.Match(
                    Right: _ =>
                    {
                        _logger.AutoRegistrationDraftCreated(fullTypeName, assessmentId);
                        DPIADiagnostics.AutoRegistrationCount.Add(1, new TagList
                        {
                            { DPIADiagnostics.TagRequestType, type.Name }
                        });
                        registeredCount++;
                    },
                    Left: error =>
                    {
                        _logger.AutoRegistrationFailed(fullTypeName, new InvalidOperationException(error.Message));
                    });
            }
            catch (Exception ex)
            {
                _logger.AutoRegistrationFailed(fullTypeName, ex);
            }
        }

        _logger.AutoRegistrationCompleted(registeredCount, skippedCount);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
