namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Categories of AI systems as defined in Annex III of the EU AI Act,
/// used to determine whether a system qualifies as high-risk under Article 6(2).
/// </summary>
/// <remarks>
/// <para>
/// Article 6(2) states that an AI system is considered high-risk if it falls into
/// one of the areas listed in Annex III, taking into account the intended purpose and
/// the specific way it is used. Each value in this enum corresponds to an Annex III category.
/// </para>
/// <para>
/// Use <see cref="AISystemCategory"/> together with <see cref="AIRiskLevel"/> to fully
/// classify an AI system. The <c>IAIActClassifier</c> uses the category to determine
/// applicable requirements.
/// </para>
/// </remarks>
public enum AISystemCategory
{
    /// <summary>
    /// Biometric identification and categorisation of natural persons.
    /// </summary>
    /// <remarks>Annex III, point 1. Includes remote biometric identification systems (not real-time).</remarks>
    BiometricIdentification = 0,

    /// <summary>
    /// Management and operation of critical infrastructure.
    /// </summary>
    /// <remarks>
    /// Annex III, point 2. AI systems used as safety components in the management and operation
    /// of critical digital infrastructure, road traffic, and the supply of water, gas, heating, or electricity.
    /// </remarks>
    CriticalInfrastructure = 1,

    /// <summary>
    /// Education and vocational training.
    /// </summary>
    /// <remarks>
    /// Annex III, point 3. AI systems used to determine access to or admission to educational
    /// and vocational training institutions, to evaluate learning outcomes, to assess the
    /// appropriate level of education, or to monitor and detect prohibited behaviour during tests.
    /// </remarks>
    EducationVocationalTraining = 2,

    /// <summary>
    /// Employment, workers management, and access to self-employment.
    /// </summary>
    /// <remarks>
    /// Annex III, point 4. AI systems used for recruitment, selection, screening, interview evaluation,
    /// promotion/termination decisions, task allocation based on individual behaviour, and monitoring
    /// or evaluation of workers' performance and behaviour.
    /// </remarks>
    EmploymentWorkersManagement = 3,

    /// <summary>
    /// Access to and enjoyment of essential private services and essential public services and benefits.
    /// </summary>
    /// <remarks>
    /// Annex III, point 5. AI systems used by public authorities or on their behalf to evaluate
    /// eligibility for public assistance benefits and services, creditworthiness assessment,
    /// risk assessment and pricing for life and health insurance, and emergency services dispatch.
    /// </remarks>
    EssentialServices = 4,

    /// <summary>
    /// Law enforcement.
    /// </summary>
    /// <remarks>
    /// Annex III, point 6. AI systems used for individual risk assessment (recidivism, offending),
    /// polygraphs, evaluation of evidence reliability, profiling in criminal investigations,
    /// and crime analytics regarding natural persons.
    /// </remarks>
    LawEnforcement = 5,

    /// <summary>
    /// Migration, asylum, and border control management.
    /// </summary>
    /// <remarks>
    /// Annex III, point 7. AI systems used for polygraphs or similar tools, risk assessments
    /// regarding security or irregular migration risks, examination of applications for asylum/visa/residence,
    /// and detection/recognition/identification of natural persons in the context of migration.
    /// </remarks>
    MigrationAsylumBorderControl = 6,

    /// <summary>
    /// Administration of justice and democratic processes.
    /// </summary>
    /// <remarks>
    /// Annex III, point 8. AI systems used to assist judicial authorities in researching
    /// and interpreting facts and the law, and in applying the law to a concrete set of facts,
    /// or used to influence the outcome of an election or referendum or the voting behaviour of persons.
    /// </remarks>
    JusticeAdministration = 7,

    /// <summary>
    /// Emotion recognition systems.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(f) and Art. 50(3). Systems that infer emotions from biometric data. When used
    /// in the workplace or educational institutions, may be prohibited under Art. 5. Otherwise,
    /// subject to transparency obligations under Art. 50(3).
    /// </remarks>
    EmotionRecognition = 8,

    /// <summary>
    /// Social scoring systems.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(c). AI systems that evaluate or classify natural persons based on their social
    /// behaviour or known/predicted personal or personality characteristics, leading to detrimental
    /// or unfavourable treatment. Always classified as <see cref="AIRiskLevel.Prohibited"/>.
    /// </remarks>
    SocialScoring = 9,

    /// <summary>
    /// Real-time remote biometric identification in publicly accessible spaces.
    /// </summary>
    /// <remarks>
    /// Art. 5(1)(h). AI systems used for real-time remote biometric identification of natural
    /// persons in publicly accessible spaces for the purpose of law enforcement.
    /// Prohibited except under narrow exceptions (Art. 5(2)). Always classified
    /// as <see cref="AIRiskLevel.Prohibited"/> unless a specific exemption applies.
    /// </remarks>
    RealTimeBiometricPublic = 10,

    /// <summary>
    /// General-purpose AI systems or models.
    /// </summary>
    /// <remarks>
    /// Title V (Arts. 51-56). AI models with general-purpose capabilities, trained using
    /// self-supervision at scale, that can competently perform a wide range of distinct tasks.
    /// Subject to GPAI-specific obligations (Art. 53) and, if presenting systemic risks,
    /// additional obligations (Art. 55).
    /// </remarks>
    GeneralPurposeAI = 11
}
