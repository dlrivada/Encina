namespace Encina.Marten.IntegrationTests.Versioning;

/// <summary>
/// Test event types for versioning integration tests.
/// </summary>

// Version 1 event - original schema
public sealed record ProductCreatedV1(Guid ProductId, string Name);

// Version 2 event - added Price field
public sealed record ProductCreatedV2(Guid ProductId, string Name, decimal Price);

// Another event for testing
public sealed record ProductUpdatedV1(Guid ProductId, string NewName);

public sealed record ProductUpdatedV2(Guid ProductId, string NewName, DateTime UpdatedAtUtc);
