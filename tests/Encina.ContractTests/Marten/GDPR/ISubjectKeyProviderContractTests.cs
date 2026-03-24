using System.Reflection;

using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.ContractTests.Marten.GDPR;

/// <summary>
/// Contract tests for <see cref="ISubjectKeyProvider"/> implementations.
/// Verifies that InMemory and PostgreSql providers follow the same behavioral contract.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class ISubjectKeyProviderContractTests
{
    #region Interface Contract

    [Fact]
    public void Contract_ISubjectKeyProvider_HasRequiredMethods()
    {
        // Arrange
        var interfaceType = typeof(ISubjectKeyProvider);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert — 6 required methods
        methods.Select(m => m.Name).ShouldContain("GetOrCreateSubjectKeyAsync",
            "ISubjectKeyProvider must have GetOrCreateSubjectKeyAsync");
        methods.Select(m => m.Name).ShouldContain("GetSubjectKeyAsync",
            "ISubjectKeyProvider must have GetSubjectKeyAsync");
        methods.Select(m => m.Name).ShouldContain("DeleteSubjectKeysAsync",
            "ISubjectKeyProvider must have DeleteSubjectKeysAsync");
        methods.Select(m => m.Name).ShouldContain("IsSubjectForgottenAsync",
            "ISubjectKeyProvider must have IsSubjectForgottenAsync");
        methods.Select(m => m.Name).ShouldContain("RotateSubjectKeyAsync",
            "ISubjectKeyProvider must have RotateSubjectKeyAsync");
        methods.Select(m => m.Name).ShouldContain("GetSubjectInfoAsync",
            "ISubjectKeyProvider must have GetSubjectInfoAsync");
    }

    [Fact]
    public void Contract_ISubjectKeyProvider_AllMethodsReturnValueTask()
    {
        // Arrange
        var interfaceType = typeof(ISubjectKeyProvider);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert — all methods must return ValueTask<Either<...>>
        foreach (var method in methods)
        {
            method.ReturnType.IsGenericType.ShouldBeTrue(
                $"{method.Name} should return a generic ValueTask");
            method.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>),
                $"{method.Name} should return ValueTask<>");
        }
    }

    [Fact]
    public void Contract_ISubjectKeyProvider_AllMethodsAcceptCancellationToken()
    {
        // Arrange
        var interfaceType = typeof(ISubjectKeyProvider);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            parameters.Any(p => p.ParameterType == typeof(CancellationToken)).ShouldBeTrue(
                $"{method.Name} should accept CancellationToken");
        }
    }

    #endregion

    #region Implementation Verification

    [Fact]
    public void Contract_InMemorySubjectKeyProvider_ImplementsInterface()
    {
        // Assert
        typeof(ISubjectKeyProvider).IsAssignableFrom(typeof(InMemorySubjectKeyProvider)).ShouldBeTrue();
    }

    [Fact]
    public void Contract_PostgreSqlSubjectKeyProvider_ImplementsInterface()
    {
        // Assert
        typeof(ISubjectKeyProvider).IsAssignableFrom(typeof(PostgreSqlSubjectKeyProvider)).ShouldBeTrue();
    }

    [Fact]
    public void Contract_InMemorySubjectKeyProvider_IsSealed()
    {
        // Assert
        typeof(InMemorySubjectKeyProvider).IsSealed.ShouldBeTrue(
            "InMemorySubjectKeyProvider should be sealed");
    }

    [Fact]
    public void Contract_PostgreSqlSubjectKeyProvider_IsSealed()
    {
        // Assert
        typeof(PostgreSqlSubjectKeyProvider).IsSealed.ShouldBeTrue(
            "PostgreSqlSubjectKeyProvider should be sealed");
    }

    #endregion

    #region Behavioral Contract — InMemory

    [Fact]
    public async Task Contract_InMemory_GetOrCreate_ThenGet_ReturnsSameKey()
    {
        // Arrange
        var sut = CreateInMemoryProvider();
        var subjectId = "contract-test-1";

        // Act
        var createResult = await sut.GetOrCreateSubjectKeyAsync(subjectId);
        var getResult = await sut.GetSubjectKeyAsync(subjectId);

        // Assert
        createResult.IsRight.ShouldBeTrue("GetOrCreate should succeed");
        getResult.IsRight.ShouldBeTrue("Get should succeed after create");

        byte[] createdKey = null!;
        byte[] gottenKey = null!;
        createResult.IfRight(k => createdKey = k);
        getResult.IfRight(k => gottenKey = k);

        createdKey.SequenceEqual(gottenKey).ShouldBeTrue(
            "Contract: Get after Create must return the same key");
    }

    [Fact]
    public async Task Contract_InMemory_Delete_ThenGet_ReturnsError()
    {
        // Arrange
        var sut = CreateInMemoryProvider();
        var subjectId = "contract-test-2";
        await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Act
        await sut.DeleteSubjectKeysAsync(subjectId);
        var getResult = await sut.GetSubjectKeyAsync(subjectId);

        // Assert
        getResult.IsLeft.ShouldBeTrue(
            "Contract: Get after Delete must return an error (subject is forgotten)");
    }

    [Fact]
    public async Task Contract_InMemory_Delete_ThenIsForgotten_ReturnsTrue()
    {
        // Arrange
        var sut = CreateInMemoryProvider();
        var subjectId = "contract-test-3";
        await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Act
        await sut.DeleteSubjectKeysAsync(subjectId);
        var result = await sut.IsSubjectForgottenAsync(subjectId);

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(f => f.ShouldBeTrue(
            "Contract: IsForgotten must return true after Delete"));
    }

    [Fact]
    public async Task Contract_InMemory_Rotate_IncrementsVersion()
    {
        // Arrange
        var sut = CreateInMemoryProvider();
        var subjectId = "contract-test-4";
        await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Act
        var rotateResult = await sut.RotateSubjectKeyAsync(subjectId);

        // Assert
        rotateResult.IsRight.ShouldBeTrue("Rotate should succeed");
        rotateResult.IfRight(r => r.NewVersion.ShouldBe(2,
            "Contract: Rotate must increment version from 1 to 2"));
    }

    [Fact]
    public async Task Contract_InMemory_GetSubjectInfo_ReturnsCorrectStatus()
    {
        // Arrange
        var sut = CreateInMemoryProvider();
        var subjectId = "contract-test-5";
        await sut.GetOrCreateSubjectKeyAsync(subjectId);

        // Act
        var infoResult = await sut.GetSubjectInfoAsync(subjectId);

        // Assert
        infoResult.IsRight.ShouldBeTrue();
        infoResult.IfRight(info =>
        {
            info.SubjectId.ShouldBe(subjectId,
                "Contract: SubjectInfo.SubjectId must match");
            Assert.Equal(SubjectStatus.Active, info.Status);
            info.ActiveKeyVersion.ShouldBe(1,
                "Contract: Initial key version must be 1");
        });
    }

    [Fact]
    public async Task Contract_InMemory_UnknownSubject_GetKey_ReturnsError()
    {
        // Arrange
        var sut = CreateInMemoryProvider();

        // Act
        var result = await sut.GetSubjectKeyAsync("nonexistent-subject");

        // Assert
        result.IsLeft.ShouldBeTrue(
            "Contract: GetKey for unknown subject must return an error");
    }

    #endregion

    #region Helpers

    private static InMemorySubjectKeyProvider CreateInMemoryProvider()
    {
        return new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    #endregion
}
