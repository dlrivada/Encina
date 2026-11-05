using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Encina.DomainModeling;
using Encina.EntityFrameworkCore.Repository;
using Encina.MongoDB.Repository;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

using AdoMySql = Encina.ADO.MySQL.Repository;
using AdoPostgreSql = Encina.ADO.PostgreSQL.Repository;
using AdoSqlServer = Encina.ADO.SqlServer.Repository;
using DapperMySql = Encina.Dapper.MySQL.Repository;
using DapperPostgreSql = Encina.Dapper.PostgreSQL.Repository;
using DapperSqlServer = Encina.Dapper.SqlServer.Repository;

namespace Encina.ContractTests.Database.Repository;

/// <summary>
/// Contract tests verifying that every repository provider pages a keyset the same way (#3154): in the
/// direction of the primary ordering, ascending when the specification has no ordering, with no keyset filter
/// on the first page, and never when the keyset property is not the primary ordering.
/// </summary>
/// <remarks>
/// Each provider is asked for the filter it builds: the SQL builders' WHERE clause, the EF Core query
/// expression and the rendered MongoDB filter. The contract is the comparison each one applies to the keyset.
/// </remarks>
[Trait("Category", "Contract")]
public sealed partial class KeysetPaginationContractTests
{
    private static readonly IReadOnlyDictionary<string, string> Columns = new Dictionary<string, string>
    {
        ["Id"] = "Id",
        ["Name"] = "Name",
        ["Total"] = "Total",
    };

    private static readonly KeysetContractOrder[] Orders =
    [
        new() { Id = 1, Name = "a", Total = 100m },
        new() { Id = 2, Name = "b", Total = 200m },
    ];

    private static readonly Dictionary<string, Func<QuerySpecification<KeysetContractOrder>, string>> Providers = new()
    {
        ["ADO.SqlServer"] = spec => new AdoSqlServer.SpecificationSqlBuilder<KeysetContractOrder>(Columns).BuildWhereClause(spec).WhereClause,
        ["ADO.PostgreSQL"] = spec => new AdoPostgreSql.SpecificationSqlBuilder<KeysetContractOrder>(Columns).BuildWhereClause(spec).WhereClause,
        ["ADO.MySQL"] = spec => new AdoMySql.SpecificationSqlBuilder<KeysetContractOrder>(Columns).BuildWhereClause(spec).WhereClause,
        ["Dapper.SqlServer"] = spec => new DapperSqlServer.SpecificationSqlBuilder<KeysetContractOrder>(Columns).BuildWhereClause(spec).WhereClause,
        ["Dapper.PostgreSQL"] = spec => new DapperPostgreSql.SpecificationSqlBuilder<KeysetContractOrder>(Columns).BuildWhereClause(spec).WhereClause,
        ["Dapper.MySQL"] = spec => new DapperMySql.SpecificationSqlBuilder<KeysetContractOrder>(Columns).BuildWhereClause(spec).WhereClause,
        ["EntityFrameworkCore"] = spec => SpecificationEvaluator.GetQuery(Orders.AsQueryable(), spec).Expression.ToString(),
        ["MongoDB"] = spec => RenderMongoFilter(new SpecificationFilterBuilder<KeysetContractOrder>().BuildFilter(spec)),
    };

    public static TheoryData<string> ProviderNames
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var provider in Providers.Keys)
            {
                data.Add(provider);
            }

            return data;
        }
    }

    public static TheoryData<string, bool, bool> RejectedShapes
    {
        get
        {
            // provider, keyset in a ThenByDescending (else ordered by another property only), first page
            var data = new TheoryData<string, bool, bool>();
            foreach (var provider in Providers.Keys)
            {
                data.Add(provider, true, true);
                data.Add(provider, true, false);
                data.Add(provider, false, true);
                data.Add(provider, false, false);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public void Contract_AllProviders_KeysetOrderedDescending_PagesBelowTheLastKey(string provider)
    {
        // Contract: a descending keyset's next page holds the keys below the last one
        var spec = new KeysetContractSpec().OrderedDescending(o => o.Total).KeysetOnTotal(lastTotal: 300m);

        KeysetComparison(provider, spec).ShouldBe("<");
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public void Contract_AllProviders_KeysetOrderedAscending_PagesAboveTheLastKey(string provider)
    {
        // Contract: an ascending keyset's next page holds the keys above the last one
        var spec = new KeysetContractSpec().Ordered(o => o.Total).KeysetOnTotal(lastTotal: 300m);

        KeysetComparison(provider, spec).ShouldBe(">");
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public void Contract_AllProviders_KeysetWithoutOrdering_PagesAscending(string provider)
    {
        // Contract: with no ordering at all, the keyset keeps the ascending filter
        var spec = new KeysetContractSpec().KeysetOnTotal(lastTotal: 300m);

        KeysetComparison(provider, spec).ShouldBe(">");
    }

    [Theory]
    [MemberData(nameof(ProviderNames))]
    public void Contract_AllProviders_FirstKeysetPage_HasNoKeysetFilter(string provider)
    {
        // Contract: without a last key, the first page is not filtered
        var spec = new KeysetContractSpec().OrderedDescending(o => o.Total).KeysetOnTotal(lastTotal: null);

        KeysetComparison(provider, spec).ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(RejectedShapes))]
    public void Contract_AllProviders_KeysetNotThePrimaryOrdering_IsRejected(string provider, bool keysetInThenBy, bool firstPage)
    {
        // Contract: a single-column keyset filter on any other ordering would skip or repeat rows, so every
        // provider rejects it, on the first page too
        var spec = new KeysetContractSpec().Ordered(o => o.Name);
        if (keysetInThenBy)
        {
            spec.ThenOrderedDescending(o => o.Total);
        }

        spec.KeysetOnTotal(firstPage ? null : 300m);

        Should.Throw<NotSupportedException>(() => Providers[provider](spec));
    }

    /// <summary>
    /// The comparison a provider applies to the keyset: <c>&lt;</c>, <c>&gt;</c>, or <see langword="null"/> without a keyset filter.
    /// </summary>
    private static string? KeysetComparison(string provider, QuerySpecification<KeysetContractOrder> spec)
    {
        var filter = Providers[provider](spec);
        if (provider == "MongoDB")
        {
            return filter.Contains("\"$lt\"", StringComparison.Ordinal) ? "<"
                : filter.Contains("\"$gt\"", StringComparison.Ordinal) ? ">"
                : null;
        }

        // SQL: [Total] < @p0, "Total" < @p0 or `Total` < @p0; EF Core expression: (o.Total < 300)
        var match = SqlOrExpressionComparison().Match(filter);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"Total[\]""`]?\s*([<>])\s*[@\d]")]
    private static partial Regex SqlOrExpressionComparison();

    private static string RenderMongoFilter(FilterDefinition<KeysetContractOrder> filter)
    {
        var registry = BsonSerializer.SerializerRegistry;
        return filter.Render(new RenderArgs<KeysetContractOrder>(registry.GetSerializer<KeysetContractOrder>(), registry)).ToJson();
    }

    private sealed class KeysetContractSpec : QuerySpecification<KeysetContractOrder>
    {
        public KeysetContractSpec Ordered(Expression<Func<KeysetContractOrder, object>> orderBy)
        {
            ApplyOrderBy(orderBy);
            return this;
        }

        public KeysetContractSpec OrderedDescending(Expression<Func<KeysetContractOrder, object>> orderBy)
        {
            ApplyOrderByDescending(orderBy);
            return this;
        }

        public KeysetContractSpec ThenOrderedDescending(Expression<Func<KeysetContractOrder, object>> thenBy)
        {
            ApplyThenByDescending(thenBy);
            return this;
        }

        public KeysetContractSpec KeysetOnTotal(decimal? lastTotal)
        {
            ApplyKeysetPagination(o => o.Total, lastTotal, 10);
            return this;
        }
    }
}

/// <summary>
/// Entity for the keyset pagination contract tests.
/// </summary>
public sealed class KeysetContractOrder
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Total { get; set; }
}
