namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// AI practices that are prohibited under Article 5 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 5(1) establishes a list of AI practices that are considered unacceptable
/// due to their potential to violate fundamental rights. These practices are prohibited
/// from being placed on the market, put into service, or used in the Union.
/// </para>
/// <para>
/// The original prohibitions became effective on 2 February 2025. The points added by
/// Regulation (EU) 2026/1744, Art. 5(1)(ba) and (bb), apply from 2 December 2026. This enum
/// has a member for every point of Art. 5(1) as amended; use
/// <see cref="ProhibitedPracticeExtensions"/> to read the point of a member. Violations are
/// subject to administrative fines of up to EUR 35 million or 7% of total worldwide annual
/// turnover (Art. 99(3)).
/// </para>
/// </remarks>
public enum ProhibitedPractice
{
    /// <summary>
    /// AI-based social scoring by public authorities or on their behalf.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(c). Evaluation or classification of natural persons based on their social
    /// behaviour or known/predicted personal or personality characteristics, where the social
    /// score leads to detrimental or unfavourable treatment in social contexts unrelated to
    /// the contexts in which the data was originally generated or collected.
    /// </remarks>
    SocialScoring = 0,

    /// <summary>
    /// Real-time remote biometric identification in publicly accessible spaces for law enforcement.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(h). Prohibited except under narrowly defined exceptions in Art. 5(2):
    /// targeted search for specific victims, prevention of imminent threat to life or terrorist
    /// attack, and identification of suspects of specific serious criminal offences.
    /// </remarks>
    RealTimeBiometricPublicSpaces = 1,

    /// <summary>
    /// Emotion recognition in the workplace.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(f). AI systems that infer emotions of natural persons in the areas of
    /// workplace, except where the AI system is intended to be placed on the market for
    /// medical or safety reasons.
    /// </remarks>
    EmotionRecognitionWorkplace = 2,

    /// <summary>
    /// Emotion recognition in educational institutions.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(f). AI systems that infer emotions of natural persons in educational
    /// institutions, except where the AI system is intended to be placed on the market
    /// for medical or safety reasons.
    /// </remarks>
    EmotionRecognitionEducation = 3,

    /// <summary>
    /// Untargeted scraping of facial images from the internet or CCTV footage.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(e). Creating or expanding facial recognition databases through the
    /// untargeted scraping of facial images from the internet or from CCTV footage.
    /// </remarks>
    UntargetedFacialScraping = 4,

    /// <summary>
    /// AI systems used for predictive policing based solely on profiling.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(d). AI systems used to make risk assessments of natural persons in order to
    /// assess or predict the risk of a natural person committing a criminal offence, based
    /// solely on the profiling of a natural person or on assessing their personality traits
    /// and characteristics. This does not apply to AI systems used to support human assessment
    /// based on objective and verifiable facts directly linked to a criminal activity.
    /// </remarks>
    PredictivePolicing = 5,

    /// <summary>
    /// Biometric categorisation systems that categorise individually on the basis of sensitive data.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(g). AI systems that categorise individually natural persons based on their
    /// biometric data to deduce or infer their race, political opinions, trade union membership,
    /// religious or philosophical beliefs, sex life, or sexual orientation. Exceptions apply
    /// for labelling or filtering in the context of law enforcement.
    /// </remarks>
    BiometricCategorisation = 6,

    /// <summary>
    /// Subliminal techniques or manipulative/deceptive practices that distort behaviour.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(a). AI systems that deploy subliminal techniques beyond a person's
    /// consciousness or purposefully manipulative or deceptive techniques, with the objective
    /// or the effect of materially distorting the behaviour of a person or group, causing
    /// or likely to cause significant harm.
    /// </remarks>
    SubliminalManipulation = 7,

    /// <summary>
    /// Exploiting vulnerabilities of a person or group due to age, disability or a specific social or economic situation.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(b). AI systems that exploit the vulnerabilities of a natural person or a
    /// specific group of persons due to their age, disability or a specific social or economic
    /// situation, with the objective or the effect of materially distorting their behaviour in
    /// a way that causes or is reasonably likely to cause significant harm.
    /// </remarks>
    VulnerabilityExploitation = 8,

    /// <summary>
    /// Generating or manipulating intimate or sexually explicit content of an identifiable person without consent.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(ba). AI systems that generate or manipulate intimate or sexually explicit
    /// images, video or audio of an identifiable natural person without that person's consent
    /// (non-consensual intimate deepfakes, often called "nudification").
    /// Added by Regulation (EU) 2026/1744; applies from 2 December 2026.
    /// </remarks>
    NonConsensualIntimateDeepfakes = 9,

    /// <summary>
    /// Generating or manipulating child sexual abuse material.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(bb). AI systems that generate or manipulate child sexual abuse material.
    /// Added by Regulation (EU) 2026/1744; applies from 2 December 2026.
    /// </remarks>
    ChildSexualAbuseMaterial = 10
}
