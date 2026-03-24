using System.Reflection;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

namespace Encina.ContractTests.Marten.GDPR;

/// <summary>
/// Contract tests for <see cref="IDataErasureStrategy"/> as implemented by
/// <see cref="CryptoShredErasureStrategy"/>.
/// Verifies the erasure strategy follows the expected behavioral contract.
/// </summary>
[Trait("Category", "Contract")]
[Trait("Provider", "Marten")]
public sealed class IDataErasureStrategyContractTests : IDisposable
{
    private readonly InMemorySubjectKeyProvider _keyProvider;

    public IDataErasureStrategyContractTests()
    {
        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
    }

    #region Interface Contract

    [Fact]
    public void Contract_IDataErasureStrategy_HasEraseFieldAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IDataErasureStrategy);
        var methods = interfaceType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.ShouldContain(m => m.Name == "EraseFieldAsync",
            "IDataErasureStrategy must have EraseFieldAsync method");
    }

    [Fact]
    public void Contract_IDataErasureStrategy_EraseFieldAsync_ReturnsEither()
    {
        // Arrange
        var method = typeof(IDataErasureStrategy).GetMethod("EraseFieldAsync");

        // Assert
        method.ShouldNotBeNull();
        method.ReturnType.IsGenericType.ShouldBeTrue();
        method.ReturnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
    }

    [Fact]
    public void Contract_IDataErasureStrategy_EraseFieldAsync_AcceptsPersonalDataLocation()
    {
        // Arrange
        var method = typeof(IDataErasureStrategy).GetMethod("EraseFieldAsync");

        // Assert
        method.ShouldNotBeNull();
        var parameters = method.GetParameters();
        parameters[0].ParameterType.ShouldBe(typeof(PersonalDataLocation));
    }

    #endregion

    #region Implementation Verification

    [Fact]
    public void Contract_CryptoShredErasureStrategy_ImplementsInterface()
    {
        // Assert
        typeof(IDataErasureStrategy).IsAssignableFrom(typeof(CryptoShredErasureStrategy)).ShouldBeTrue();
    }

    [Fact]
    public void Contract_CryptoShredErasureStrategy_IsSealed()
    {
        // Assert
        typeof(CryptoShredErasureStrategy).IsSealed.ShouldBeTrue(
            "CryptoShredErasureStrategy should be sealed");
    }

    #endregion

    #region Behavioral Contract

    [Fact]
    public async Task Contract_EraseField_WithValidLocation_ReturnsSuccess()
    {
        // Arrange — create a subject key first
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-1");

        var sut = new CryptoShredErasureStrategy(
            _keyProvider,
            NullLogger<CryptoShredErasureStrategy>.Instance);

        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "user-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        // Act
        var result = await sut.EraseFieldAsync(location);

        // Assert
        result.IsRight.ShouldBeTrue(
            "Contract: EraseField with valid location should return success");
    }

    [Fact]
    public async Task Contract_EraseField_WhenSubjectAlreadyForgotten_ReturnsError()
    {
        // Arrange — create and forget a subject
        await _keyProvider.GetOrCreateSubjectKeyAsync("user-2");
        await _keyProvider.DeleteSubjectKeysAsync("user-2");

        var sut = new CryptoShredErasureStrategy(
            _keyProvider,
            NullLogger<CryptoShredErasureStrategy>.Instance);

        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "user-2",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        // Act
        var result = await sut.EraseFieldAsync(location);

        // Assert
        result.IsLeft.ShouldBeTrue(
            "Contract: EraseField should return error for already-forgotten subject");
    }

    [Fact]
    public async Task Contract_EraseField_DelegatesSubjectIdFromEntityId()
    {
        // Arrange — create a subject with specific ID
        await _keyProvider.GetOrCreateSubjectKeyAsync("subject-abc");

        var sut = new CryptoShredErasureStrategy(
            _keyProvider,
            NullLogger<CryptoShredErasureStrategy>.Instance);

        var location = new PersonalDataLocation
        {
            EntityType = typeof(string),
            EntityId = "subject-abc",
            FieldName = "Name",
            Category = PersonalDataCategory.Other,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        // Act
        await sut.EraseFieldAsync(location);

        // Assert — verify subject is now forgotten (proves it used the entity ID as subject ID)
        var forgottenResult = await _keyProvider.IsSubjectForgottenAsync("subject-abc");
        forgottenResult.IsRight.ShouldBeTrue();
        forgottenResult.IfRight(f => f.ShouldBeTrue(
            "Contract: EraseField should delete keys using entity ID as subject ID"));
    }

    #endregion
}
