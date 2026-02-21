namespace Encina.Security.PII;

/// <summary>
/// Identifies the type of personally identifiable information (PII) for automatic masking.
/// </summary>
/// <remarks>
/// <para>
/// Each PII type has a corresponding default masking strategy that determines how the value
/// is redacted. For example, <see cref="Email"/> masks the local part while preserving the domain,
/// and <see cref="CreditCard"/> reveals only the last four digits.
/// </para>
/// <para>
/// Use <see cref="Custom"/> when none of the predefined types match and you need to supply
/// a custom regex pattern via <see cref="Attributes.PIIAttribute.Pattern"/>.
/// </para>
/// </remarks>
public enum PIIType
{
    /// <summary>
    /// Email address (e.g., <c>j***@example.com</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy preserves the domain and masks the local part,
    /// retaining the first character for identification.
    /// </remarks>
    Email = 0,

    /// <summary>
    /// Phone number (e.g., <c>***-***-1234</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy reveals the last four digits for partial identification.
    /// </remarks>
    Phone,

    /// <summary>
    /// Credit card number (e.g., <c>****-****-****-1234</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy reveals the last four digits, following PCI-DSS guidelines
    /// for acceptable display of card numbers.
    /// </remarks>
    CreditCard,

    /// <summary>
    /// Social Security Number or national ID (e.g., <c>***-**-1234</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy reveals the last four digits for partial identification.
    /// </remarks>
    SSN,

    /// <summary>
    /// Person's name (e.g., <c>J*** D**</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy preserves the first character of each name part
    /// and masks the remainder.
    /// </remarks>
    Name,

    /// <summary>
    /// Physical or mailing address (e.g., <c>*** Main St, City</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy masks the street number and name while preserving
    /// the city and country for general location context.
    /// </remarks>
    Address,

    /// <summary>
    /// Date of birth (e.g., <c>**/**/1990</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy preserves the year while masking the month and day.
    /// </remarks>
    DateOfBirth,

    /// <summary>
    /// IP address (e.g., <c>192.168.*.*</c>).
    /// </summary>
    /// <remarks>
    /// Default strategy preserves the network portion and masks the host portion,
    /// maintaining subnet-level identification without revealing the specific host.
    /// </remarks>
    IPAddress,

    /// <summary>
    /// Custom PII type requiring a user-supplied pattern.
    /// </summary>
    /// <remarks>
    /// Use this type when none of the predefined types match. A custom regex pattern
    /// must be provided via <see cref="Attributes.PIIAttribute.Pattern"/>.
    /// </remarks>
    Custom
}
