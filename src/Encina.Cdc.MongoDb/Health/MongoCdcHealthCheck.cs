using Encina.Cdc.Abstractions;
using Encina.Cdc.Health;

namespace Encina.Cdc.MongoDb.Health;

/// <summary>
/// Health check for MongoDB Change Streams CDC infrastructure.
/// Verifies connector connectivity and position store accessibility.
/// </summary>
/// <remarks>
/// Tags: "encina", "cdc", "ready", "mongodb", "change-stream" for Kubernetes readiness probes.
/// </remarks>
public class MongoCdcHealthCheck : CdcHealthCheck
{
    private static readonly string[] ProviderTags = ["mongodb", "change-stream"];

    /// <summary>
    /// The default health check name.
    /// </summary>
    public const string DefaultName = "encina-cdc-mongodb";

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoCdcHealthCheck"/> class.
    /// </summary>
    /// <param name="connector">The MongoDB CDC connector to check.</param>
    /// <param name="positionStore">The position store to verify accessibility.</param>
    public MongoCdcHealthCheck(
        ICdcConnector connector,
        ICdcPositionStore positionStore)
        : base(DefaultName, connector, positionStore, ProviderTags)
    {
    }
}
