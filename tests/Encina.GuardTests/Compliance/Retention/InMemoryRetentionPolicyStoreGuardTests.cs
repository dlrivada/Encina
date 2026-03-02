using Encina.Compliance.Retention.InMemory;
using Encina.Compliance.Retention.Model;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="InMemoryRetentionPolicyStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryRetentionPolicyStoreGuardTests
{
    private readonly InMemoryRetentionPolicyStore _store = new(NullLogger<InMemoryRetentionPolicyStore>.Instance);

    private static readonly RetentionPolicy ValidPolicy = RetentionPolicy.Create(
        dataCategory: "financial-records",
        retentionPeriod: TimeSpan.FromDays(365 * 7),
        autoDelete: false);

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryRetentionPolicyStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region CreateAsync Guards

    /// <summary>
    /// Verifies that CreateAsync throws ArgumentNullException when policy is null.
    /// </summary>
    [Fact]
    public async Task CreateAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var act = async () => await _store.CreateAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("policy");
    }

    #endregion

    #region GetByIdAsync Guards

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when policyId is null.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_NullPolicyId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("policyId");
    }

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when policyId is empty.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_EmptyPolicyId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("policyId");
    }

    /// <summary>
    /// Verifies that GetByIdAsync throws ArgumentException when policyId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByIdAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("policyId");
    }

    #endregion

    #region GetByCategoryAsync Guards

    /// <summary>
    /// Verifies that GetByCategoryAsync throws ArgumentException when dataCategory is null.
    /// </summary>
    [Fact]
    public async Task GetByCategoryAsync_NullDataCategory_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByCategoryAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that GetByCategoryAsync throws ArgumentException when dataCategory is empty.
    /// </summary>
    [Fact]
    public async Task GetByCategoryAsync_EmptyDataCategory_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByCategoryAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    /// <summary>
    /// Verifies that GetByCategoryAsync throws ArgumentException when dataCategory is whitespace.
    /// </summary>
    [Fact]
    public async Task GetByCategoryAsync_WhitespaceDataCategory_ThrowsArgumentException()
    {
        var act = async () => await _store.GetByCategoryAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("dataCategory");
    }

    #endregion

    #region UpdateAsync Guards

    /// <summary>
    /// Verifies that UpdateAsync throws ArgumentNullException when policy is null.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var act = async () => await _store.UpdateAsync(null!);
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("policy");
    }

    #endregion

    #region DeleteAsync Guards

    /// <summary>
    /// Verifies that DeleteAsync throws ArgumentException when policyId is null.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_NullPolicyId_ThrowsArgumentException()
    {
        var act = async () => await _store.DeleteAsync(null!);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("policyId");
    }

    /// <summary>
    /// Verifies that DeleteAsync throws ArgumentException when policyId is empty.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_EmptyPolicyId_ThrowsArgumentException()
    {
        var act = async () => await _store.DeleteAsync(string.Empty);
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("policyId");
    }

    /// <summary>
    /// Verifies that DeleteAsync throws ArgumentException when policyId is whitespace.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhitespacePolicyId_ThrowsArgumentException()
    {
        var act = async () => await _store.DeleteAsync(" ");
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.ShouldBe("policyId");
    }

    #endregion
}
