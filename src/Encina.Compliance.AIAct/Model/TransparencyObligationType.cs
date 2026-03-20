namespace Encina.Compliance.AIAct.Model;

/// <summary>
/// Types of transparency obligations under Articles 13 and 50 of the EU AI Act.
/// </summary>
/// <remarks>
/// <para>
/// Article 50 establishes transparency obligations for providers and deployers of certain
/// AI systems, regardless of whether they are classified as high-risk. These obligations
/// ensure that natural persons are informed when they are interacting with AI or exposed
/// to AI-generated content.
/// </para>
/// <para>
/// Article 13 adds further transparency requirements specific to high-risk AI systems,
/// including instructions for use and interpretability of outputs.
/// </para>
/// </remarks>
public enum TransparencyObligationType
{
    /// <summary>
    /// AI-generated or AI-manipulated content (text, images, audio, video).
    /// </summary>
    /// <remarks>
    /// Art. 50(2). Providers of AI systems that generate synthetic audio, image, video, or text
    /// content must ensure that the outputs are marked in a machine-readable format and are
    /// detectable as artificially generated or manipulated.
    /// </remarks>
    AIGeneratedContent = 0,

    /// <summary>
    /// Deepfake content — AI-generated or manipulated images, audio, or video resembling real persons, objects, places, or events.
    /// </summary>
    /// <remarks>
    /// Art. 50(4). Deployers of AI systems that generate or manipulate image, audio, or video
    /// content constituting a deep fake must disclose that the content has been artificially
    /// generated or manipulated. Exceptions apply for content authorised by law for purposes of
    /// detection, prevention, investigation, or prosecution of criminal offences.
    /// </remarks>
    DeepfakeContent = 1,

    /// <summary>
    /// Emotion recognition systems.
    /// </summary>
    /// <remarks>
    /// Art. 50(3). Deployers of emotion recognition systems must inform natural persons
    /// exposed thereto of the operation of the system. This applies in all contexts where
    /// emotion recognition is permitted (i.e., not in workplace or education, which are
    /// prohibited under Art. 5(1)(f)).
    /// </remarks>
    EmotionRecognition = 2,

    /// <summary>
    /// Biometric categorisation systems.
    /// </summary>
    /// <remarks>
    /// Art. 50(3). Deployers of biometric categorisation systems must inform natural persons
    /// exposed thereto of the operation of the system. This applies to systems that categorise
    /// natural persons based on biometric data, excluding prohibited uses under Art. 5(1)(g).
    /// </remarks>
    BiometricCategorisation = 3,

    /// <summary>
    /// AI systems that interact directly with natural persons (e.g., chatbots, virtual assistants).
    /// </summary>
    /// <remarks>
    /// Art. 50(1). Providers must ensure that AI systems intended to interact directly with
    /// natural persons are designed and developed in such a way that the natural persons
    /// concerned are informed that they are interacting with an AI system, unless this is
    /// obvious from the circumstances and context of use.
    /// </remarks>
    ChatbotInteraction = 4
}
