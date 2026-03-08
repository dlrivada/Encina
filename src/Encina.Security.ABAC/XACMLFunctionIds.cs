namespace Encina.Security.ABAC;

/// <summary>
/// XACML 3.0 Appendix A — Standard function identifiers used in
/// <see cref="Apply.FunctionId"/> and <see cref="Match.FunctionId"/>.
/// </summary>
/// <remarks>
/// <para>
/// These C#-friendly identifiers map to the XACML 3.0 standard functions.
/// They are registered in the <see cref="IFunctionRegistry"/> at startup and
/// referenced by policy conditions.
/// </para>
/// <para>
/// Naming convention: <c>{type}-{operation}</c> (e.g., <c>string-equal</c>,
/// <c>integer-greater-than</c>). This matches the XACML URN local names without
/// the full <c>urn:oasis:names:tc:xacml:...</c> prefix.
/// </para>
/// </remarks>
public static class XACMLFunctionIds
{
    // ── Equality Functions ──────────────────────────────────────────

    /// <summary>String equality comparison.</summary>
    public const string StringEqual = "string-equal";

    /// <summary>Boolean equality comparison.</summary>
    public const string BooleanEqual = "boolean-equal";

    /// <summary>Integer equality comparison.</summary>
    public const string IntegerEqual = "integer-equal";

    /// <summary>Double equality comparison.</summary>
    public const string DoubleEqual = "double-equal";

    /// <summary>Date equality comparison.</summary>
    public const string DateEqual = "date-equal";

    /// <summary>DateTime equality comparison.</summary>
    public const string DateTimeEqual = "dateTime-equal";

    /// <summary>Time equality comparison.</summary>
    public const string TimeEqual = "time-equal";

    // ── Comparison Functions ────────────────────────────────────────

    /// <summary>Integer greater-than comparison.</summary>
    public const string IntegerGreaterThan = "integer-greater-than";

    /// <summary>Integer less-than comparison.</summary>
    public const string IntegerLessThan = "integer-less-than";

    /// <summary>Integer greater-than-or-equal comparison.</summary>
    public const string IntegerGreaterThanOrEqual = "integer-greater-than-or-equal";

    /// <summary>Integer less-than-or-equal comparison.</summary>
    public const string IntegerLessThanOrEqual = "integer-less-than-or-equal";

    /// <summary>Double greater-than comparison.</summary>
    public const string DoubleGreaterThan = "double-greater-than";

    /// <summary>Double less-than comparison.</summary>
    public const string DoubleLessThan = "double-less-than";

    /// <summary>Double greater-than-or-equal comparison.</summary>
    public const string DoubleGreaterThanOrEqual = "double-greater-than-or-equal";

    /// <summary>Double less-than-or-equal comparison.</summary>
    public const string DoubleLessThanOrEqual = "double-less-than-or-equal";

    /// <summary>String greater-than comparison (lexicographic).</summary>
    public const string StringGreaterThan = "string-greater-than";

    /// <summary>String less-than comparison (lexicographic).</summary>
    public const string StringLessThan = "string-less-than";

    /// <summary>String greater-than-or-equal comparison (lexicographic).</summary>
    public const string StringGreaterThanOrEqual = "string-greater-than-or-equal";

    /// <summary>String less-than-or-equal comparison (lexicographic).</summary>
    public const string StringLessThanOrEqual = "string-less-than-or-equal";

    /// <summary>Date greater-than comparison.</summary>
    public const string DateGreaterThan = "date-greater-than";

    /// <summary>Date less-than comparison.</summary>
    public const string DateLessThan = "date-less-than";

    /// <summary>Date greater-than-or-equal comparison.</summary>
    public const string DateGreaterThanOrEqual = "date-greater-than-or-equal";

    /// <summary>Date less-than-or-equal comparison.</summary>
    public const string DateLessThanOrEqual = "date-less-than-or-equal";

    /// <summary>DateTime greater-than comparison.</summary>
    public const string DateTimeGreaterThan = "dateTime-greater-than";

    /// <summary>DateTime less-than comparison.</summary>
    public const string DateTimeLessThan = "dateTime-less-than";

    /// <summary>DateTime greater-than-or-equal comparison.</summary>
    public const string DateTimeGreaterThanOrEqual = "dateTime-greater-than-or-equal";

    /// <summary>DateTime less-than-or-equal comparison.</summary>
    public const string DateTimeLessThanOrEqual = "dateTime-less-than-or-equal";

    /// <summary>Time greater-than comparison.</summary>
    public const string TimeGreaterThan = "time-greater-than";

    /// <summary>Time less-than comparison.</summary>
    public const string TimeLessThan = "time-less-than";

    /// <summary>Time greater-than-or-equal comparison.</summary>
    public const string TimeGreaterThanOrEqual = "time-greater-than-or-equal";

    /// <summary>Time less-than-or-equal comparison.</summary>
    public const string TimeLessThanOrEqual = "time-less-than-or-equal";

    // ── Arithmetic Functions ────────────────────────────────────────

    /// <summary>Integer addition.</summary>
    public const string IntegerAdd = "integer-add";

    /// <summary>Integer subtraction.</summary>
    public const string IntegerSubtract = "integer-subtract";

    /// <summary>Integer multiplication.</summary>
    public const string IntegerMultiply = "integer-multiply";

    /// <summary>Integer division.</summary>
    public const string IntegerDivide = "integer-divide";

    /// <summary>Integer modulus.</summary>
    public const string IntegerMod = "integer-mod";

    /// <summary>Integer absolute value.</summary>
    public const string IntegerAbs = "integer-abs";

    /// <summary>Double addition.</summary>
    public const string DoubleAdd = "double-add";

    /// <summary>Double subtraction.</summary>
    public const string DoubleSubtract = "double-subtract";

    /// <summary>Double multiplication.</summary>
    public const string DoubleMultiply = "double-multiply";

    /// <summary>Double division.</summary>
    public const string DoubleDivide = "double-divide";

    /// <summary>Double absolute value.</summary>
    public const string DoubleAbs = "double-abs";

    /// <summary>Round a double to the nearest integer.</summary>
    public const string Round = "round";

    /// <summary>Floor a double to the nearest lower integer.</summary>
    public const string Floor = "floor";

    // ── String Functions ────────────────────────────────────────────

    /// <summary>Concatenate two or more strings.</summary>
    public const string StringConcatenate = "string-concatenate";

    /// <summary>Check if a string starts with a prefix.</summary>
    public const string StringStartsWith = "string-starts-with";

    /// <summary>Check if a string ends with a suffix.</summary>
    public const string StringEndsWith = "string-ends-with";

    /// <summary>Check if a string contains a substring.</summary>
    public const string StringContains = "string-contains";

    /// <summary>Extract a substring by position and length.</summary>
    public const string StringSubstring = "string-substring";

    /// <summary>Normalize whitespace in a string.</summary>
    public const string StringNormalizeSpace = "string-normalize-space";

    /// <summary>Convert a string to lower case.</summary>
    public const string StringNormalizeToLowerCase = "string-normalize-to-lower-case";

    /// <summary>Get the length of a string.</summary>
    public const string StringLength = "string-length";

    // ── Logical Functions ───────────────────────────────────────────

    /// <summary>Logical AND (short-circuit evaluation).</summary>
    public const string And = "and";

    /// <summary>Logical OR (short-circuit evaluation).</summary>
    public const string Or = "or";

    /// <summary>Logical NOT (boolean inversion).</summary>
    public const string Not = "not";

    /// <summary>Returns true if at least N of the arguments are true.</summary>
    public const string NOf = "n-of";

    // ── Bag Functions ───────────────────────────────────────────────

    /// <summary>Extract the single value from a string bag (error if bag size != 1).</summary>
    public const string StringOneAndOnly = "string-one-and-only";

    /// <summary>Get the number of values in a string bag.</summary>
    public const string StringBagSize = "string-bag-size";

    /// <summary>Check if a value is in a string bag.</summary>
    public const string StringIsIn = "string-is-in";

    /// <summary>Create a string bag from values.</summary>
    public const string StringBag = "string-bag";

    /// <summary>Extract the single value from a boolean bag.</summary>
    public const string BooleanOneAndOnly = "boolean-one-and-only";

    /// <summary>Get the number of values in a boolean bag.</summary>
    public const string BooleanBagSize = "boolean-bag-size";

    /// <summary>Check if a value is in a boolean bag.</summary>
    public const string BooleanIsIn = "boolean-is-in";

    /// <summary>Create a boolean bag from values.</summary>
    public const string BooleanBag = "boolean-bag";

    /// <summary>Extract the single value from an integer bag.</summary>
    public const string IntegerOneAndOnly = "integer-one-and-only";

    /// <summary>Get the number of values in an integer bag.</summary>
    public const string IntegerBagSize = "integer-bag-size";

    /// <summary>Check if a value is in an integer bag.</summary>
    public const string IntegerIsIn = "integer-is-in";

    /// <summary>Create an integer bag from values.</summary>
    public const string IntegerBag = "integer-bag";

    /// <summary>Extract the single value from a double bag.</summary>
    public const string DoubleOneAndOnly = "double-one-and-only";

    /// <summary>Get the number of values in a double bag.</summary>
    public const string DoubleBagSize = "double-bag-size";

    /// <summary>Check if a value is in a double bag.</summary>
    public const string DoubleIsIn = "double-is-in";

    /// <summary>Create a double bag from values.</summary>
    public const string DoubleBag = "double-bag";

    /// <summary>Extract the single value from a date bag.</summary>
    public const string DateOneAndOnly = "date-one-and-only";

    /// <summary>Get the number of values in a date bag.</summary>
    public const string DateBagSize = "date-bag-size";

    /// <summary>Check if a value is in a date bag.</summary>
    public const string DateIsIn = "date-is-in";

    /// <summary>Create a date bag from values.</summary>
    public const string DateBag = "date-bag";

    /// <summary>Extract the single value from a dateTime bag.</summary>
    public const string DateTimeOneAndOnly = "dateTime-one-and-only";

    /// <summary>Get the number of values in a dateTime bag.</summary>
    public const string DateTimeBagSize = "dateTime-bag-size";

    /// <summary>Check if a value is in a dateTime bag.</summary>
    public const string DateTimeIsIn = "dateTime-is-in";

    /// <summary>Create a dateTime bag from values.</summary>
    public const string DateTimeBag = "dateTime-bag";

    /// <summary>Extract the single value from a time bag.</summary>
    public const string TimeOneAndOnly = "time-one-and-only";

    /// <summary>Get the number of values in a time bag.</summary>
    public const string TimeBagSize = "time-bag-size";

    /// <summary>Check if a value is in a time bag.</summary>
    public const string TimeIsIn = "time-is-in";

    /// <summary>Create a time bag from values.</summary>
    public const string TimeBag = "time-bag";

    /// <summary>Extract the single value from an anyURI bag.</summary>
    public const string AnyURIOneAndOnly = "anyURI-one-and-only";

    /// <summary>Get the number of values in an anyURI bag.</summary>
    public const string AnyURIBagSize = "anyURI-bag-size";

    /// <summary>Check if a value is in an anyURI bag.</summary>
    public const string AnyURIIsIn = "anyURI-is-in";

    /// <summary>Create an anyURI bag from values.</summary>
    public const string AnyURIBag = "anyURI-bag";

    // ── Set Functions ───────────────────────────────────────────────

    /// <summary>Compute the intersection of two string bags.</summary>
    public const string StringIntersection = "string-intersection";

    /// <summary>Compute the union of two string bags.</summary>
    public const string StringUnion = "string-union";

    /// <summary>Check if the first string bag is a subset of the second.</summary>
    public const string StringSubset = "string-subset";

    /// <summary>Check if the two string bags share at least one common member.</summary>
    public const string StringAtLeastOneMemberOf = "string-at-least-one-member-of";

    /// <summary>Check if two string bags contain the same values.</summary>
    public const string StringSetEquals = "string-set-equals";

    /// <summary>Compute the intersection of two integer bags.</summary>
    public const string IntegerIntersection = "integer-intersection";

    /// <summary>Compute the union of two integer bags.</summary>
    public const string IntegerUnion = "integer-union";

    /// <summary>Check if the first integer bag is a subset of the second.</summary>
    public const string IntegerSubset = "integer-subset";

    /// <summary>Check if the two integer bags share at least one common member.</summary>
    public const string IntegerAtLeastOneMemberOf = "integer-at-least-one-member-of";

    /// <summary>Check if two integer bags contain the same values.</summary>
    public const string IntegerSetEquals = "integer-set-equals";

    /// <summary>Compute the intersection of two double bags.</summary>
    public const string DoubleIntersection = "double-intersection";

    /// <summary>Compute the union of two double bags.</summary>
    public const string DoubleUnion = "double-union";

    /// <summary>Check if the first double bag is a subset of the second.</summary>
    public const string DoubleSubset = "double-subset";

    /// <summary>Check if the two double bags share at least one common member.</summary>
    public const string DoubleAtLeastOneMemberOf = "double-at-least-one-member-of";

    /// <summary>Check if two double bags contain the same values.</summary>
    public const string DoubleSetEquals = "double-set-equals";

    // ── Higher-Order Functions ──────────────────────────────────────

    /// <summary>Returns true if a function applied to any element in a bag returns true.</summary>
    public const string AnyOfFunc = "any-of";

    /// <summary>Returns true if a function applied to all elements in a bag returns true.</summary>
    public const string AllOfFunc = "all-of";

    /// <summary>Returns true if a function returns true for any pair from two bags.</summary>
    public const string AnyOfAny = "any-of-any";

    /// <summary>Returns true if for any element in the first bag, the function returns true for all elements in the second bag.</summary>
    public const string AllOfAny = "all-of-any";

    /// <summary>Returns true if the function returns true for all pairs from two bags.</summary>
    public const string AllOfAll = "all-of-all";

    /// <summary>Applies a function to each element in a bag and returns the resulting bag.</summary>
    public const string Map = "map";

    // ── Type Conversion Functions ───────────────────────────────────

    /// <summary>Convert an integer to its string representation.</summary>
    public const string StringFromInteger = "string-from-integer";

    /// <summary>Parse a string into an integer.</summary>
    public const string IntegerFromString = "integer-from-string";

    /// <summary>Parse a string into a double.</summary>
    public const string DoubleFromString = "double-from-string";

    /// <summary>Parse a string into a boolean.</summary>
    public const string BooleanFromString = "boolean-from-string";

    /// <summary>Convert a boolean to its string representation.</summary>
    public const string StringFromBoolean = "string-from-boolean";

    /// <summary>Convert a double to its string representation.</summary>
    public const string StringFromDouble = "string-from-double";

    /// <summary>Convert a dateTime to its string representation.</summary>
    public const string StringFromDateTime = "string-from-dateTime";

    // ── Regular Expression Functions ────────────────────────────────

    /// <summary>Check if a string matches a regular expression pattern.</summary>
    public const string StringRegexpMatch = "string-regexp-match";
}
