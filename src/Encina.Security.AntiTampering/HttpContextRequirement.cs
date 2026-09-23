namespace Encina.Security.AntiTampering;

/// <summary>
/// Controls whether a <see cref="RequireSignatureAttribute"/>-decorated request type may bypass
/// HMAC validation when there is no <see cref="Microsoft.AspNetCore.Http.HttpContext"/> available.
/// </summary>
/// <remarks>
/// <para>
/// An explicit <see cref="RequireSignatureAttribute.WhenNoHttpContext"/> value always wins over
/// <see cref="AntiTamperingOptions.SkipWhenNoHttpContext"/>: a request type can insist on strict,
/// fail-closed validation via <see cref="Reject"/> even when the global option skips validation,
/// and a request type can opt into the skip via <see cref="Skip"/> even when the global option
/// does not.
/// </para>
/// </remarks>
public enum HttpContextRequirement
{
    /// <summary>
    /// Defer to <see cref="AntiTamperingOptions.SkipWhenNoHttpContext"/>. This is the default.
    /// </summary>
    Inherit = 0,

    /// <summary>
    /// Always fail closed (reject the request) when no <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
    /// is available, regardless of the global option.
    /// </summary>
    Reject = 1,

    /// <summary>
    /// Always skip validation when no <see cref="Microsoft.AspNetCore.Http.HttpContext"/> is
    /// available, regardless of the global option.
    /// </summary>
    Skip = 2
}
