using Encina.DomainModeling;
using NSubstitute;
using Shouldly;

namespace Encina.GuardTests.DomainModeling;

/// <summary>
/// Guard clause tests for <see cref="ImmutableAggregateHelper"/>.
/// </summary>
[Trait("Category", "Guard")]
public sealed class ImmutableAggregateHelperGuardTests
{
    #region PrepareForUpdate Guards

    [Fact]
    public void PrepareForUpdate_NullModified_ThrowsArgumentNullException()
    {
        // Arrange
        TestAggregateRoot modified = null!;
        var original = new TestAggregateRoot(Guid.NewGuid()) { Name = "Original" };
        var collector = Substitute.For<IDomainEventCollector>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector));
        ex.ParamName.ShouldBe("modified");
    }

    [Fact]
    public void PrepareForUpdate_NullOriginal_ThrowsArgumentNullException()
    {
        // Arrange
        var modified = new TestAggregateRoot(Guid.NewGuid()) { Name = "Modified" };
        TestAggregateRoot original = null!;
        var collector = Substitute.For<IDomainEventCollector>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector));
        ex.ParamName.ShouldBe("original");
    }

    [Fact]
    public void PrepareForUpdate_NullCollector_ThrowsArgumentNullException()
    {
        // Arrange
        var modified = new TestAggregateRoot(Guid.NewGuid()) { Name = "Modified" };
        var original = new TestAggregateRoot(Guid.NewGuid()) { Name = "Original" };
        IDomainEventCollector collector = null!;

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            ImmutableAggregateHelper.PrepareForUpdate(modified, original, collector));
        ex.ParamName.ShouldBe("collector");
    }

    #endregion

    #region Test Infrastructure

    private sealed record TestDomainEvent(string Message) : DomainEvent;

    private sealed class TestAggregateRoot : AggregateRoot<Guid>
    {
        public string Name { get; init; } = string.Empty;

        public TestAggregateRoot(Guid id) : base(id) { }

        public void RaiseEvent(IDomainEvent domainEvent) => RaiseDomainEvent(domainEvent);
    }

    #endregion
}
