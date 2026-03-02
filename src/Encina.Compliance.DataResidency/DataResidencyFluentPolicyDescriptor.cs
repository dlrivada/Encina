namespace Encina.Compliance.DataResidency;

/// <summary>
/// Internal descriptor that carries fluent-configured residency policies for startup creation.
/// </summary>
/// <param name="Policies">The residency policies configured via <see cref="DataResidencyOptions.AddPolicy"/>.</param>
internal sealed record DataResidencyFluentPolicyDescriptor(IReadOnlyList<DataResidencyFluentPolicyEntry> Policies);
