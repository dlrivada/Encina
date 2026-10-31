using System.Net;
using Encina.MongoDB.Transactions;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Servers;
using Shouldly;

namespace Encina.UnitTests.MongoDB.Transactions;

/// <summary>
/// A client with <c>directConnection=true</c> reports a standalone cluster for a replica set member; the type of the
/// server itself decides whether transactions are supported.
/// </summary>
[Trait("Category", "Unit")]
public sealed class EffectiveClusterTypeTests
{
    private static ClusterDescription Describe(ClusterType type, params ServerType[] servers)
    {
        var clusterId = new ClusterId();
        var descriptions = servers.Select((serverType, i) => new ServerDescription(
            new ServerId(clusterId, new DnsEndPoint("localhost", 27017 + i)),
            new DnsEndPoint("localhost", 27017 + i),
            state: ServerState.Connected,
            type: serverType));
        return new ClusterDescription(clusterId, true, null, type, descriptions);
    }

    [Fact]
    public void NullDescription_IsUnknown()
        => MongoSessionAccessor.EffectiveClusterType(null).ShouldBe(ClusterType.Unknown);

    [Theory]
    [InlineData(ClusterType.ReplicaSet)]
    [InlineData(ClusterType.Sharded)]
    [InlineData(ClusterType.Unknown)]
    public void NonStandaloneCluster_KeepsItsType(ClusterType type)
        => MongoSessionAccessor.EffectiveClusterType(Describe(type, ServerType.ReplicaSetPrimary)).ShouldBe(type);

    [Fact]
    public void StandaloneClusterOfAStandaloneServer_IsStandalone()
        => MongoSessionAccessor.EffectiveClusterType(Describe(ClusterType.Standalone, ServerType.Standalone)).ShouldBe(ClusterType.Standalone);

    [Fact]
    public void DirectConnectionToAReplicaSetMember_IsReplicaSet()
        => MongoSessionAccessor.EffectiveClusterType(Describe(ClusterType.Standalone, ServerType.ReplicaSetPrimary)).ShouldBe(ClusterType.ReplicaSet);

    [Fact]
    public void DirectConnectionToAMongos_IsSharded()
        => MongoSessionAccessor.EffectiveClusterType(Describe(ClusterType.Standalone, ServerType.ShardRouter)).ShouldBe(ClusterType.Sharded);

    [Fact]
    public void StandaloneClusterWithNoDiscoveredServer_StaysStandalone()
        => MongoSessionAccessor.EffectiveClusterType(Describe(ClusterType.Standalone, ServerType.Unknown)).ShouldBe(ClusterType.Standalone);
}
