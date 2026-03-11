using Encina.Compliance.DPIA.Model;

using LanguageExt;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Default implementation of <see cref="IDPIATemplateProvider"/> with 7 built-in templates.
/// </summary>
/// <remarks>
/// <para>
/// Provides pre-configured DPIA templates covering the most common processing types
/// that require impact assessments under GDPR Article 35. Templates are stored in memory
/// and indexed by <see cref="DPIATemplate.ProcessingType"/> for O(1) lookup.
/// </para>
/// <para>
/// Built-in templates:
/// </para>
/// <list type="bullet">
/// <item><description><c>profiling</c> — Systematic profiling and scoring (Art. 35(3)(a)).</description></item>
/// <item><description><c>special-category</c> — Special category data processing (Art. 9).</description></item>
/// <item><description><c>public-monitoring</c> — Systematic monitoring of public areas (Art. 35(3)(c)).</description></item>
/// <item><description><c>ai-ml</c> — AI/ML-based processing and automated analysis.</description></item>
/// <item><description><c>biometric</c> — Biometric data processing for identification.</description></item>
/// <item><description><c>health-data</c> — Health and medical data processing.</description></item>
/// <item><description><c>general</c> — General-purpose fallback template.</description></item>
/// </list>
/// <para>
/// When the requested <c>processingType</c> is not found, the <c>general</c> template is returned
/// as a fallback. If neither is found, a <see cref="DPIAErrors.TemplateNotFound"/> error is returned.
/// </para>
/// </remarks>
public sealed class DefaultDPIATemplateProvider : IDPIATemplateProvider
{
    private readonly Dictionary<string, DPIATemplate> _templates;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDPIATemplateProvider"/> class
    /// with the 7 built-in templates.
    /// </summary>
    public DefaultDPIATemplateProvider()
    {
        _templates = BuildTemplates().ToDictionary(
            t => t.ProcessingType, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, DPIATemplate>> GetTemplateAsync(
        string processingType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processingType);

        if (_templates.TryGetValue(processingType, out var template))
        {
            return ValueTask.FromResult<Either<EncinaError, DPIATemplate>>(template);
        }

        // Fallback to the general template.
        if (_templates.TryGetValue("general", out var general))
        {
            return ValueTask.FromResult<Either<EncinaError, DPIATemplate>>(general);
        }

        return ValueTask.FromResult<Either<EncinaError, DPIATemplate>>(
            DPIAErrors.TemplateNotFound(processingType));
    }

    /// <inheritdoc />
    public ValueTask<Either<EncinaError, IReadOnlyList<DPIATemplate>>> GetAllTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var templates = _templates.Values.ToList();

        return ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIATemplate>>>(templates);
    }

    private static List<DPIATemplate> BuildTemplates() =>
    [
        CreateProfilingTemplate(),
        CreateSpecialCategoryTemplate(),
        CreatePublicMonitoringTemplate(),
        CreateAiMlTemplate(),
        CreateBiometricTemplate(),
        CreateHealthDataTemplate(),
        CreateGeneralTemplate(),
    ];

    // ── Template Builders ────────────────────────────────────────────────

    private static DPIATemplate CreateProfilingTemplate() => new()
    {
        Name = "Systematic Profiling Assessment",
        Description = "Template for assessing processing involving systematic and extensive evaluation of personal aspects, including profiling, per GDPR Article 35(3)(a).",
        ProcessingType = "profiling",
        Sections =
        [
            new DPIASection(
                "Purpose and Scope",
                "Describe the profiling purpose, data sources, and scope of personal aspects evaluated.",
                IsRequired: true,
                Questions:
                [
                    "What personal aspects are being evaluated or predicted?",
                    "What data sources feed into the profiling process?",
                    "What is the intended purpose of the profiling?",
                ]),
            new DPIASection(
                "Profiling Logic and Fairness",
                "Document the logic, algorithms, and fairness measures in the profiling system.",
                IsRequired: true,
                Questions:
                [
                    "What algorithms or models are used for profiling?",
                    "How are decisions derived from profile data?",
                    "What measures ensure accuracy and prevent discrimination?",
                ]),
            new DPIASection(
                "Rights and Impact Assessment",
                "Assess the impact on data subject rights and the availability of safeguards.",
                IsRequired: true,
                Questions:
                [
                    "Does profiling produce legal effects or similarly significant effects?",
                    "How can data subjects access, rectify, or contest their profile?",
                    "What human oversight mechanisms are in place?",
                ]),
            new DPIASection(
                "Risk Mitigation",
                "Identify residual risks and planned mitigation measures.",
                IsRequired: true,
                Questions:
                [
                    "What residual risks remain after applying safeguards?",
                    "What additional mitigation measures are planned?",
                ]),
        ],
        RiskCategories =
        [
            "Automated Decision-Making",
            "Systematic Profiling",
            "Individual Rights",
            "Bias and Discrimination",
        ],
        SuggestedMitigations =
        [
            "Implement meaningful human oversight of profiling decisions.",
            "Provide transparent information about profiling logic to data subjects.",
            "Offer opt-out mechanisms for non-essential profiling.",
            "Conduct regular accuracy reviews and bias audits.",
            "Implement automated bias detection in profiling algorithms.",
        ],
    };

    private static DPIATemplate CreateSpecialCategoryTemplate() => new()
    {
        Name = "Special Category Data Assessment",
        Description = "Template for assessing processing of special categories of personal data as defined in GDPR Article 9(1), including health, biometric, genetic, and other sensitive data.",
        ProcessingType = "special-category",
        Sections =
        [
            new DPIASection(
                "Data Classification",
                "Identify and classify the special categories of personal data being processed.",
                IsRequired: true,
                Questions:
                [
                    "Which special categories of data are processed (Art. 9(1))?",
                    "Is criminal conviction data also processed (Art. 10)?",
                    "What volume of special category data is involved?",
                ]),
            new DPIASection(
                "Legal Basis",
                "Document the legal basis for processing special category data under Article 9(2).",
                IsRequired: true,
                Questions:
                [
                    "What is the specific Article 9(2) exemption relied upon?",
                    "If relying on explicit consent, how is it obtained and recorded?",
                    "Are there additional national law requirements that apply?",
                ]),
            new DPIASection(
                "Security Measures",
                "Describe technical and organizational measures protecting special category data.",
                IsRequired: true,
                Questions:
                [
                    "What encryption methods protect data at rest and in transit?",
                    "How is access to special category data restricted?",
                    "Is pseudonymization or anonymization applied where feasible?",
                ]),
            new DPIASection(
                "Data Subject Rights",
                "Assess how data subject rights are facilitated for special category data.",
                IsRequired: true,
                Questions:
                [
                    "How can data subjects exercise their rights under Articles 15-22?",
                    "What is the process for handling data subject access requests?",
                ]),
        ],
        RiskCategories =
        [
            "Special Category Data",
            "Confidentiality",
            "Data Minimization",
            "Legal Basis Compliance",
        ],
        SuggestedMitigations =
        [
            "Apply encryption at rest and in transit for all special category data.",
            "Implement strict role-based access controls with audit logging.",
            "Use pseudonymization wherever processing purposes allow.",
            "Apply data minimization to collect only necessary special category data.",
            "Conduct regular access reviews and compliance audits.",
        ],
    };

    private static DPIATemplate CreatePublicMonitoringTemplate() => new()
    {
        Name = "Systematic Public Monitoring Assessment",
        Description = "Template for assessing systematic monitoring of publicly accessible areas on a large scale per GDPR Article 35(3)(c), including CCTV, Wi-Fi tracking, and location analytics.",
        ProcessingType = "public-monitoring",
        Sections =
        [
            new DPIASection(
                "Monitoring Scope",
                "Define the scope, technology, and geographic extent of the monitoring system.",
                IsRequired: true,
                Questions:
                [
                    "What areas are being monitored and what technology is used?",
                    "What is the geographic extent and hours of operation?",
                    "What types of personal data are captured (images, location, behavior)?",
                ]),
            new DPIASection(
                "Necessity and Proportionality",
                "Assess whether the monitoring is necessary and proportionate to its purpose.",
                IsRequired: true,
                Questions:
                [
                    "What is the specific purpose of the monitoring?",
                    "Are there less intrusive alternatives that achieve the same purpose?",
                    "Is the monitoring scope proportionate to the identified need?",
                ]),
            new DPIASection(
                "Individual Impact",
                "Evaluate the impact of monitoring on individuals' rights and expectations.",
                IsRequired: true,
                Questions:
                [
                    "How many individuals are likely affected by the monitoring?",
                    "What is the reasonable expectation of privacy in the monitored area?",
                    "How are individuals informed about the monitoring?",
                ]),
            new DPIASection(
                "Safeguards",
                "Document the safeguards and retention policies in place.",
                IsRequired: true,
                Questions:
                [
                    "What is the data retention period and deletion procedure?",
                    "Who has access to monitoring data and under what conditions?",
                    "What oversight mechanisms prevent misuse of monitoring data?",
                ]),
        ],
        RiskCategories =
        [
            "Surveillance",
            "Privacy Intrusion",
            "Public Space Monitoring",
            "Data Retention",
        ],
        SuggestedMitigations =
        [
            "Install clear and visible signage informing individuals of monitoring.",
            "Apply strict data minimization and retention limits.",
            "Restrict access to monitoring data to authorized personnel only.",
            "Implement automated deletion of monitoring data after the retention period.",
            "Conduct regular proportionality reviews of monitoring scope.",
        ],
    };

    private static DPIATemplate CreateAiMlTemplate() => new()
    {
        Name = "AI/ML Processing Assessment",
        Description = "Template for assessing AI and machine learning processing operations, including model training, inference, and automated analysis of personal data.",
        ProcessingType = "ai-ml",
        Sections =
        [
            new DPIASection(
                "Model Description",
                "Describe the AI/ML model, its purpose, and the processing it performs.",
                IsRequired: true,
                Questions:
                [
                    "What type of AI/ML model is used and what is its purpose?",
                    "What personal data is used for training and inference?",
                    "Does the model produce outputs that affect individuals?",
                ]),
            new DPIASection(
                "Training Data",
                "Document the training data sources, quality, and representativeness.",
                IsRequired: true,
                Questions:
                [
                    "What data sources are used for model training?",
                    "How is training data quality and representativeness ensured?",
                    "Is the training data regularly updated and reviewed?",
                ]),
            new DPIASection(
                "Fairness and Bias",
                "Assess the model for potential bias and discriminatory outcomes.",
                IsRequired: true,
                Questions:
                [
                    "What bias testing has been performed on the model?",
                    "How are protected characteristics handled in the model?",
                    "What processes exist for detecting and remediating bias?",
                ]),
            new DPIASection(
                "Explainability",
                "Evaluate the transparency and explainability of AI/ML decisions.",
                IsRequired: true,
                Questions:
                [
                    "Can the model's decisions be explained to affected individuals?",
                    "What level of interpretability does the model provide?",
                    "How is meaningful information about the logic provided (Art. 13-14)?",
                ]),
        ],
        RiskCategories =
        [
            "AI/ML Processing",
            "Bias and Fairness",
            "Transparency",
            "Automated Decisions",
        ],
        SuggestedMitigations =
        [
            "Implement comprehensive bias testing across protected characteristics.",
            "Provide meaningful explainability for AI/ML model decisions.",
            "Establish human review processes for high-impact automated decisions.",
            "Conduct regular model retraining and performance audits.",
            "Document model architecture and decision logic for transparency.",
        ],
    };

    private static DPIATemplate CreateBiometricTemplate() => new()
    {
        Name = "Biometric Data Processing Assessment",
        Description = "Template for assessing processing of biometric data for identification purposes, including facial recognition, fingerprint scanning, and voice recognition.",
        ProcessingType = "biometric",
        Sections =
        [
            new DPIASection(
                "Biometric Data Types",
                "Identify the types of biometric data collected and their characteristics.",
                IsRequired: true,
                Questions:
                [
                    "What types of biometric data are collected (facial, fingerprint, iris, voice)?",
                    "Is biometric data used for identification or verification purposes?",
                    "What is the accuracy rate and false acceptance/rejection rate?",
                ]),
            new DPIASection(
                "Collection Methods",
                "Document how biometric data is collected and under what conditions.",
                IsRequired: true,
                Questions:
                [
                    "How is biometric data collected (active enrollment vs. passive capture)?",
                    "What legal basis applies (explicit consent or other Art. 9(2) exemption)?",
                    "Are individuals informed at the point of collection?",
                ]),
            new DPIASection(
                "Storage and Security",
                "Assess the security measures for biometric data storage and processing.",
                IsRequired: true,
                Questions:
                [
                    "Are biometric templates stored instead of raw biometric data?",
                    "What encryption protects biometric data at rest and in transit?",
                    "Is biometric data stored on-device or in a centralized database?",
                ]),
            new DPIASection(
                "Purpose Limitation",
                "Evaluate adherence to purpose limitation for biometric processing.",
                IsRequired: true,
                Questions:
                [
                    "Is biometric data used solely for the stated purpose?",
                    "What controls prevent secondary use of biometric data?",
                    "What is the deletion procedure when the purpose is fulfilled?",
                ]),
        ],
        RiskCategories =
        [
            "Biometric Data",
            "Unique Identification",
            "Physical Security",
            "Consent Management",
        ],
        SuggestedMitigations =
        [
            "Store biometric templates rather than raw biometric data.",
            "Apply strong encryption for biometric data at rest and in transit.",
            "Implement liveness detection to prevent spoofing attacks.",
            "Obtain and record explicit consent for biometric processing.",
            "Establish clear deletion procedures for biometric data.",
        ],
    };

    private static DPIATemplate CreateHealthDataTemplate() => new()
    {
        Name = "Health Data Processing Assessment",
        Description = "Template for assessing processing of health and medical data, including electronic health records, patient monitoring, and health research.",
        ProcessingType = "health-data",
        Sections =
        [
            new DPIASection(
                "Health Data Categories",
                "Classify the health data being processed and its sensitivity level.",
                IsRequired: true,
                Questions:
                [
                    "What categories of health data are processed (diagnoses, treatments, genetic)?",
                    "Does processing include mental health or substance abuse data?",
                    "What is the volume of health records involved?",
                ]),
            new DPIASection(
                "Legal Basis and Ethics",
                "Document the legal and ethical basis for health data processing.",
                IsRequired: true,
                Questions:
                [
                    "What Article 9(2) exemption applies to this health data processing?",
                    "Has an ethics review or institutional review board approved the processing?",
                    "Are there sector-specific regulations that apply (e.g., national health law)?",
                ]),
            new DPIASection(
                "Access Controls",
                "Assess the access control framework protecting health data.",
                IsRequired: true,
                Questions:
                [
                    "Who has access to health data and under what role-based permissions?",
                    "How are access events logged and audited?",
                    "What is the process for granting and revoking access?",
                ]),
            new DPIASection(
                "Data Sharing",
                "Evaluate how health data is shared and with whom.",
                IsRequired: true,
                Questions:
                [
                    "Is health data shared with third parties (insurers, researchers, regulators)?",
                    "What data sharing agreements are in place?",
                    "Is data anonymized or pseudonymized before sharing?",
                ]),
        ],
        RiskCategories =
        [
            "Health Data",
            "Patient Confidentiality",
            "Medical Records",
            "Data Sharing",
        ],
        SuggestedMitigations =
        [
            "Apply end-to-end encryption for health data storage and transmission.",
            "Implement role-based access controls with mandatory audit logging.",
            "Use pseudonymization for health data used in research or analytics.",
            "Obtain explicit patient consent with clear information about data use.",
            "Establish data sharing agreements with all recipients of health data.",
        ],
    };

    private static DPIATemplate CreateGeneralTemplate() => new()
    {
        Name = "General DPIA Template",
        Description = "General-purpose template suitable for any processing operation requiring a Data Protection Impact Assessment. Used as a fallback when no specific template matches the processing type.",
        ProcessingType = "general",
        Sections =
        [
            new DPIASection(
                "Processing Description",
                "Provide a systematic description of the envisaged processing operations and purposes per Article 35(7)(a).",
                IsRequired: true,
                Questions:
                [
                    "What personal data is collected, stored, and processed?",
                    "What are the purposes of the processing?",
                    "Who are the data subjects and how many are affected?",
                    "What is the data flow from collection to deletion?",
                ]),
            new DPIASection(
                "Necessity and Proportionality",
                "Assess the necessity and proportionality of the processing in relation to the purposes per Article 35(7)(b).",
                IsRequired: true,
                Questions:
                [
                    "Is the processing necessary for the stated purpose?",
                    "Are there less invasive alternatives that achieve the same purpose?",
                    "How is data minimization applied?",
                    "What is the legal basis for processing (Art. 6)?",
                ]),
            new DPIASection(
                "Risk Assessment",
                "Assess the risks to the rights and freedoms of data subjects per Article 35(7)(c).",
                IsRequired: true,
                Questions:
                [
                    "What risks does the processing pose to data subjects?",
                    "What is the likelihood and severity of each identified risk?",
                    "Are there particular categories of data subjects at higher risk?",
                ]),
            new DPIASection(
                "Mitigation Measures",
                "Describe the measures envisaged to address the risks per Article 35(7)(d).",
                IsRequired: true,
                Questions:
                [
                    "What technical measures are in place (encryption, pseudonymization)?",
                    "What organizational measures are in place (policies, training)?",
                    "How will compliance be demonstrated and monitored?",
                ]),
        ],
        RiskCategories =
        [
            "Data Protection",
            "Privacy",
            "Security",
            "Individual Rights",
        ],
        SuggestedMitigations =
        [
            "Implement appropriate technical and organizational security measures.",
            "Apply data minimization and purpose limitation principles.",
            "Ensure transparency through clear privacy notices.",
            "Establish procedures for data subject rights requests.",
            "Conduct regular compliance reviews and staff training.",
        ],
    };
}
