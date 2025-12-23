// Disable test parallelization for SQLite in-memory database tests
// SQLite in-memory databases are connection-specific and don't persist across connections
[assembly: Xunit.CollectionBehaviorAttribute(DisableTestParallelization = true)]
