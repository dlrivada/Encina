using Encina.Security.ABAC.Providers;
using Encina.Testing.Identity;

using Shouldly;

namespace Encina.UnitTests.Security.ABAC;

/// <summary>
/// Unit tests for <see cref="DefaultAttributeProvider"/>: every category is empty, whatever the
/// caller (the PEP adds the built-in subject attributes itself, #1705).
/// </summary>
public sealed class DefaultAttributeProviderTests
{
    private readonly DefaultAttributeProvider _sut = new();

    [Fact]
    public async Task GetSubjectAttributesAsync_UserOrService_ReturnsAnEmptyDictionary()
    {
        (await _sut.GetSubjectAttributesAsync(TestIdentity.User("alice"))).ShouldBeEmpty();
        (await _sut.GetSubjectAttributesAsync(TestIdentity.Service("billing-job"))).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetResourceAttributesAsync_ReturnsAnEmptyDictionary()
    {
        (await _sut.GetResourceAttributesAsync(new { Id = 1 })).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetEnvironmentAttributesAsync_ReturnsAnEmptyDictionary()
    {
        (await _sut.GetEnvironmentAttributesAsync()).ShouldBeEmpty();
    }
}
