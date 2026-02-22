namespace Encina.Security.Secrets;

/// <summary>
/// Marks a property for automatic secret injection from the registered
/// <see cref="Abstractions.ISecretReader"/>.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a <c>string</c> property on a request object, the
/// <c>SecretInjectionPipelineBehavior</c> will automatically fetch the secret
/// from the configured <see cref="Abstractions.ISecretReader"/> and set the
/// property value before the handler executes.
/// </para>
/// <para>
/// Requires <see cref="SecretsOptions.EnableSecretInjection"/> to be <c>true</c>
/// in the DI configuration.
/// </para>
/// <para>
/// <b>Note:</b> Only writable <c>string</c> properties are supported. Properties
/// with <c>init</c>-only setters may not be injectable depending on the runtime context.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed record ProcessPaymentCommand(
///     decimal Amount,
///     [property: InjectSecret("stripe-api-key")] string? StripeKey
/// ) : ICommand&lt;PaymentResult&gt;;
///
/// // With explicit version
/// public sealed record ConnectDatabaseCommand(
///     [property: InjectSecret("db-password", Version = "v2")] string? Password
/// ) : ICommand;
///
/// // With optional secret (no error if not found)
/// public sealed record SendNotificationCommand(
///     string Message,
///     [property: InjectSecret("slack-webhook", FailOnError = false)] string? WebhookUrl
/// ) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class InjectSecretAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectSecretAttribute"/> class
    /// with the specified secret name.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve and inject.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secretName"/> is null.</exception>
    public InjectSecretAttribute(string secretName)
    {
        ArgumentNullException.ThrowIfNull(secretName);
        SecretName = secretName;
    }

    /// <summary>
    /// Gets the name of the secret to retrieve and inject.
    /// </summary>
    public string SecretName { get; }

    /// <summary>
    /// Gets or sets the optional secret version.
    /// </summary>
    /// <remarks>
    /// When set, the version is appended to the secret name using the format
    /// <c>"{secretName}/{version}"</c> when querying the provider. When <c>null</c>
    /// (default), the latest version is retrieved.
    /// </remarks>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets whether injection failure should cause the pipeline to short-circuit
    /// with an error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (default), a failed secret retrieval returns
    /// <c>Left(SecretsErrors.InjectionFailed(...))</c> and the handler is not executed.
    /// </para>
    /// <para>
    /// When <c>false</c>, the failure is logged as a warning and the pipeline continues
    /// with the property value unchanged.
    /// </para>
    /// </remarks>
    public bool FailOnError { get; set; } = true;
}
