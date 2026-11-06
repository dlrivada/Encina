using Encina.DomainModeling;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore;

/// <summary>
/// Next page of <see cref="TestRepositoryEntity"/> rows after a last key, on a <see cref="string"/> or
/// <see cref="Guid"/> keyset sorted in either direction: the key types that have no comparison operators (#3201).
/// </summary>
public sealed class RepositoryEntityKeysetSpec : QuerySpecification<TestRepositoryEntity>
{
    private RepositoryEntityKeysetSpec()
    {
    }

    /// <summary>
    /// Rows ordered by name, after <paramref name="lastName"/>.
    /// </summary>
    public static RepositoryEntityKeysetSpec ByName(bool descending, string lastName)
    {
        var spec = new RepositoryEntityKeysetSpec();
        if (descending)
        {
            spec.ApplyOrderByDescending(e => e.Name);
        }
        else
        {
            spec.ApplyOrderBy(e => e.Name);
        }

        spec.ApplyKeysetPagination(e => e.Name, lastName, 10);
        return spec;
    }

    /// <summary>
    /// Rows ordered by id, after <paramref name="lastId"/>.
    /// </summary>
    public static RepositoryEntityKeysetSpec ById(bool descending, Guid lastId)
    {
        var spec = new RepositoryEntityKeysetSpec();
        if (descending)
        {
            spec.ApplyOrderByDescending(e => e.Id);
        }
        else
        {
            spec.ApplyOrderBy(e => e.Id);
        }

        spec.ApplyKeysetPagination(e => e.Id, lastId, 10);
        return spec;
    }
}
