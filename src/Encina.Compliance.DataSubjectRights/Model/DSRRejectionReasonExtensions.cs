namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Extension members for <see cref="DSRRejectionReason"/>.
/// </summary>
public static class DSRRejectionReasonExtensions
{
    extension(DSRRejectionReason reason)
    {
        /// <summary>
        /// Gets a value indicating whether the reason rests on draft law rather than current law.
        /// </summary>
        /// <remarks>
        /// <c>true</c> only for <see cref="DSRRejectionReason.AbusiveIntent"/> (COM(2025) 837).
        /// </remarks>
        public bool IsDraftLawGround => reason is DSRRejectionReason.AbusiveIntent;

        /// <summary>
        /// Gets the legal basis of the reason as citation text.
        /// </summary>
        /// <remarks>Returns an empty string for a value that is not a defined member.</remarks>
        public string LegalBasis => reason switch
        {
            DSRRejectionReason.ManifestlyUnfounded => "GDPR Art. 12(5)",
            DSRRejectionReason.Excessive => "GDPR Art. 12(5)",
            DSRRejectionReason.IdentityNotVerifiable => "GDPR Art. 12(2) with Art. 11(2)",
            DSRRejectionReason.RestrictedByLaw => "GDPR Art. 23",
            DSRRejectionReason.RightNotApplicable => "GDPR Art. 17(3), 20(1) or 21(1)",
            DSRRejectionReason.Other => "GDPR Art. 12(4)",
            DSRRejectionReason.AbusiveIntent => "COM(2025) 837, draft GDPR Art. 12(5)",
            _ => string.Empty
        };
    }
}
