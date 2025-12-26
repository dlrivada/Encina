namespace Encina.Marten.Tests.Versioning;

/// <summary>
/// Test event types for versioning tests.
/// </summary>

// Version 1 event - original schema
public sealed record OrderCreatedV1(Guid OrderId, string CustomerName);

// Version 2 event - added Email field
public sealed record OrderCreatedV2(Guid OrderId, string CustomerName, string Email);

// Version 3 event - added ShippingAddress
public sealed record OrderCreatedV3(Guid OrderId, string CustomerName, string Email, string ShippingAddress);

// Simple event for basic upcasting tests
public sealed record SimpleEventV1(string Value);

// Target event for simple tests
public sealed record SimpleEventV2(string Value, int Number);
