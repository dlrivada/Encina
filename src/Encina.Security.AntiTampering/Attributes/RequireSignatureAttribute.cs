namespace Encina.Security.AntiTampering;

/// <summary>
/// Marks a request type for automatic HMAC signature validation in the pipeline.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a command or query class, the <c>HMACValidationPipelineBehavior</c>
/// automatically extracts signature headers from the HTTP context, verifies the HMAC
/// signature, validates the timestamp tolerance, and checks the nonce for replay attacks.
/// </para>
/// <para>
/// Requests without this attribute pass through the pipeline behavior without validation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Require signature with any registered key
/// [RequireSignature]
/// public sealed record ProcessPaymentCommand(decimal Amount) : ICommand;
///
/// // Require signature with a specific key ID
/// [RequireSignature(KeyId = "partner-api-v2")]
/// public sealed record PartnerWebhookCommand(string Payload) : ICommand;
///
/// // Skip nonce validation for idempotent operations
/// [RequireSignature(SkipReplayProtection = true)]
/// public sealed record IdempotentUpdateCommand(Guid Id, string Data) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireSignatureAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the expected key identifier for signature validation.
    /// </summary>
    /// <remarks>
    /// When <c>null</c> (default), any registered key can be used for signing.
    /// Set to a specific key ID to restrict which key is accepted for this request type,
    /// useful for partner-specific API endpoints.
    /// </remarks>
    public string? KeyId { get; set; }

    /// <summary>
    /// Gets or sets whether to skip nonce-based replay protection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <c>false</c> (replay protection enabled). Set to <c>true</c> only for
    /// idempotent operations where replaying the same request is safe and acceptable.
    /// </para>
    /// <para>
    /// When <c>true</c>, the nonce header is still extracted but not validated against
    /// the <see cref="Abstractions.INonceStore"/>.
    /// </para>
    /// </remarks>
    public bool SkipReplayProtection { get; set; }

    /// <summary>
    /// Gets or sets whether this request type is allowed to bypass HMAC validation when
    /// there is no <see cref="Microsoft.AspNetCore.Http.HttpContext"/> available.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is <see cref="HttpContextRequirement.Inherit"/>: the effective behavior comes from
    /// the global <see cref="AntiTamperingOptions.SkipWhenNoHttpContext"/> switch, which defaults
    /// to fail closed (reject the request) when no <see cref="Microsoft.AspNetCore.Http.HttpContext"/>
    /// is available to extract signature headers from (background jobs, message consumers,
    /// scheduled jobs, gRPC/SignalR paths without an HTTP context).
    /// </para>
    /// <para>
    /// Set to <see cref="HttpContextRequirement.Skip"/> when this specific request type is
    /// intentionally invoked outside an HTTP pipeline (for example, replayed internally by a
    /// trusted background worker) and the caller accepts running without signature verification
    /// in that case, even if the global switch does not skip validation. Set to
    /// <see cref="HttpContextRequirement.Reject"/> to force strict, fail-closed validation for
    /// this request type even when the global switch skips validation for everything else. An
    /// explicit attribute value always wins over the global option. Every use of the skip is
    /// logged as a warning, naming which switch (attribute or global option) caused it.
    /// </para>
    /// </remarks>
    public HttpContextRequirement WhenNoHttpContext { get; set; }
}
