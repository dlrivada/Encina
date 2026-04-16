// Fix regression: Revert .Count().ShouldBe( -> .Count.ShouldBe( for specific files
// that use LanguageExt Arr<T> which has .Count as a property, not a LINQ extension method.
// Also fix similar Count patterns for Arr<T> usage.

var filesToFix = new[]
{
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\MySQL\Repository\SpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\MySQL\Tenancy\TenantAwareSpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\PostgreSQL\Repository\SpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\PostgreSQL\Tenancy\TenantAwareSpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\SqlServer\Repository\QuerySpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\SqlServer\Repository\SpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\ADO\SqlServer\Tenancy\TenantAwareSpecificationSqlBuilderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Compliance\Anonymization\DefaultTokenizerTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Compliance\Anonymization\InMemoryAnonymizationAuditStoreTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Compliance\Anonymization\InMemoryKeyProviderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Compliance\Anonymization\InMemoryTokenMappingStoreTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Compliance\PrivacyByDesign\InMemoryPurposeRegistryTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Core\Sharding\Aggregation\AggregationResultTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Core\Sharding\ShardTopologyTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\EntityFrameworkCore\Caching\CachedDataReaderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Marten\Versioning\EventUpcasterRegistryTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Messaging\RedisPubSub\RedisPubSubMessagePublisherTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Messaging\ScatterGather\ScatterExecutionResultTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Messaging\Services\ConnectionWarmupHostedServiceTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Evaluation\ConditionEvaluatorTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Functions\BagFunctionsTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Functions\HigherOrderFunctionsTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\AntiTampering\InMemoryKeyProviderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit\InMemoryAuditStoreTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit\PagedResultTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit\ReadAudit\InMemoryReadAuditStoreTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Encryption\InMemoryKeyProviderTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Security\PII\PIIMaskerTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Tenancy\AspNetCore\TenantResolverChainTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Tenancy\InMemoryTenantStoreTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Tenancy\ServiceCollectionExtensionsTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Testing\Base\Modules\IntegrationEventCollectorTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Testing\Base\Modules\ModuleTestFixtureTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Testing\Modules\IntegrationEventCollectorTests.cs",
    @"D:\Proyectos\Encina\tests\Encina.UnitTests\Testing\Modules\ModuleTestFixtureTests.cs",
};

// Patterns to revert: .Count().Should* -> .Count.Should*
var patterns = new (string Old, string New)[]
{
    (".Count().ShouldBe(", ".Count.ShouldBe("),
    (".Count().ShouldBeGreaterThan(", ".Count.ShouldBeGreaterThan("),
    (".Count().ShouldBeGreaterThanOrEqualTo(", ".Count.ShouldBeGreaterThanOrEqualTo("),
    (".Count().ShouldBeLessThan(", ".Count.ShouldBeLessThan("),
    (".Count().ShouldBeLessThanOrEqualTo(", ".Count.ShouldBeLessThanOrEqualTo("),
    (".Count().ShouldNotBe(", ".Count.ShouldNotBe("),
};

int totalFixed = 0;
int totalFiles = 0;

foreach (var file in filesToFix)
{
    if (!File.Exists(file))
    {
        Console.WriteLine($"File not found: {file}");
        continue;
    }

    var content = File.ReadAllText(file);
    var original = content;

    foreach (var (old, @new) in patterns)
    {
        content = content.Replace(old, @new);
    }

    if (content != original)
    {
        File.WriteAllText(file, content);

        int count = 0;
        foreach (var (old, _) in patterns)
        {
            int idx = 0;
            while ((idx = original.IndexOf(old, idx, StringComparison.Ordinal)) >= 0)
            {
                count++;
                idx += old.Length;
            }
        }
        Console.WriteLine($"Fixed {count} in: {Path.GetFileName(file)}");
        totalFiles++;
        totalFixed += count;
    }
}

Console.WriteLine($"\nTotal: {totalFixed} replacements in {totalFiles} files");
