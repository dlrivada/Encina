using Encina.Messaging;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging;

/// <summary>
/// Property-based tests for <see cref="SqlIdentifierValidator"/>.
/// Valid names are generated over the allowed alphabet, never from constant filler.
/// </summary>
[Trait("Category", "Property")]
public sealed class SqlIdentifierValidatorPropertyTests
{
    private const string FirstChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
    private const string RestChars = FirstChars + "0123456789";

    // Non-ASCII fragments are built from code points so the source stays free of invisible characters:
    // no-break space (A0), line separator (2028), fullwidth letters A and B (FF21, FF22), e acute (E9).
    private static readonly string[] InjectionFragments =
    [
        "; DROP TABLE x", "' OR '1'='1", "--", "/*", "*/", "\"", "`", "'", " ", ";", "@", "-",
        "]; --", "[a]]",
        char.ConvertFromUtf32(0x00A0),
        char.ConvertFromUtf32(0x2028),
        char.ConvertFromUtf32(0xFF21) + char.ConvertFromUtf32(0xFF22),
        char.ConvertFromUtf32(0x00E9),
    ];

    private static Gen<string> IdentifierGen(int maxLength) =>
        from first in Gen.Elements(FirstChars.ToCharArray())
        from rest in Gen.Elements(RestChars.ToCharArray()).ListOf(0, maxLength - 1)
        select first + new string(rest.ToArray());

    private static Gen<string> PartGen(int maxLength) =>
        from id in IdentifierGen(maxLength)
        from bracketed in Gen.Elements(true, false)
        select bracketed ? "[" + id + "]" : id;

    /// <summary>Every accepted shape: bare or bracketed parts, with or without schema, mixed forms.</summary>
    private static Gen<string> ValidNameGen() =>
        from schema in PartGen(40).OrNull(0.5)
        from name in PartGen(40)
        select schema is null ? name : schema + "." + name;

    private static Gen<char> ControlCharGen() =>
        Gen.OneOf(Gen.Choose(0x00, 0x1F), Gen.Choose(0x7F, 0x9F)).Select(i => (char)i);

    private static bool Accepted(string name) => SqlIdentifierValidator.IsValidTableName(name);

    private static bool Throws(string name)
    {
        try
        {
            SqlIdentifierValidator.ValidateTableName(name);
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private static bool RejectedByBoth(string name) => !Accepted(name) && Throws(name);

    [Property(MaxTest = 200)]
    public Property ValidShapes_AreAcceptedByBothMethods_AndReturnedUnchanged() =>
        Prop.ForAll(Arb.From(ValidNameGen()), name =>
            Accepted(name) && SqlIdentifierValidator.ValidateTableName(name) == name);

    [Property(MaxTest = 100)]
    public Property BareIdentifiers_FromOneToMaxLength_AreAccepted() =>
        Prop.ForAll(Arb.From(IdentifierGen(128)), name =>
            name.Length <= 128 && Accepted(name) && SqlIdentifierValidator.ValidateTableName(name) == name);

    [Property(MaxTest = 200)]
    public Property ControlCharacterInsertedAnywhere_IsRejectedByBoth() =>
        Prop.ForAll(
            Arb.From(ValidNameGen()),
            Arb.From(ControlCharGen()),
            Arb.From(Gen.Choose(0, 1000)),
            (name, control, seed) =>
            {
                var position = seed % (name.Length + 1);
                return RejectedByBoth(name.Insert(position, control.ToString()));
            });

    [Property(MaxTest = 100)]
    public Property ValidNameLongerThanMaxLength_IsRejectedByBoth() =>
        Prop.ForAll(
            Arb.From(IdentifierGen(300)),
            Arb.From(Gen.Choose(129, 300)),
            (id, length) =>
            {
                var name = id.PadRight(length, 'a');
                return name.Length >= 129 && RejectedByBoth(name);
            });

    [Property(MaxTest = 300)]
    public Property MaliciousFragmentAtAnyPosition_IsRejectedByBoth() =>
        Prop.ForAll(
            Arb.From(ValidNameGen()),
            Arb.From(Gen.Elements(InjectionFragments)),
            Arb.From(Gen.Choose(0, 1000)),
            (name, fragment, seed) =>
            {
                var position = seed % (name.Length + 1);
                return RejectedByBoth(name.Insert(position, fragment));
            });

    [Property(MaxTest = 300)]
    public Property ValidNameFollowedByMaliciousFragment_IsRejectedByBoth() =>
        Prop.ForAll(
            Arb.From(ValidNameGen()),
            Arb.From(Gen.Elements(InjectionFragments)),
            (name, fragment) => RejectedByBoth(name + fragment));

    [Property(MaxTest = 1000)]
    public Property AcceptedName_HasBalancedBracketsAndNoControlCharacter() =>
        Prop.ForAll(Arb.From(NoisyStringGen()), candidate =>
        {
            if (!Accepted(candidate))
            {
                return true;
            }

            return !candidate.Any(char.IsControl) && HasBalancedBrackets(candidate);
        });

    [Property(MaxTest = 1000)]
    public Property BothMethodsAgree_OnArbitraryStrings() =>
        Prop.ForAll(Arb.From(NoisyStringGen()), candidate =>
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return !Accepted(candidate) && Throws(candidate);
            }

            return Accepted(candidate) == !Throws(candidate);
        });

    [Property(MaxTest = 50)]
    public bool IsValidTableName_NullOrWhitespace_ReturnsFalse(bool useNull)
    {
        var input = useNull ? null : "   ";
        return !SqlIdentifierValidator.IsValidTableName(input);
    }

    /// <summary>Strings over an alphabet dense in identifier, bracket, dot and control characters.</summary>
    private static Gen<string> NoisyStringGen() =>
        Gen.Elements("abZ_09.[]\n\r\t\0 `\"-;".ToCharArray())
            .ListOf(0, 14)
            .Select(chars => new string(chars.ToArray()));

    private static bool HasBalancedBrackets(string value)
    {
        var open = false;
        foreach (var c in value)
        {
            if (c == '[')
            {
                if (open)
                    return false;
                open = true;
            }
            else if (c == ']')
            {
                if (!open)
                    return false;
                open = false;
            }
        }

        return !open;
    }
}
