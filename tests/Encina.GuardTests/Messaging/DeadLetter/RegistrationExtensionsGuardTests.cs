using Encina.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.GuardTests.Messaging.DeadLetter;

/// <summary>
/// Null guards of the tenancy and fake-store registration extensions touched by #583.
/// </summary>
[Trait("Category", "Guard")]
public sealed class RegistrationExtensionsGuardTests
{
    [Fact]
    public void AddEncinaTenancy_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaTenancy())
            .ParamName.ShouldBe("services");

    [Fact]
    public void AddFakeDeadLetterStore_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeDeadLetterStore());

    [Fact]
    public void AddFakeOutboxStore_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeOutboxStore());

    [Fact]
    public void AddFakeInboxStore_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeInboxStore());

    [Fact]
    public void AddFakeSagaStore_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeSagaStore());

    [Fact]
    public void AddFakeScheduledMessageStore_NullServices_Throws()
        => Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeScheduledMessageStore());
}
