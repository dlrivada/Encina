# ID Generation Scaling Guide

Machine ID allocation strategies, cluster operations, and migration considerations for production deployments.

---

## Machine ID Allocation (Snowflake)

Every Snowflake generator instance needs a unique `MachineId` within the bit range (default: 0-1023 with 10 shard bits). Collisions between machines produce duplicate IDs.

### Strategy 1: Configuration-Based

Assign machine IDs via environment variables or configuration files:

```csharp
services.AddEncinaIdGeneration(options =>
{
    options.UseSnowflake(sf =>
    {
        sf.MachineId = long.Parse(
            Environment.GetEnvironmentVariable("MACHINE_ID") ?? "0");
    });
});
```

**Pros**: Simple, no external dependencies. **Cons**: Manual management, risk of misconfiguration during scaling.

### Strategy 2: Kubernetes Ordinal

StatefulSet pods get ordinal indices (0, 1, 2, ...) that serve as natural machine IDs:

```yaml
# deployment.yaml
env:
  - name: MACHINE_ID
    valueFrom:
      fieldRef:
        fieldPath: metadata.labels['apps.kubernetes.io/pod-index']
```

```csharp
sf.MachineId = long.Parse(
    Environment.GetEnvironmentVariable("MACHINE_ID") ?? "0");
```

**Pros**: Automatic, unique per pod. **Cons**: Requires StatefulSet (not Deployment), limited to shard count.

### Strategy 3: Database Sequence

Use a shared database sequence to allocate machine IDs at startup:

```csharp
public class MachineIdAllocator(IDbConnection connection)
{
    public async Task<long> AllocateAsync()
    {
        // Atomically claim the next machine ID
        return await connection.ExecuteScalarAsync<long>(
            "UPDATE machine_id_registry SET next_id = next_id + 1 OUTPUT INSERTED.next_id - 1");
    }
}
```

**Pros**: Works with any deployment model. **Cons**: Requires database access at startup, single point of coordination.

### Strategy 4: Consul/etcd Service Discovery

Register machine IDs through a distributed KV store:

```csharp
// Pseudo-code: allocate from Consul KV with TTL
var machineId = await consul.AcquireLockAndClaimId(
    prefix: "encina/machine-ids/",
    ttl: TimeSpan.FromMinutes(5));

sf.MachineId = machineId;
```

**Pros**: Dynamic, handles node failures via TTL. **Cons**: External dependency, complexity.

---

## Snowflake Epoch Configuration

### Choosing an Epoch

The epoch determines when the timestamp bits start counting:

| Epoch | 41-bit Overflow | Remaining (from 2026) |
|-------|-----------------|-----------------------|
| 1970-01-01 (Unix) | 2039 | ~13 years |
| 2010-01-01 | 2079 | ~53 years |
| 2020-01-01 | 2089 | ~63 years |
| **2024-01-01** (default) | **2093** | **~67 years** |
| 2025-01-01 | 2094 | ~68 years |

**Recommendation**: Use your system's launch year. The default (2024) provides ~67 years from 2026.

### Epoch Migration

Changing the epoch after deployment breaks existing ID ordering. If you must change:

1. Stop all ID generation
2. Note the highest timestamp generated
3. Set new epoch such that new IDs will be strictly greater
4. Resume generation

---

## Clock Drift Management

### NTP Best Practices

Snowflake IDs depend on monotonically increasing system clocks:

- Run `ntpd` or `chrony` on all ID-generating nodes
- Configure NTP to slew (gradual adjustment) rather than step (instant jump)
- Monitor clock offset via `ntpstat` or metrics
- Set `ClockDriftToleranceMs` based on your NTP sync quality:

| Environment | Typical Drift | Recommended Tolerance |
|-------------|---------------|----------------------|
| Physical servers with GPS NTP | <1ms | 2ms |
| Cloud VMs (AWS, Azure, GCP) | 1-5ms | 5ms |
| VMs with live migration | 5-50ms | 50ms |
| Development/local | Variable | 100ms |

### Monitoring Clock Health

Use the health check to detect drift:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<IdGeneratorHealthCheck>(
        IdGeneratorHealthCheck.DefaultName,
        failureStatus: HealthStatus.Degraded);
```

---

## Capacity Planning

### Snowflake Throughput

With default configuration (12 sequence bits):

| Machines | IDs/ms (total) | IDs/sec (total) | Annual capacity |
|----------|----------------|-----------------|-----------------|
| 1 | 4,096 | 4,096,000 | ~129 trillion |
| 10 | 40,960 | 40,960,000 | ~1.29 quadrillion |
| 100 | 409,600 | 409,600,000 | ~12.9 quadrillion |
| 1,024 | 4,194,304 | 4,194,304,000 | ~132 quadrillion |

### Sequence Exhaustion

If a single machine generates >4,096 IDs in one millisecond:

1. The generator returns `SequenceExhausted` error
2. Options:
   - Increase `SequenceBits` (reduces `ShardBits` or `TimestampBits`)
   - Spread load across multiple machine IDs
   - Add a thin retry with 1ms delay

### Shard Count Growth

| Shard Bits | Max Shards | Trade-off |
|------------|-----------|-----------|
| 5 | 32 | More sequence capacity (131K/ms) |
| 8 | 256 | Balanced for medium clusters |
| 10 | 1,024 | Default; good for large clusters |
| 13 | 8,192 | Reduced sequence (128/ms) per shard |
| 16 | 65,536 | Minimal sequence (64/ms); use only if needed |

---

## Migration Considerations

### From Database Auto-Increment to Snowflake

1. Find the maximum existing ID value
2. Set `EpochStart` and `MachineId` such that generated IDs exceed the maximum
3. Or use a separate column for the new Snowflake IDs during transition

### From UUID v4 to UUIDv7

UUIDv7 is a drop-in replacement for `Guid` columns:

1. Replace `Guid.NewGuid()` calls with `UuidV7IdGenerator.Generate()`
2. Existing UUID v4 values remain valid (they just won't be time-ordered)
3. New UUIDv7 values sort after most existing v4 values (by chance, ~50%)
4. No schema changes required

### From String IDs to ULID

1. Ensure column width accommodates 26 characters
2. Existing string IDs remain valid if they don't conflict
3. New ULIDs sort lexicographically after most existing values

---

## Multi-Region Deployment

### Shard ID Assignment by Region

Partition the machine ID space by region to prevent collisions:

```csharp
// Region A: machines 0-255
// Region B: machines 256-511
// Region C: machines 512-767
// Reserve: 768-1023

var regionOffset = region switch
{
    "us-east" => 0,
    "eu-west" => 256,
    "ap-south" => 512,
    _ => 768
};

sf.MachineId = regionOffset + localMachineIndex;
```

### ShardPrefixed for Multi-Region

Use region as the shard prefix for immediate geographic routing:

```csharp
var id = shardPrefixed.Generate("us-east-01");
// Result: us-east-01:01ARZ3NDEKTSV4RRFFQ69G5FAV
```

---

## See Also

- [Configuration Guide](./id-generation-configuration.md) — Strategy selection and tuning
- [Feature Overview](../features/id-generation.md) — Architecture and API reference
- [ADR-011](../architecture/adr/011-id-generation-multi-strategy.md) — Design rationale
