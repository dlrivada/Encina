using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Default implementation of <see cref="IGDPRComplianceValidator"/> that always returns compliant.
/// </summary>
/// <remarks>
/// <para>
/// This is a no-op validator registered by default via <c>TryAdd</c>. It allows the pipeline
/// behavior to run without a custom validator while still performing registry-based checks.
/// </para>
/// <para>
/// Replace this by registering your own <see cref="IGDPRComplianceValidator"/> implementation
/// before calling <c>AddEncinaGDPR()</c>, or register it after using the standard DI override pattern.
/// </para>
/// </remarks>
public sealed class DefaultGDPRComplianceValidator : IGDPRComplianceValidator
{
    /// <inheritdoc />
    /// <returns>Always returns <see cref="ComplianceResult.Compliant()"/>.</returns>
    public ValueTask<Either<EncinaError, ComplianceResult>> ValidateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<Either<EncinaError, ComplianceResult>>(ComplianceResult.Compliant());
    }
}
