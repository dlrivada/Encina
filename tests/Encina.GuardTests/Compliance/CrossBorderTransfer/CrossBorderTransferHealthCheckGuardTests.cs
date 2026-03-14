#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Health;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="CrossBorderTransferHealthCheck"/>.
/// </summary>
/// <remarks>
/// The <see cref="CrossBorderTransferHealthCheck"/> constructor does NOT use
/// <c>ArgumentNullException.ThrowIfNull</c> for its parameters.
/// No constructor guard tests are applicable for this type.
/// This file is retained as documentation of the evaluation.
/// </remarks>
public class CrossBorderTransferHealthCheckGuardTests
{
    // No guard clause tests: CrossBorderTransferHealthCheck constructor
    // does not validate its parameters with ArgumentNullException.ThrowIfNull.
}
