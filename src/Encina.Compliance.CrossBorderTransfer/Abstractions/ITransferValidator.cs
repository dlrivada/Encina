using Encina.Compliance.CrossBorderTransfer.Model;
using LanguageExt;

namespace Encina.Compliance.CrossBorderTransfer.Abstractions;

/// <summary>
/// Orchestrates the full validation of an international data transfer request
/// against GDPR Chapter V requirements and the Schrems II judgment.
/// </summary>
/// <remarks>
/// <para>
/// The validator performs a cascading evaluation:
/// (1) adequacy decision check → (2) approved transfer check → (3) SCC agreement check →
/// (4) TIA requirement check → (5) derogation evaluation.
/// </para>
/// <para>
/// The first matching mechanism determines the <see cref="TransferValidationOutcome.Basis"/>.
/// If no mechanism applies, the transfer is blocked with <see cref="TransferBasis.Blocked"/>.
/// </para>
/// <para>
/// This interface is used by <c>TransferBlockingPipelineBehavior</c> to enforce transfer
/// compliance at the request pipeline level.
/// </para>
/// </remarks>
public interface ITransferValidator
{
    /// <summary>
    /// Validates a proposed international data transfer against all applicable GDPR Chapter V mechanisms.
    /// </summary>
    /// <param name="request">The transfer request containing source, destination, and data category.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Either an error or the transfer validation outcome.</returns>
    ValueTask<Either<EncinaError, TransferValidationOutcome>> ValidateAsync(
        TransferRequest request,
        CancellationToken cancellationToken = default);
}
