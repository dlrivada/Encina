using Encina.Security.ABAC;
using Encina.Security.ABAC.Persistence;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC.Persistence;

/// <summary>
/// Unit tests for <see cref="PolicyEntityMapper"/>: verifies bidirectional mapping
/// between XACML domain models and their persistence entities, including timestamp
/// handling, metadata extraction, and deserialization error propagation.
/// </summary>
public sealed class PolicyEntityMapperTests
{
    private readonly DefaultPolicySerializer _serializer = new();
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExistingCreated = new(2026, 1, 15, 8, 0, 0, TimeSpan.Zero);

    private static FakeTimeProvider CreateFixedTimeProvider()
    {
        return new FakeTimeProvider(FixedNow);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PolicySet CreateMinimalPolicySet(string id = "ps-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Policies = [],
        PolicySets = [],
        Obligations = [],
        Advice = []
    };

    private static Policy CreateMinimalPolicy(string id = "p-1") => new()
    {
        Id = id,
        Target = null,
        Algorithm = CombiningAlgorithmId.DenyOverrides,
        Rules = [],
        Obligations = [],
        Advice = [],
        VariableDefinitions = []
    };

    // ── ToPolicySetEntity ───────────────────────────────────────────

    #region ToPolicySetEntity

    [Fact]
    public void ToPolicySetEntity_MinimalPolicySet_MapsId()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet("ps-test");

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Id.ShouldBe("ps-test");
    }

    [Fact]
    public void ToPolicySetEntity_WithVersion_MapsVersion()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { Version = "1.2.3" };

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Version.ShouldBe("1.2.3");
    }

    [Fact]
    public void ToPolicySetEntity_WithDescription_MapsDescription()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { Description = "Test description" };

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Description.ShouldBe("Test description");
    }

    [Fact]
    public void ToPolicySetEntity_MapsIsEnabled()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { IsEnabled = false };

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ToPolicySetEntity_MapsPriority()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet() with { Priority = 10 };

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Priority.ShouldBe(10);
    }

    [Fact]
    public void ToPolicySetEntity_ProducesNonEmptyPolicyJson()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.PolicyJson.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ToPolicySetEntity_Insert_SetsCreatedAtUtcFromTimeProvider()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.CreatedAtUtc.ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public void ToPolicySetEntity_Insert_SetsUpdatedAtUtcFromTimeProvider()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.UpdatedAtUtc.ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public void ToPolicySetEntity_Update_PreservesExistingCreatedAtUtc()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet();
        var existingEntity = new PolicySetEntity
        {
            Id = "ps-1",
            PolicyJson = "{}",
            CreatedAtUtc = ExistingCreated.UtcDateTime,
            UpdatedAtUtc = ExistingCreated.UtcDateTime
        };

        // Act
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider(), existingEntity);

        // Assert
        entity.CreatedAtUtc.ShouldBe(ExistingCreated.UtcDateTime);
        entity.UpdatedAtUtc.ShouldBe(FixedNow.UtcDateTime);
    }

    #endregion

    // ── ToPolicySet ─────────────────────────────────────────────────

    #region ToPolicySet

    [Fact]
    public void ToPolicySet_ValidEntity_ReturnsRight()
    {
        // Arrange
        var policySet = CreateMinimalPolicySet("ps-roundtrip");
        var entity = PolicyEntityMapper.ToPolicySetEntity(
            policySet, _serializer, CreateFixedTimeProvider());

        // Act
        var result = PolicyEntityMapper.ToPolicySet(entity, _serializer);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: ps => ps, Left: _ => null!);
        deserialized.Id.ShouldBe("ps-roundtrip");
    }

    [Fact]
    public void ToPolicySet_MalformedJson_ReturnsLeft()
    {
        // Arrange
        var entity = new PolicySetEntity
        {
            Id = "ps-bad",
            PolicyJson = "{invalid json}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = PolicyEntityMapper.ToPolicySet(entity, _serializer);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public void ToPolicySet_EmptyJson_ReturnsLeft()
    {
        // Arrange
        var entity = new PolicySetEntity
        {
            Id = "ps-empty",
            PolicyJson = "",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = PolicyEntityMapper.ToPolicySet(entity, _serializer);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    // ── ToPolicyEntity ──────────────────────────────────────────────

    #region ToPolicyEntity

    [Fact]
    public void ToPolicyEntity_MinimalPolicy_MapsId()
    {
        // Arrange
        var policy = CreateMinimalPolicy("p-test");

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Id.ShouldBe("p-test");
    }

    [Fact]
    public void ToPolicyEntity_WithVersion_MapsVersion()
    {
        // Arrange
        var policy = CreateMinimalPolicy() with { Version = "3.0" };

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Version.ShouldBe("3.0");
    }

    [Fact]
    public void ToPolicyEntity_WithDescription_MapsDescription()
    {
        // Arrange
        var policy = CreateMinimalPolicy() with { Description = "Test policy" };

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Description.ShouldBe("Test policy");
    }

    [Fact]
    public void ToPolicyEntity_MapsIsEnabled()
    {
        // Arrange
        var policy = CreateMinimalPolicy() with { IsEnabled = false };

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ToPolicyEntity_MapsPriority()
    {
        // Arrange
        var policy = CreateMinimalPolicy() with { Priority = 7 };

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.Priority.ShouldBe(7);
    }

    [Fact]
    public void ToPolicyEntity_Insert_SetsTimestamps()
    {
        // Arrange
        var policy = CreateMinimalPolicy();

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Assert
        entity.CreatedAtUtc.ShouldBe(FixedNow.UtcDateTime);
        entity.UpdatedAtUtc.ShouldBe(FixedNow.UtcDateTime);
    }

    [Fact]
    public void ToPolicyEntity_Update_PreservesExistingCreatedAtUtc()
    {
        // Arrange
        var policy = CreateMinimalPolicy();
        var existingEntity = new PolicyEntity
        {
            Id = "p-1",
            PolicyJson = "{}",
            CreatedAtUtc = ExistingCreated.UtcDateTime,
            UpdatedAtUtc = ExistingCreated.UtcDateTime
        };

        // Act
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider(), existingEntity);

        // Assert
        entity.CreatedAtUtc.ShouldBe(ExistingCreated.UtcDateTime);
        entity.UpdatedAtUtc.ShouldBe(FixedNow.UtcDateTime);
    }

    #endregion

    // ── ToPolicy ────────────────────────────────────────────────────

    #region ToPolicy

    [Fact]
    public void ToPolicy_ValidEntity_ReturnsRight()
    {
        // Arrange
        var policy = CreateMinimalPolicy("p-roundtrip");
        var entity = PolicyEntityMapper.ToPolicyEntity(
            policy, _serializer, CreateFixedTimeProvider());

        // Act
        var result = PolicyEntityMapper.ToPolicy(entity, _serializer);

        // Assert
        result.IsRight.ShouldBeTrue();
        var deserialized = result.Match(Right: p => p, Left: _ => null!);
        deserialized.Id.ShouldBe("p-roundtrip");
    }

    [Fact]
    public void ToPolicy_MalformedJson_ReturnsLeft()
    {
        // Arrange
        var entity = new PolicyEntity
        {
            Id = "p-bad",
            PolicyJson = "{invalid json}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        var result = PolicyEntityMapper.ToPolicy(entity, _serializer);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    // ── Guard Clauses ───────────────────────────────────────────────

    #region Guard Clauses

    [Fact]
    public void ToPolicySetEntity_NullPolicySet_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicySetEntity(
            null!, _serializer, CreateFixedTimeProvider());

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicySetEntity_NullSerializer_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicySetEntity(
            CreateMinimalPolicySet(), null!, CreateFixedTimeProvider());

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicySetEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicySetEntity(
            CreateMinimalPolicySet(), _serializer, null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicySet_NullEntity_ThrowsArgumentNullException()
    {
        Action act = () => { PolicyEntityMapper.ToPolicySet(null!, _serializer); };

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicySet_NullSerializer_ThrowsArgumentNullException()
    {
        var entity = new PolicySetEntity
        {
            Id = "ps-1",
            PolicyJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Action act = () => { PolicyEntityMapper.ToPolicySet(entity, null!); };

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicyEntity_NullPolicy_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicyEntity(
            null!, _serializer, CreateFixedTimeProvider());

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicyEntity_NullSerializer_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicyEntity(
            CreateMinimalPolicy(), null!, CreateFixedTimeProvider());

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicyEntity_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => PolicyEntityMapper.ToPolicyEntity(
            CreateMinimalPolicy(), _serializer, null!);

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicy_NullEntity_ThrowsArgumentNullException()
    {
        Action act = () => { PolicyEntityMapper.ToPolicy(null!, _serializer); };

        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ToPolicy_NullSerializer_ThrowsArgumentNullException()
    {
        var entity = new PolicyEntity
        {
            Id = "p-1",
            PolicyJson = "{}",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        Action act = () => { PolicyEntityMapper.ToPolicy(entity, null!); };

        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    // ── FakeTimeProvider ─────────────────────────────────────────────

    /// <summary>
    /// Simple deterministic <see cref="TimeProvider"/> for testing.
    /// </summary>
    private sealed class FakeTimeProvider(DateTimeOffset fixedUtcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedUtcNow;
    }
}
