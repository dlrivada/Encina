using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.GDPR;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="DefaultDataSubjectRightsHandler"/> to verify null and invalid parameter handling.
/// </summary>
public class DefaultDataSubjectRightsHandlerGuardTests
{
    #region Constructor Guard Tests

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null requestStore.
    /// </summary>
    [Fact]
    public void Constructor_NullRequestStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            null!,
            Substitute.For<IDSRAuditStore>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            TimeProvider.System,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("requestStore");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null auditStore.
    /// </summary>
    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            null!,
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            TimeProvider.System,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("auditStore");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null locator.
    /// </summary>
    [Fact]
    public void Constructor_NullLocator_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            Substitute.For<IDSRAuditStore>(),
            null!,
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            TimeProvider.System,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("locator");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null erasureExecutor.
    /// </summary>
    [Fact]
    public void Constructor_NullErasureExecutor_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            Substitute.For<IDSRAuditStore>(),
            Substitute.For<IPersonalDataLocator>(),
            null!,
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            TimeProvider.System,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("erasureExecutor");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null portabilityExporter.
    /// </summary>
    [Fact]
    public void Constructor_NullPortabilityExporter_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            Substitute.For<IDSRAuditStore>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            null!,
            Substitute.For<IProcessingActivityRegistry>(),
            TimeProvider.System,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("portabilityExporter");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null processingActivityRegistry.
    /// </summary>
    [Fact]
    public void Constructor_NullProcessingActivityRegistry_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            Substitute.For<IDSRAuditStore>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            null!,
            TimeProvider.System,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processingActivityRegistry");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null timeProvider.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            Substitute.For<IDSRAuditStore>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            null!,
            NullLogger<DefaultDataSubjectRightsHandler>.Instance);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentNullException"/> for null logger.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDataSubjectRightsHandler(
            Substitute.For<IDSRRequestStore>(),
            Substitute.For<IDSRAuditStore>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            TimeProvider.System,
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
