using Encina.Compliance.DataSubjectRights;
using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Infrastructure.Marten.GDPR;

/// <summary>
/// Integration tests verifying that <see cref="CryptoShredErasureStrategy"/> works
/// correctly in the DSR (Data Subject Rights) workflow with <see cref="DefaultDataErasureExecutor"/>.
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class DSRIntegrationTests : IDisposable
{
    private readonly MartenFixture _fixture;
    private readonly InMemorySubjectKeyProvider _keyProvider;

    public DSRIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);
    }

    public void Dispose()
    {
        _keyProvider.Clear();
        CryptoShreddedPropertyCache.ClearCache();
    }

    [Fact]
    public async Task CryptoShredErasureStrategy_InDSRWorkflow_ErasesViaKeyDeletion()
    {
        // Arrange — create a key first (simulating a previously stored encrypted event)
        var subjectId = "dsr-user-1";
        var createResult = await _keyProvider.GetOrCreateSubjectKeyAsync(subjectId);
        createResult.IsRight.ShouldBeTrue("Key creation should succeed");

        // Build the DSR erasure executor with crypto-shred strategy
        var erasureStrategy = new CryptoShredErasureStrategy(
            _keyProvider,
            NullLogger<CryptoShredErasureStrategy>.Instance);

        var location = new PersonalDataLocation
        {
            EntityType = typeof(DSRTestEvent),
            EntityId = subjectId,
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        // Act — erase via the strategy
        var eraseResult = await erasureStrategy.EraseFieldAsync(location);

        // Assert — erasure succeeded
        eraseResult.IsRight.ShouldBeTrue("Erasure should succeed");

        // Verify subject is now forgotten
        var forgottenResult = await _keyProvider.IsSubjectForgottenAsync(subjectId);
        forgottenResult.IsRight.ShouldBeTrue();
        forgottenResult.IfRight(f => f.ShouldBeTrue(
            "Subject should be forgotten after DSR erasure"));

        // Verify key is no longer accessible
        var keyResult = await _keyProvider.GetSubjectKeyAsync(subjectId);
        keyResult.IsLeft.ShouldBeTrue(
            "Key should not be retrievable after erasure");
    }

    [Fact]
    public async Task DefaultDataErasureExecutor_WithCryptoShredStrategy_CompletesWorkflow()
    {
        // Arrange — create a key and build the full DSR workflow
        var subjectId = "dsr-full-workflow";
        await _keyProvider.GetOrCreateSubjectKeyAsync(subjectId);

        var erasureStrategy = new CryptoShredErasureStrategy(
            _keyProvider,
            NullLogger<CryptoShredErasureStrategy>.Instance);

        // Create a locator that returns one location
        var locator = new StaticPersonalDataLocator(
        [
            new PersonalDataLocation
            {
                EntityType = typeof(DSRTestEvent),
                EntityId = subjectId,
                FieldName = "Email",
                Category = PersonalDataCategory.Contact,
                IsErasable = true,
                IsPortable = false,
                HasLegalRetention = false,
                CurrentValue = "user@example.com"
            }
        ]);

        var executor = new DefaultDataErasureExecutor(
            locator,
            erasureStrategy,
            NullLogger<DefaultDataErasureExecutor>.Instance);

        var scope = new ErasureScope { Reason = ErasureReason.ConsentWithdrawn };

        // Act
        var result = await executor.EraseAsync(subjectId, scope);

        // Assert
        result.IsRight.ShouldBeTrue("Full DSR erasure workflow should succeed");
        result.IfRight(erasureResult =>
        {
            erasureResult.FieldsErased.ShouldBeGreaterThanOrEqualTo(1,
                "At least one field should have been erased");
        });

        // Verify the subject is forgotten
        var forgottenResult = await _keyProvider.IsSubjectForgottenAsync(subjectId);
        forgottenResult.IsRight.ShouldBeTrue();
        forgottenResult.IfRight(f => f.ShouldBeTrue());
    }

    #region Test Helpers

    /// <summary>
    /// Simple locator that returns pre-configured locations for testing.
    /// </summary>
    private sealed class StaticPersonalDataLocator : IPersonalDataLocator
    {
        private readonly IReadOnlyList<PersonalDataLocation> _locations;

        public StaticPersonalDataLocator(IReadOnlyList<PersonalDataLocation> locations)
        {
            _locations = locations;
        }

        public ValueTask<LanguageExt.Either<EncinaError, IReadOnlyList<PersonalDataLocation>>> LocateAllDataAsync(
            string subjectId,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                LanguageExt.Prelude.Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(_locations));
        }
    }

    public class DSRTestEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}
