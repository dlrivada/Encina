using Xunit;

namespace Encina.Tests;

// Ensures tests in this collection do not run in parallel, avoiding shared ActivitySource cross-talk.
[CollectionDefinition("NonParallel", DisableParallelization = true)]
public sealed class NonParallelCollectionDefinition : ICollectionFixture<object>
{
}
