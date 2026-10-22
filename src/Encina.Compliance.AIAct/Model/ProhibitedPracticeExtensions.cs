namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Extension methods for <see cref="ProhibitedPractice"/>.
/// </summary>
public static class ProhibitedPracticeExtensions
{
    /// <summary>
    /// Gets the point of Article 5(1) of the EU AI Act that prohibits this practice,
    /// for example <c>5(1)(a)</c> or <c>5(1)(ba)</c>.
    /// </summary>
    /// <param name="practice">The prohibited practice.</param>
    /// <returns>The Article 5(1) point, such as <c>5(1)(f)</c>.</returns>
    /// <remarks>
    /// Both emotion-recognition members map to <c>5(1)(f)</c>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="practice"/> is not a defined <see cref="ProhibitedPractice"/> member.
    /// </exception>
    // crap-exempt: single-question switch — maps each practice to its Art. 5(1) point
    public static string GetArticlePoint(this ProhibitedPractice practice) => practice switch
    {
        ProhibitedPractice.SubliminalManipulation => "5(1)(a)",
        ProhibitedPractice.VulnerabilityExploitation => "5(1)(b)",
        ProhibitedPractice.NonConsensualIntimateDeepfakes => "5(1)(ba)",
        ProhibitedPractice.ChildSexualAbuseMaterial => "5(1)(bb)",
        ProhibitedPractice.SocialScoring => "5(1)(c)",
        ProhibitedPractice.PredictivePolicing => "5(1)(d)",
        ProhibitedPractice.UntargetedFacialScraping => "5(1)(e)",
        ProhibitedPractice.EmotionRecognitionWorkplace => "5(1)(f)",
        ProhibitedPractice.EmotionRecognitionEducation => "5(1)(f)",
        ProhibitedPractice.BiometricCategorisation => "5(1)(g)",
        ProhibitedPractice.RealTimeBiometricPublicSpaces => "5(1)(h)",
        _ => throw new ArgumentOutOfRangeException(nameof(practice), practice, "Undefined prohibited practice.")
    };
}
