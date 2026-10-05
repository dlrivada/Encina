using Encina.Security.PII;
using Encina.Security.PII.Abstractions;
using Encina.Security.PII.Attributes;
using Encina.Security.PII.Strategies;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.PropertyTests.Security.PII;

/// <summary>
/// Property-based tests for PII masking invariants.
/// Uses FsCheck to verify behavioral properties that must hold for all valid inputs.
/// </summary>
public sealed class MaskingPropertyTests
{
    #region MaskObject Invariants

    [Property(MaxTest = 100)]
    public bool MaskObject_NeverReturnsNull_ForNonNullInput(NonEmptyString email)
    {
        // Arrange
        var masker = CreateMasker();
        var dto = new TestDto { Email = email.Get.Replace("@", "") + "@test.com" };

        // Act
        var result = masker.MaskObject(dto);

        // Assert
        return result is not null;
    }

    [Property(MaxTest = 100)]
    public bool MaskByPattern_InvalidPattern_AlwaysMasksTheWholeValue(NonEmptyString value)
    {
        var masker = CreateMasker();

        var result = masker.Mask(value.Get, "[unclosed(group");

        return result == new string('*', value.Get.Length);
    }

    [Property(MaxTest = 100)]
    public bool MaskByPattern_AnyPattern_PreservesLength(NonEmptyString value, NonEmptyString pattern)
    {
        var masker = CreateMasker();

        // Whatever the pattern is (valid, invalid or slow), the result keeps the value length:
        // matches become mask characters and failures mask the whole value.
        return masker.Mask(value.Get, pattern.Get).Length == value.Get.Length;
    }

    #endregion

    #region Hash Mode Invariants

    [Property(MaxTest = 100)]
    public bool Hash_SameValueAndKey_IsDeterministic64CharHex(NonEmptyString value, NonEmptyString key)
    {
        var first = HashHelper.ComputeHash(value.Get, key.Get);
        var second = HashHelper.ComputeHash(value.Get, key.Get);

        return first == second && first.Length == 64 && first.All(char.IsAsciiHexDigitLower);
    }

    [Property(MaxTest = 100)]
    public bool Hash_DifferentKeys_ProduceDifferentHashes(NonEmptyString value, NonEmptyString keyA, NonEmptyString keyB)
    {
        if (keyA.Get == keyB.Get)
        {
            return true;
        }

        return HashHelper.ComputeHash(value.Get, keyA.Get) != HashHelper.ComputeHash(value.Get, keyB.Get);
    }

    [Property(MaxTest = 100)]
    public bool Hash_KeyedDiffersFromUnkeyed(NonEmptyString value, NonEmptyString key) =>
        HashHelper.ComputeHash(value.Get, key.Get) != HashHelper.ComputeHash(value.Get, null);

    #endregion

    #region Email Masking Invariants

    [Property(MaxTest = 100)]
    public bool EmailMasking_AlwaysContainsAtSymbol_WhenInputHasAt(NonEmptyString localPart)
    {
        // Arrange
        var sut = new EmailMaskingStrategy();
        var cleanLocal = localPart.Get.Replace("@", "");
        if (string.IsNullOrEmpty(cleanLocal))
        {
            cleanLocal = "a";
        }

        var email = cleanLocal + "@test.com";
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            VisibleCharactersStart = 1
        };

        // Act
        var result = sut.Apply(email, options);

        // Assert
        return result.Contains('@');
    }

    [Property(MaxTest = 100)]
    public bool EmailMasking_NeverExposesFullLocalPart(NonEmptyString localPart)
    {
        // Arrange
        var sut = new EmailMaskingStrategy();
        var cleanLocal = localPart.Get.Replace("@", "");
        if (cleanLocal.Length < 2)
        {
            cleanLocal = "ab";
        }

        var email = cleanLocal + "@test.com";
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            VisibleCharactersStart = 1
        };

        // Act
        var result = sut.Apply(email, options);

        // Assert — the masked part should contain '*'
        var maskedLocalPart = result[..result.IndexOf('@')];
        return maskedLocalPart.Contains('*');
    }

    #endregion

    #region Full Masking Invariants

    [Property(MaxTest = 100)]
    public bool FullMasking_PreservesLength_WhenPreserveLengthEnabled(NonEmptyString input)
    {
        // Arrange
        var sut = new FullMaskingStrategy();
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Full,
            MaskCharacter = '*',
            PreserveLength = true
        };

        // Act
        var result = sut.Apply(input.Get, options);

        // Assert
        return result.Length == input.Get.Length;
    }

    #endregion

    #region Phone Masking Invariants

    [Property(MaxTest = 100)]
    public bool PhoneMasking_PreservesLastFourDigits(PositiveInt areaCode, PositiveInt number)
    {
        // Arrange
        var sut = new PhoneMaskingStrategy();
        var phone = $"+1-{areaCode.Get % 1000:D3}-{number.Get % 10000:D4}-5678";
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Partial,
            MaskCharacter = '*',
            PreserveLength = true,
            VisibleCharactersEnd = 4
        };

        // Act
        var result = sut.Apply(phone, options);

        // Assert — last 4 digit characters should be "5678"
        var resultDigits = new string(result.Where(char.IsDigit).ToArray());
        return resultDigits.EndsWith("5678", StringComparison.Ordinal);
    }

    #endregion

    #region Idempotency Invariants

    [Property(MaxTest = 100)]
    public bool Masking_IsIdempotent_ApplyingTwiceGivesSameResult(NonEmptyString input)
    {
        // Arrange — use Full mode which is inherently idempotent:
        // replacing all characters with '*' and then masking again produces the same output.
        var sut = new FullMaskingStrategy();
        var options = new MaskingOptions
        {
            Mode = MaskingMode.Full,
            MaskCharacter = '*',
            PreserveLength = true
        };

        // Act
        var firstPass = sut.Apply(input.Get, options);
        var secondPass = sut.Apply(firstPass, options);

        // Assert
        return firstPass == secondPass;
    }

    #endregion

    #region Helpers

    private static PIIMasker CreateMasker() =>
        new(
            Options.Create(new PIIOptions()),
            NullLogger<PIIMasker>.Instance,
            new ServiceCollection().BuildServiceProvider());

    #endregion

    #region Test Types

    private sealed class TestDto
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}
