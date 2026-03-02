namespace Encina.Compliance.Retention;

/// <summary>
/// Internal descriptor that carries fluent-configured retention policies for startup creation.
/// </summary>
/// <param name="Policies">The retention policies configured via <see cref="RetentionOptions.AddPolicy"/>.</param>
internal sealed record RetentionFluentPolicyDescriptor(IReadOnlyList<RetentionPolicyDescriptor> Policies);
