using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina;

/// <summary>
/// The default <see cref="IAccessElevationGate"/>: no elevation mechanism is installed, so every
/// denial is kept.
/// </summary>
internal sealed class NoAccessElevationGate : IAccessElevationGate
{
    /// <inheritdoc />
    public ValueTask<Either<EncinaError, Option<AccessElevation>>> TryElevateAsync<TRequest>(
        TRequest request,
        IRequestContext context,
        EncinaError denial,
        CancellationToken cancellationToken)
        where TRequest : notnull =>
        ValueTask.FromResult<Either<EncinaError, Option<AccessElevation>>>(Right<EncinaError, Option<AccessElevation>>(Option<AccessElevation>.None));
}
