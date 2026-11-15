Connection strings in `ShardReplicaConfig` are now masked in `ToString()` and excluded from JSON
serialisation via `[JsonIgnore]`, preventing accidental exposure in logs, diagnostics, and
serialised payloads. Fixes #3857.
