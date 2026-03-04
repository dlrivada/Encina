namespace Encina.DomainModeling;

/// <summary>
/// Marker interface indicating that read access to this entity should be audited.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface on entities that contain sensitive data where read access
/// must be tracked for regulatory compliance or security purposes. When combined with
/// the repository pattern and <c>AddEncinaReadAuditing()</c>, all read operations
/// (GetById, Find, GetAll, GetPaged) are automatically recorded in the read audit store.
/// </para>
/// <para>
/// This interface supports compliance requirements across multiple regulations:
/// <list type="bullet">
/// <item><b>GDPR Art. 15</b> — Right of access: track who viewed personal data and why</item>
/// <item><b>HIPAA §164.312(b)</b> — Audit controls: record access to electronic protected health information (ePHI)</item>
/// <item><b>SOX §302/§404</b> — Internal controls: track access to financial records</item>
/// <item><b>PCI-DSS Req. 10.2</b> — Logging: monitor access to cardholder data environments</item>
/// </list>
/// </para>
/// <para>
/// This is a <b>marker interface</b> with no members. It serves as an opt-in signal for the
/// read auditing infrastructure. Entities not implementing this interface are never audited
/// for read operations, ensuring zero overhead for non-sensitive data.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Mark a sensitive entity for read auditing
/// public class Patient : Entity&lt;PatientId&gt;, IReadAuditable
/// {
///     public string Name { get; set; }
///     public string MedicalRecordNumber { get; set; }
/// }
///
/// // Configure read auditing in DI
/// services.AddEncinaReadAuditing(options =>
/// {
///     options.AuditReadsFor&lt;Patient&gt;();
/// });
/// </code>
/// </example>
/// <seealso cref="IEntity{TId}"/>
public interface IReadAuditable;
