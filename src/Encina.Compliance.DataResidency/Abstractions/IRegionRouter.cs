using Encina.Compliance.DataResidency.Model;

using LanguageExt;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Determines the target region for processing a request based on data residency rules.
/// </summary>
/// <remarks>
/// <para>
/// The region router inspects incoming requests and determines which region should handle
/// the processing, based on the data category's residency policy, the request's metadata,
/// and the current execution context. This enables automatic geographic routing of data
/// operations to compliant regions.
/// </para>
/// <para>
/// Per GDPR Article 44, data must be processed in regions that comply with the applicable
/// transfer rules. The region router automates this routing decision, ensuring that requests
/// decorated with <c>[DataResidency]</c> attributes are directed to an appropriate region
/// before the pipeline behavior processes them.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Determine where to process a healthcare data request
/// var region = await regionRouter.DetermineTargetRegionAsync(
///     new StorePatientRecordCommand { PatientId = "patient-42" },
///     cancellationToken);
/// </code>
/// </example>
public interface IRegionRouter
{
    /// <summary>
    /// Determines the target region for processing the given request.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request being processed.</typeparam>
    /// <param name="request">The request to route to a target region.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="Region"/> where the request should be processed,
    /// or an <see cref="EncinaError"/> if no suitable region could be determined.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The routing logic may consider:
    /// - The <c>[DataResidency]</c> attribute on the request type to identify the data category.
    /// - The <c>AllowedRegions</c> for that category.
    /// - The current region from <see cref="IRegionContextProvider"/> as the preferred target.
    /// - Proximity, load balancing, or failover preferences.
    /// </para>
    /// <para>
    /// If the request type does not have a <c>[DataResidency]</c> attribute, the router may
    /// return the current region as the default or return an error depending on configuration.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Region>> DetermineTargetRegionAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken = default);
}
