using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// Marker options validated at startup to require the default <see cref="RequestContextAccessor"/>.
/// </summary>
/// <remarks>
/// Identity scopes push and end holders in the static store of <see cref="RequestContextAccessor"/>,
/// and the dispatcher reads that store. A different registered accessor would make every scope
/// write where nothing reads, so dispatches would silently run anonymous. The host fails to start
/// instead, with the message of <see cref="RequestIdentityErrorCodes.UnsupportedAccessor"/>.
/// </remarks>
internal sealed class RequestContextAccessorRequirement;

/// <summary>
/// Fails startup when the registered <see cref="IRequestContextAccessor"/> is not the default
/// <see cref="RequestContextAccessor"/>.
/// </summary>
internal sealed class RequestContextAccessorRequirementValidator : IValidateOptions<RequestContextAccessorRequirement>
{
    private readonly IRequestContextAccessor _accessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestContextAccessorRequirementValidator"/> class.
    /// </summary>
    /// <param name="accessor">The registered accessor.</param>
    public RequestContextAccessorRequirementValidator(IRequestContextAccessor accessor)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        _accessor = accessor;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RequestContextAccessorRequirement options) =>
        _accessor is RequestContextAccessor
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(RequestIdentityErrors.UnsupportedAccessorMessage);
}
