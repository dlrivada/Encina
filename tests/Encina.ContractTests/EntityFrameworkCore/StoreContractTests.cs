using System.Reflection;
using Encina.EntityFrameworkCore.Inbox;
using Encina.EntityFrameworkCore.Outbox;
using Encina.EntityFrameworkCore.Sagas;
using Encina.EntityFrameworkCore.Scheduling;
using Encina.Messaging.Inbox;
using Encina.Messaging.Outbox;
using Encina.Messaging.Sagas;
using Encina.Messaging.Scheduling;
using Shouldly;

namespace Encina.ContractTests.EntityFrameworkCore;

/// <summary>
/// Contract tests verifying that all EF Core store implementations follow consistent patterns:
/// implement the correct interfaces from Encina.Messaging and have constructors that accept DbContext.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Feature", "Stores")]
public sealed class StoreContractTests
{
    #region Interface Implementation Contract

    [Fact]
    public void OutboxStoreEF_ShouldImplementIOutboxStore()
    {
        typeof(IOutboxStore).IsAssignableFrom(typeof(OutboxStoreEF)).ShouldBeTrue(
            "OutboxStoreEF must implement IOutboxStore from Encina.Messaging");
    }

    [Fact]
    public void InboxStoreEF_ShouldImplementIInboxStore()
    {
        typeof(IInboxStore).IsAssignableFrom(typeof(InboxStoreEF)).ShouldBeTrue(
            "InboxStoreEF must implement IInboxStore from Encina.Messaging");
    }

    [Fact]
    public void SagaStoreEF_ShouldImplementISagaStore()
    {
        typeof(ISagaStore).IsAssignableFrom(typeof(SagaStoreEF)).ShouldBeTrue(
            "SagaStoreEF must implement ISagaStore from Encina.Messaging");
    }

    [Fact]
    public void ScheduledMessageStoreEF_ShouldImplementIScheduledMessageStore()
    {
        typeof(IScheduledMessageStore).IsAssignableFrom(typeof(ScheduledMessageStoreEF)).ShouldBeTrue(
            "ScheduledMessageStoreEF must implement IScheduledMessageStore from Encina.Messaging");
    }

    [Theory]
    [InlineData(typeof(OutboxStoreEF), typeof(IOutboxStore))]
    [InlineData(typeof(InboxStoreEF), typeof(IInboxStore))]
    [InlineData(typeof(SagaStoreEF), typeof(ISagaStore))]
    [InlineData(typeof(ScheduledMessageStoreEF), typeof(IScheduledMessageStore))]
    public void AllStores_ShouldImplementCorrectInterface(Type storeType, Type interfaceType)
    {
        interfaceType.IsAssignableFrom(storeType).ShouldBeTrue(
            $"{storeType.Name} must implement {interfaceType.Name}");
    }

    #endregion

    #region Sealed Class Contract

    [Theory]
    [InlineData(typeof(OutboxStoreEF))]
    [InlineData(typeof(InboxStoreEF))]
    [InlineData(typeof(SagaStoreEF))]
    [InlineData(typeof(ScheduledMessageStoreEF))]
    public void AllStores_ShouldBeSealed(Type storeType)
    {
        storeType.IsSealed.ShouldBeTrue(
            $"{storeType.Name} should be sealed for performance and safety");
    }

    #endregion

    #region Constructor Contract - DbContext Required

    [Theory]
    [InlineData(typeof(OutboxStoreEF))]
    [InlineData(typeof(InboxStoreEF))]
    [InlineData(typeof(SagaStoreEF))]
    [InlineData(typeof(ScheduledMessageStoreEF))]
    public void AllStores_ShouldHaveConstructorWithDbContext(Type storeType)
    {
        var constructors = storeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        constructors.Length.ShouldBeGreaterThan(0,
            $"{storeType.Name} should have at least one public constructor");

        var hasDbContextParam = constructors.Any(c =>
            c.GetParameters().Any(p =>
                p.ParameterType.FullName == "Microsoft.EntityFrameworkCore.DbContext"));

        hasDbContextParam.ShouldBeTrue(
            $"{storeType.Name} should have a constructor that accepts DbContext");
    }

    [Theory]
    [InlineData(typeof(OutboxStoreEF))]
    [InlineData(typeof(InboxStoreEF))]
    [InlineData(typeof(SagaStoreEF))]
    [InlineData(typeof(ScheduledMessageStoreEF))]
    public void AllStores_ConstructorFirstParameter_ShouldBeDbContext(Type storeType)
    {
        var constructor = storeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
        var firstParam = constructor.GetParameters()[0];

        firstParam.ParameterType.FullName.ShouldBe(
            "Microsoft.EntityFrameworkCore.DbContext",
            $"{storeType.Name} constructor's first parameter should be DbContext");
    }

    #endregion

    #region Naming Convention Contract

    [Theory]
    [InlineData(typeof(OutboxStoreEF), "Outbox")]
    [InlineData(typeof(InboxStoreEF), "Inbox")]
    [InlineData(typeof(SagaStoreEF), "Saga")]
    [InlineData(typeof(ScheduledMessageStoreEF), "ScheduledMessage")]
    public void AllStores_ShouldFollowNamingConvention(Type storeType, string expectedPrefix)
    {
        storeType.Name.ShouldBe($"{expectedPrefix}StoreEF",
            $"Store class should follow the naming convention: {expectedPrefix}StoreEF");
    }

    [Theory]
    [InlineData(typeof(OutboxStoreEF), "Encina.EntityFrameworkCore.Outbox")]
    [InlineData(typeof(InboxStoreEF), "Encina.EntityFrameworkCore.Inbox")]
    [InlineData(typeof(SagaStoreEF), "Encina.EntityFrameworkCore.Sagas")]
    [InlineData(typeof(ScheduledMessageStoreEF), "Encina.EntityFrameworkCore.Scheduling")]
    public void AllStores_ShouldBeInCorrectNamespace(Type storeType, string expectedNamespace)
    {
        storeType.Namespace.ShouldBe(expectedNamespace,
            $"{storeType.Name} should be in namespace {expectedNamespace}");
    }

    #endregion

    #region Store Interface Origin Contract

    [Fact]
    public void IOutboxStore_ShouldBeFromEncinaMessaging()
    {
        typeof(IOutboxStore).Assembly.GetName().Name.ShouldBe("Encina.Messaging",
            "IOutboxStore should come from Encina.Messaging assembly");
    }

    [Fact]
    public void IInboxStore_ShouldBeFromEncinaMessaging()
    {
        typeof(IInboxStore).Assembly.GetName().Name.ShouldBe("Encina.Messaging",
            "IInboxStore should come from Encina.Messaging assembly");
    }

    [Fact]
    public void ISagaStore_ShouldBeFromEncinaMessaging()
    {
        typeof(ISagaStore).Assembly.GetName().Name.ShouldBe("Encina.Messaging",
            "ISagaStore should come from Encina.Messaging assembly");
    }

    [Fact]
    public void IScheduledMessageStore_ShouldBeFromEncinaMessaging()
    {
        typeof(IScheduledMessageStore).Assembly.GetName().Name.ShouldBe("Encina.Messaging",
            "IScheduledMessageStore should come from Encina.Messaging assembly");
    }

    #endregion

    #region All Stores From Same Assembly

    [Fact]
    public void AllEFStores_ShouldBeFromSameAssembly()
    {
        var assemblies = new[]
        {
            typeof(OutboxStoreEF).Assembly,
            typeof(InboxStoreEF).Assembly,
            typeof(SagaStoreEF).Assembly,
            typeof(ScheduledMessageStoreEF).Assembly
        };

        var distinctAssemblies = assemblies.Distinct().ToArray();

        distinctAssemblies.Length.ShouldBe(1,
            "All EF Core store implementations should be in the same assembly (Encina.EntityFrameworkCore)");

        distinctAssemblies[0].GetName().Name.ShouldBe("Encina.EntityFrameworkCore",
            "All stores should be in the Encina.EntityFrameworkCore assembly");
    }

    #endregion

    #region TimeProvider Constructor Parameter Contract

    [Theory]
    [InlineData(typeof(OutboxStoreEF))]
    [InlineData(typeof(InboxStoreEF))]
    [InlineData(typeof(SagaStoreEF))]
    [InlineData(typeof(ScheduledMessageStoreEF))]
    public void AllStores_ShouldAcceptTimeProvider(Type storeType)
    {
        var constructor = storeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)[0];
        var hasTimeProvider = constructor.GetParameters()
            .Any(p => p.ParameterType == typeof(TimeProvider));

        hasTimeProvider.ShouldBeTrue(
            $"{storeType.Name} constructor should accept a TimeProvider parameter");
    }

    #endregion
}
