# Integration Tests - MongoDB Reference Table Store

## Status: Deferred to Phase 6

## Justification

MongoDB reference table stores use document-based operations (`BulkWriteAsync` with `ReplaceOneModel`) and require:

1. MongoDB-specific entity serialization (BSON class maps)
2. Collection naming conventions different from SQL table names
3. The `ReferenceTableStoreMongoDB` constructs `IMongoCollection<T>` instances that need proper BSON mapping

This infrastructure will be implemented in Phase 6 when MongoDB sharding integration is fully tested.

### Adequate Coverage from Other Test Types
- **Unit Tests**: `ReferenceTableStoreMongoDB` logic tested via mocked `IMongoCollection<T>`
- **Contract Tests**: Interface compliance verified via reflection
- **ADO/Dapper Integration**: Core replication logic (hash, upsert, get-all) verified with 8 real database tests

## Related Files
- `src/Encina.MongoDB/Sharding/ReferenceTables/ReferenceTableStoreMongoDB.cs`
- `src/Encina.MongoDB/Sharding/ReferenceTables/ReferenceTableStoreFactoryMongoDB.cs`

## Date: 2026-02-15
## Issue: #639
