# ABAC Tutorials — 8 Real-World Scenarios

Hands-on tutorials demonstrating Encina ABAC in production-like scenarios.
Each tutorial includes the full policy definition, attribute provider, request class, and service registration.

> **Prerequisites**: Familiarity with Encina messaging (ICommand/IQuery) and dependency injection.

---

## Tutorial 1: Department-Based Document Access

**Scenario**: Finance department users can read financial reports; HR users can read employee records. All other access is denied.

### Policy Definition

```csharp
using Encina.Security.ABAC;
using Encina.Security.ABAC.Builders;

public static class DepartmentAccessPolicies
{
    public static PolicySet Build() =>
        new PolicySetBuilder("department-document-access")
            .WithDescription("Controls document access based on user department")
            .WithAlgorithm(CombiningAlgorithmId.FirstApplicable)
            .AddPolicy("finance-reports-policy", policy => policy
                .WithDescription("Finance department can read financial reports")
                .WithTarget(t => t
                    .AnyOf(any => any
                        .AllOf(all => all
                            .MatchAttribute(
                                AttributeCategory.Resource,
                                "documentType",
                                ConditionOperator.Equals,
                                "FinancialReport"))))
                .AddRule("allow-finance-read", Effect.Permit, rule => rule
                    .WithDescription("Permit when subject is in Finance department")
                    .WithTarget(t => t
                        .AnyOf(any => any
                            .AllOf(all => all
                                .MatchAttribute(
                                    AttributeCategory.Subject,
                                    "department",
                                    ConditionOperator.Equals,
                                    "Finance"))))
                    .WithCondition(ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(
                            AttributeCategory.Action, "name", XACMLDataTypes.String),
                        ConditionBuilder.StringValue("read")))))
            .AddPolicy("hr-records-policy", policy => policy
                .WithDescription("HR department can read employee records")
                .WithTarget(t => t
                    .AnyOf(any => any
                        .AllOf(all => all
                            .MatchAttribute(
                                AttributeCategory.Resource,
                                "documentType",
                                ConditionOperator.Equals,
                                "EmployeeRecord"))))
                .AddRule("allow-hr-read", Effect.Permit, rule => rule
                    .WithTarget(t => t
                        .AnyOf(any => any
                            .AllOf(all => all
                                .MatchAttribute(
                                    AttributeCategory.Subject,
                                    "department",
                                    ConditionOperator.Equals,
                                    "HR"))))
                    .WithCondition(ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(
                            AttributeCategory.Action, "name", XACMLDataTypes.String),
                        ConditionBuilder.StringValue("read")))))
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class DepartmentAttributeProvider(IUserRepository userRepository) : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return new Dictionary<string, object>
        {
            ["department"] = user.Department,
            ["role"] = user.Role
        };
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
    {
        var attrs = resource switch
        {
            GetFinancialReportQuery => new Dictionary<string, object>
            {
                ["documentType"] = "FinancialReport"
            },
            GetEmployeeRecordQuery => new Dictionary<string, object>
            {
                ["documentType"] = "EmployeeRecord"
            },
            _ => new Dictionary<string, object>()
        };
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(attrs);
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>());
}
```

### Request Classes

```csharp
[RequirePolicy("department-document-access")]
public sealed record GetFinancialReportQuery(Guid ReportId) : IQuery<ReportDto>;

[RequirePolicy("department-document-access")]
public sealed record GetEmployeeRecordQuery(Guid EmployeeId) : IQuery<EmployeeRecordDto>;
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, DepartmentAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.SeedPolicySets.Add(DepartmentAccessPolicies.Build());
});
```

### Expected Behavior

| User Department | Request | Result |
|-----------------|---------|--------|
| Finance | `GetFinancialReportQuery` | **Permit** |
| Finance | `GetEmployeeRecordQuery` | **Deny** (NotApplicable, default deny) |
| HR | `GetEmployeeRecordQuery` | **Permit** |
| HR | `GetFinancialReportQuery` | **Deny** |
| Engineering | Either query | **Deny** |

---

## Tutorial 2: Amount-Based Approval Workflow

**Scenario**: Purchase approvals follow a tiered hierarchy. Any manager can approve under $10K. Senior managers handle $10K-$50K. Only directors approve above $50K.

### Policy Definition

```csharp
public static class ApprovalWorkflowPolicies
{
    public static PolicySet Build() =>
        new PolicySetBuilder("purchase-approval")
            .WithDescription("Tiered purchase approval based on amount and role")
            .WithAlgorithm(CombiningAlgorithmId.FirstApplicable)
            .AddPolicy("low-tier-approval", policy => policy
                .WithDescription("Under $10K: any manager")
                .AddRule("allow-manager-low", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Or(
                            ConditionBuilder.Equal(
                                ConditionBuilder.Attribute(
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String),
                                ConditionBuilder.StringValue("Manager")),
                            ConditionBuilder.Equal(
                                ConditionBuilder.Attribute(
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String),
                                ConditionBuilder.StringValue("SeniorManager")),
                            ConditionBuilder.Equal(
                                ConditionBuilder.Attribute(
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String),
                                ConditionBuilder.StringValue("Director"))),
                        ConditionBuilder.LessThan(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
                            ConditionBuilder.IntValue(10_000))))))
            .AddPolicy("mid-tier-approval", policy => policy
                .WithDescription("$10K-$50K: senior managers and directors")
                .AddRule("allow-senior-mid", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Or(
                            ConditionBuilder.Equal(
                                ConditionBuilder.Attribute(
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String),
                                ConditionBuilder.StringValue("SeniorManager")),
                            ConditionBuilder.Equal(
                                ConditionBuilder.Attribute(
                                    AttributeCategory.Subject, "role", XACMLDataTypes.String),
                                ConditionBuilder.StringValue("Director"))),
                        ConditionBuilder.GreaterThanOrEqual(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
                            ConditionBuilder.IntValue(10_000)),
                        ConditionBuilder.LessThanOrEqual(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
                            ConditionBuilder.IntValue(50_000))))))
            .AddPolicy("high-tier-approval", policy => policy
                .WithDescription("Over $50K: directors only")
                .AddRule("allow-director-high", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Subject, "role", XACMLDataTypes.String),
                            ConditionBuilder.StringValue("Director")),
                        ConditionBuilder.GreaterThan(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Resource, "amount", XACMLDataTypes.Integer),
                            ConditionBuilder.IntValue(50_000))))))
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class ApprovalAttributeProvider(IUserRepository userRepository) : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return new Dictionary<string, object>
        {
            ["role"] = user.Role // "Manager", "SeniorManager", "Director"
        };
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
    {
        var attrs = resource switch
        {
            ApprovePurchaseCommand cmd => new Dictionary<string, object>
            {
                ["amount"] = cmd.Amount
            },
            _ => new Dictionary<string, object>()
        };
        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(attrs);
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>());
}
```

### Request Class

```csharp
[RequirePolicy("purchase-approval")]
public sealed record ApprovePurchaseCommand(Guid PurchaseId, int Amount) : ICommand;
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, ApprovalAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.SeedPolicySets.Add(ApprovalWorkflowPolicies.Build());
});
```

### Expected Behavior

| Role | Amount | Result |
|------|--------|--------|
| Manager | $5,000 | **Permit** |
| Manager | $15,000 | **Deny** |
| SeniorManager | $25,000 | **Permit** |
| SeniorManager | $60,000 | **Deny** |
| Director | $100,000 | **Permit** |
| Employee | Any | **Deny** |

---

## Tutorial 3: Time-Based Access Control

**Scenario**: The payments API is only accessible during business hours (Monday-Friday, 9:00-17:00 UTC). Requests outside this window are denied.

### Policy Definition

```csharp
public static class BusinessHoursPolicies
{
    public static Policy Build() =>
        new PolicyBuilder("business-hours-access")
            .WithDescription("Restrict access to business hours only")
            .WithAlgorithm(CombiningAlgorithmId.DenyUnlessPermit)
            .AddRule("allow-business-hours", Effect.Permit, rule => rule
                .WithDescription("Permit access during weekday business hours")
                .WithCondition(ConditionBuilder.And(
                    ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(
                            AttributeCategory.Environment, "isBusinessHours",
                            XACMLDataTypes.Boolean),
                        ConditionBuilder.BoolValue(true)),
                    ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(
                            AttributeCategory.Environment, "isWeekday",
                            XACMLDataTypes.Boolean),
                        ConditionBuilder.BoolValue(true)))))
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class BusinessHoursAttributeProvider(TimeProvider timeProvider) : IAttributeProvider
{
    public ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object> { ["userId"] = userId });

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>());

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var hour = now.Hour;
        var dayOfWeek = now.DayOfWeek;

        return ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>
            {
                ["currentTime"] = now.DateTime,
                ["isBusinessHours"] = hour >= 9 && hour < 17,
                ["isWeekday"] = dayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
                ["dayOfWeek"] = dayOfWeek.ToString()
            });
    }
}
```

### Request Class

```csharp
[RequirePolicy("business-hours-access")]
public sealed record ProcessPaymentCommand(Guid PaymentId, decimal Amount) : ICommand;
```

### Service Registration

```csharp
services.AddSingleton(TimeProvider.System);
services.AddScoped<IAttributeProvider, BusinessHoursAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.SeedPolicies.Add(BusinessHoursPolicies.Build());
});
```

### Expected Behavior

| Time | Day | Result |
|------|-----|--------|
| 10:00 UTC | Monday | **Permit** |
| 16:59 UTC | Friday | **Permit** |
| 17:00 UTC | Wednesday | **Deny** |
| 08:59 UTC | Tuesday | **Deny** |
| 12:00 UTC | Saturday | **Deny** |

---

## Tutorial 4: Data Classification and Clearance Levels

**Scenario**: Documents carry a classification level (Public=1, Internal=2, Confidential=3, Secret=4). Users have a numeric clearance level (1-5). Access requires the user clearance to be greater than or equal to the document classification.

### Policy Definition

```csharp
public static class DataClassificationPolicies
{
    public static Policy Build() =>
        new PolicyBuilder("data-classification")
            .WithDescription("Enforce clearance-based access to classified documents")
            .WithAlgorithm(CombiningAlgorithmId.DenyUnlessPermit)
            .AddRule("allow-sufficient-clearance", Effect.Permit, rule => rule
                .WithDescription("Permit when user clearance >= document classification")
                .WithCondition(ConditionBuilder.GreaterThanOrEqual(
                    ConditionBuilder.Attribute(
                        AttributeCategory.Subject, "clearanceLevel",
                        XACMLDataTypes.Integer, mustBePresent: true),
                    ConditionBuilder.Attribute(
                        AttributeCategory.Resource, "classificationLevel",
                        XACMLDataTypes.Integer, mustBePresent: true))))
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class ClassificationAttributeProvider(
    IUserRepository userRepository,
    IDocumentRepository documentRepository) : IAttributeProvider
{
    private static readonly Dictionary<string, int> ClassificationMap = new()
    {
        ["Public"] = 1,
        ["Internal"] = 2,
        ["Confidential"] = 3,
        ["Secret"] = 4
    };

    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return new Dictionary<string, object>
        {
            ["clearanceLevel"] = user.ClearanceLevel // 1-5
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is GetClassifiedDocumentQuery query)
        {
            var doc = await documentRepository.GetByIdAsync(query.DocumentId, cancellationToken);
            return new Dictionary<string, object>
            {
                ["classificationLevel"] = ClassificationMap.GetValueOrDefault(
                    doc.Classification, 1),
                ["classification"] = doc.Classification
            };
        }

        return new Dictionary<string, object>();
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>());
}
```

### Request Class

```csharp
[RequirePolicy("data-classification")]
public sealed record GetClassifiedDocumentQuery(Guid DocumentId) : IQuery<ClassifiedDocumentDto>;
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, ClassificationAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.SeedPolicies.Add(DataClassificationPolicies.Build());
});
```

### Expected Behavior

| User Clearance | Document Classification | Result |
|----------------|------------------------|--------|
| 5 | Secret (4) | **Permit** |
| 3 | Confidential (3) | **Permit** |
| 2 | Confidential (3) | **Deny** |
| 1 | Internal (2) | **Deny** |
| 1 | Public (1) | **Permit** |

---

## Tutorial 5: Multi-Tenant Resource Isolation

**Scenario**: Users must only access resources belonging to their own tenant. A strict string-equal comparison on `tenantId` ensures complete isolation.

### Policy Definition

```csharp
public static class TenantIsolationPolicies
{
    public static Policy Build() =>
        new PolicyBuilder("tenant-isolation")
            .WithDescription("Enforce strict tenant isolation on all resource access")
            .WithAlgorithm(CombiningAlgorithmId.DenyUnlessPermit)
            .AddRule("allow-same-tenant", Effect.Permit, rule => rule
                .WithDescription("Permit only when subject and resource share the same tenant")
                .WithCondition(ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(
                        AttributeCategory.Subject, "tenantId",
                        XACMLDataTypes.String, mustBePresent: true),
                    ConditionBuilder.Attribute(
                        AttributeCategory.Resource, "tenantId",
                        XACMLDataTypes.String, mustBePresent: true))))
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class TenantAttributeProvider(
    ITenantContext tenantContext,
    IResourceRepository resourceRepository) : IAttributeProvider
{
    public ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>
            {
                ["tenantId"] = tenantContext.CurrentTenantId,
                ["userId"] = userId
            });

    public async ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is IHasTenantResource tenantResource)
        {
            var entity = await resourceRepository.GetByIdAsync(
                tenantResource.ResourceId, cancellationToken);
            return new Dictionary<string, object>
            {
                ["tenantId"] = entity.TenantId,
                ["resourceId"] = entity.Id.ToString()
            };
        }

        return new Dictionary<string, object>();
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>());
}

// Helper interface for commands/queries that target a tenant resource
public interface IHasTenantResource
{
    Guid ResourceId { get; }
}
```

### Request Class

```csharp
[RequirePolicy("tenant-isolation")]
public sealed record GetTenantDocumentQuery(Guid DocumentId)
    : IQuery<DocumentDto>, IHasTenantResource
{
    public Guid ResourceId => DocumentId;
}

[RequirePolicy("tenant-isolation")]
public sealed record UpdateTenantSettingsCommand(Guid SettingsId, string Value)
    : ICommand, IHasTenantResource
{
    public Guid ResourceId => SettingsId;
}
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, TenantAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.SeedPolicies.Add(TenantIsolationPolicies.Build());
});
```

### Expected Behavior

| User Tenant | Resource Tenant | Result |
|-------------|-----------------|--------|
| `tenant-a` | `tenant-a` | **Permit** |
| `tenant-a` | `tenant-b` | **Deny** |
| `tenant-b` | `tenant-a` | **Deny** |
| `tenant-b` | `tenant-b` | **Permit** |

---

## Tutorial 6: Obligations -- Audit Logging

**Scenario**: Every permitted access to financial data must generate an audit log entry. The obligation is mandatory: if audit logging fails, access is denied per XACML 3.0 section 7.18.

### Policy Definition

```csharp
public static class AuditedFinancePolicies
{
    public static Policy Build() =>
        new PolicyBuilder("audited-finance-access")
            .WithDescription("Finance access with mandatory audit logging")
            .WithAlgorithm(CombiningAlgorithmId.DenyUnlessPermit)
            .AddRule("allow-finance-with-audit", Effect.Permit, rule => rule
                .WithDescription("Permit Finance department, trigger audit obligation")
                .WithCondition(ConditionBuilder.Equal(
                    ConditionBuilder.Attribute(
                        AttributeCategory.Subject, "department", XACMLDataTypes.String),
                    ConditionBuilder.StringValue("Finance")))
                .AddObligation("audit-log", ob => ob
                    .OnPermit()
                    .WithAttribute("action", "Financial data accessed")
                    .WithAttribute("severity", "High")
                    .WithAttribute("timestamp",
                        ConditionBuilder.Attribute(
                            AttributeCategory.Environment, "currentTime",
                            XACMLDataTypes.DateTime))))
            .Build();
}
```

### Obligation Handler

```csharp
public sealed class AuditLogObligationHandler(
    IAuditService auditService,
    ILogger<AuditLogObligationHandler> logger) : IObligationHandler
{
    public bool CanHandle(string obligationId)
        => obligationId == "audit-log";

    public async ValueTask<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var action = obligation.AttributeAssignments
                .FirstOrDefault(a => a.AttributeId == "action")?.Value?.ToString();
            var severity = obligation.AttributeAssignments
                .FirstOrDefault(a => a.AttributeId == "severity")?.Value?.ToString();

            await auditService.LogAsync(new AuditEntry
            {
                UserId = context.SubjectAttributes.GetValueOrDefault("userId")?.ToString()
                    ?? "unknown",
                Action = action ?? "unknown",
                Severity = severity ?? "Normal",
                TimestampUtc = DateTime.UtcNow,
                ResourceType = context.ResourceAttributes.GetValueOrDefault("resourceType")
                    ?.ToString()
            }, cancellationToken);

            logger.LogInformation(
                "Audit obligation fulfilled for user {UserId}",
                context.SubjectAttributes.GetValueOrDefault("userId"));

            return Unit.Default;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fulfill audit obligation");
            return EncinaError.Create("AUDIT_FAILURE", "Audit logging failed — access denied");
        }
    }
}
```

### Attribute Provider

```csharp
public sealed class AuditedFinanceAttributeProvider(
    IUserRepository userRepository,
    TimeProvider timeProvider) : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["department"] = user.Department
        };
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object> { ["resourceType"] = typeof(TResource).Name });

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>
            {
                ["currentTime"] = timeProvider.GetUtcNow().DateTime
            });
}
```

### Request Class

```csharp
[RequirePolicy("audited-finance-access")]
public sealed record GetFinancialSummaryQuery(Guid AccountId) : IQuery<FinancialSummaryDto>;
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, AuditedFinanceAttributeProvider>();
services.AddScoped<IObligationHandler, AuditLogObligationHandler>();
services.AddEncinaABAC(options =>
{
    options.FailOnMissingObligationHandler = true; // XACML 3.0 compliant
    options.SeedPolicies.Add(AuditedFinancePolicies.Build());
});
```

### Expected Behavior

| User Dept | Audit Service | Result |
|-----------|--------------|--------|
| Finance | Healthy | **Permit** + audit log written |
| Finance | Down/failing | **Deny** (obligation failed) |
| Engineering | N/A | **Deny** (rule does not match) |

> **Key point**: If `FailOnMissingObligationHandler = true` and no handler is registered for `"audit-log"`, every Permit decision converts to Deny. This ensures audit compliance.

---

## Tutorial 7: Healthcare HIPAA Compliance

**Scenario**: Doctors access patient records only within their assigned department. An emergency override allows cross-department access but requires elevated logging. This tutorial uses the EEL `[RequireCondition]` attribute for inline expressions.

### Policy Definition (for the emergency override)

```csharp
public static class HealthcarePolicies
{
    public static PolicySet Build() =>
        new PolicySetBuilder("hipaa-patient-access")
            .WithDescription("HIPAA-compliant patient record access control")
            .WithAlgorithm(CombiningAlgorithmId.PermitOverrides)
            .AddPolicy("emergency-override", policy => policy
                .WithDescription("Emergency override: any doctor, any department, with logging")
                .WithPriority(0) // Highest priority
                .AddRule("allow-emergency", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Subject, "role", XACMLDataTypes.String),
                            ConditionBuilder.StringValue("Doctor")),
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Environment, "isEmergency",
                                XACMLDataTypes.Boolean),
                            ConditionBuilder.BoolValue(true))))
                    .AddObligation("emergency-audit", ob => ob
                        .OnPermit()
                        .WithAttribute("action", "EMERGENCY ACCESS — HIPAA audit required")
                        .WithAttribute("severity", "Critical"))))
            .AddPolicy("department-access", policy => policy
                .WithDescription("Standard access: doctors in matching department only")
                .AddRule("allow-same-department", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Subject, "role", XACMLDataTypes.String),
                            ConditionBuilder.StringValue("Doctor")),
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Subject, "department",
                                XACMLDataTypes.String),
                            ConditionBuilder.Attribute(
                                AttributeCategory.Resource, "department",
                                XACMLDataTypes.String))))))
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class HealthcareAttributeProvider(
    IUserRepository userRepository,
    IPatientRepository patientRepository,
    IEmergencyContext emergencyContext) : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return new Dictionary<string, object>
        {
            ["role"] = user.Role,
            ["department"] = user.Department,
            ["licenseNumber"] = user.LicenseNumber ?? ""
        };
    }

    public async ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
    {
        if (resource is GetPatientRecordQuery query)
        {
            var patient = await patientRepository.GetByIdAsync(
                query.PatientId, cancellationToken);
            return new Dictionary<string, object>
            {
                ["department"] = patient.AssignedDepartment,
                ["patientId"] = patient.Id.ToString()
            };
        }

        return new Dictionary<string, object>();
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>
            {
                ["isEmergency"] = emergencyContext.IsEmergencyActive,
                ["currentTime"] = DateTime.UtcNow
            });
}
```

### Request Classes (using EEL)

```csharp
// Standard access — EEL inline condition checks role + department match
[RequireCondition("user.role == \"Doctor\" && user.department == resource.department")]
public sealed record GetPatientRecordQuery(Guid PatientId) : IQuery<PatientRecordDto>;

// For emergency scenarios, use the full policy that includes the override
[RequirePolicy("hipaa-patient-access")]
public sealed record GetPatientRecordEmergencyQuery(Guid PatientId)
    : IQuery<PatientRecordDto>;
```

### Emergency Audit Obligation Handler

```csharp
public sealed class EmergencyAuditHandler(
    IAuditService auditService,
    INotificationService notifications) : IObligationHandler
{
    public bool CanHandle(string obligationId)
        => obligationId == "emergency-audit";

    public async ValueTask<Either<EncinaError, Unit>> HandleAsync(
        Obligation obligation,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        var userId = context.SubjectAttributes.GetValueOrDefault("userId")?.ToString();
        var patientId = context.ResourceAttributes.GetValueOrDefault("patientId")?.ToString();

        await auditService.LogAsync(new AuditEntry
        {
            UserId = userId ?? "unknown",
            Action = "EMERGENCY_PATIENT_ACCESS",
            Severity = "Critical",
            TimestampUtc = DateTime.UtcNow,
            Details = $"Emergency access to patient {patientId}"
        }, cancellationToken);

        // Notify compliance team
        await notifications.SendAsync(
            "compliance@hospital.org",
            $"HIPAA Alert: Emergency access by {userId} to patient {patientId}",
            cancellationToken);

        return Unit.Default;
    }
}
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, HealthcareAttributeProvider>();
services.AddScoped<IObligationHandler, EmergencyAuditHandler>();
services.AddEncinaABAC(options =>
{
    options.FailOnMissingObligationHandler = true;
    options.SeedPolicySets.Add(HealthcarePolicies.Build());
    options.ValidateExpressionsAtStartup = true;
    options.ExpressionScanAssemblies.Add(typeof(GetPatientRecordQuery).Assembly);
});
```

### Expected Behavior

| Doctor Dept | Patient Dept | Emergency | Query Type | Result |
|-------------|-------------|-----------|------------|--------|
| Cardiology | Cardiology | No | Standard | **Permit** |
| Cardiology | Neurology | No | Standard | **Deny** |
| Cardiology | Neurology | Yes | Emergency | **Permit** + critical audit |
| Nurse | Cardiology | Yes | Emergency | **Deny** (role is not Doctor) |

---

## Tutorial 8: Combining Multiple Policy Sets

**Scenario**: An organization applies security at two levels: (1) an organization-wide security policy set that enforces global rules (active accounts, non-suspended users), and (2) department-specific policy sets with their own rules. The `DenyOverrides` algorithm ensures that any deny from either level blocks access.

### Policy Definitions

```csharp
public static class OrganizationPolicies
{
    /// <summary>
    /// Organization-wide security: every request must pass these checks.
    /// </summary>
    public static PolicySet BuildGlobalPolicies() =>
        new PolicySetBuilder("org-global-security")
            .WithDescription("Organization-wide mandatory security checks")
            .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
            .AddPolicy("active-account-check", policy => policy
                .WithDescription("Deny access for inactive or suspended accounts")
                .AddRule("deny-inactive", Effect.Deny, rule => rule
                    .WithDescription("Block access if account is not active")
                    .WithCondition(ConditionBuilder.Not(
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Subject, "accountStatus",
                                XACMLDataTypes.String),
                            ConditionBuilder.StringValue("Active")))))
                .AddRule("allow-active", Effect.Permit, rule => rule
                    .WithDescription("Default permit for active accounts")
                    .WithCondition(ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(
                            AttributeCategory.Subject, "accountStatus",
                            XACMLDataTypes.String),
                        ConditionBuilder.StringValue("Active")))))
            .AddPolicy("ip-restriction", policy => policy
                .WithDescription("Deny access from blocked IP ranges")
                .AddRule("deny-blocked-ip", Effect.Deny, rule => rule
                    .WithCondition(ConditionBuilder.Equal(
                        ConditionBuilder.Attribute(
                            AttributeCategory.Environment, "isBlockedIp",
                            XACMLDataTypes.Boolean),
                        ConditionBuilder.BoolValue(true))))
                .AddRule("allow-unblocked-ip", Effect.Permit, _ => { }))
            .Build();

    /// <summary>
    /// Department-level policies for Engineering team.
    /// </summary>
    public static PolicySet BuildEngineeringPolicies() =>
        new PolicySetBuilder("dept-engineering")
            .WithDescription("Engineering department access controls")
            .WithAlgorithm(CombiningAlgorithmId.FirstApplicable)
            .WithTarget(t => t
                .AnyOf(any => any
                    .AllOf(all => all
                        .MatchAttribute(
                            AttributeCategory.Subject,
                            "department",
                            ConditionOperator.Equals,
                            "Engineering"))))
            .AddPolicy("code-repo-access", policy => policy
                .WithDescription("Engineers can read/write code repositories")
                .AddRule("allow-repo-access", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.Or(
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Action, "name", XACMLDataTypes.String),
                            ConditionBuilder.StringValue("read")),
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Action, "name", XACMLDataTypes.String),
                            ConditionBuilder.StringValue("write"))))))
            .AddPolicy("deploy-access", policy => policy
                .WithDescription("Only senior engineers can deploy")
                .AddRule("allow-senior-deploy", Effect.Permit, rule => rule
                    .WithCondition(ConditionBuilder.And(
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Action, "name", XACMLDataTypes.String),
                            ConditionBuilder.StringValue("deploy")),
                        ConditionBuilder.Equal(
                            ConditionBuilder.Attribute(
                                AttributeCategory.Subject, "seniority",
                                XACMLDataTypes.String),
                            ConditionBuilder.StringValue("Senior"))))))
            .Build();

    /// <summary>
    /// Top-level policy set combining global + department policies.
    /// DenyOverrides ensures global denials always take precedence.
    /// </summary>
    public static PolicySet BuildCombinedPolicies() =>
        new PolicySetBuilder("organization-root")
            .WithDescription("Root policy set: global security + department policies")
            .WithAlgorithm(CombiningAlgorithmId.DenyOverrides)
            .AddPolicySet(BuildGlobalPolicies())
            .AddPolicySet(BuildEngineeringPolicies())
            .Build();
}
```

### Attribute Provider

```csharp
public sealed class OrganizationAttributeProvider(
    IUserRepository userRepository,
    IIpReputationService ipService,
    IHttpContextAccessor httpContextAccessor) : IAttributeProvider
{
    public async ValueTask<IReadOnlyDictionary<string, object>> GetSubjectAttributesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["department"] = user.Department,
            ["accountStatus"] = user.AccountStatus, // "Active", "Suspended", "Inactive"
            ["seniority"] = user.Seniority // "Junior", "Mid", "Senior"
        };
    }

    public ValueTask<IReadOnlyDictionary<string, object>> GetResourceAttributesAsync<TResource>(
        TResource resource, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<IReadOnlyDictionary<string, object>>(
            new Dictionary<string, object>
            {
                ["resourceType"] = typeof(TResource).Name
            });

    public async ValueTask<IReadOnlyDictionary<string, object>> GetEnvironmentAttributesAsync(
        CancellationToken cancellationToken = default)
    {
        var ip = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        var isBlocked = await ipService.IsBlockedAsync(ip, cancellationToken);

        return new Dictionary<string, object>
        {
            ["clientIp"] = ip,
            ["isBlockedIp"] = isBlocked,
            ["currentTime"] = DateTime.UtcNow
        };
    }
}
```

### Request Classes

```csharp
[RequirePolicy("organization-root")]
public sealed record ReadRepositoryQuery(Guid RepoId) : IQuery<RepositoryDto>;

[RequirePolicy("organization-root")]
public sealed record DeployToProductionCommand(Guid ReleaseId) : ICommand;
```

### Service Registration

```csharp
services.AddScoped<IAttributeProvider, OrganizationAttributeProvider>();
services.AddEncinaABAC(options =>
{
    options.EnforcementMode = ABACEnforcementMode.Block;
    options.DefaultNotApplicableEffect = Effect.Deny;
    options.AddHealthCheck = true;
    options.SeedPolicySets.Add(OrganizationPolicies.BuildCombinedPolicies());
});
```

### Expected Behavior

The `DenyOverrides` algorithm on the root policy set means global denials always win:

| Account | Department | Seniority | IP | Action | Result |
|---------|-----------|-----------|-----|--------|--------|
| Active | Engineering | Senior | Clean | deploy | **Permit** |
| Active | Engineering | Junior | Clean | read | **Permit** |
| Active | Engineering | Junior | Clean | deploy | **Deny** (not senior) |
| Suspended | Engineering | Senior | Clean | deploy | **Deny** (global: inactive) |
| Active | Engineering | Senior | Blocked | deploy | **Deny** (global: blocked IP) |
| Active | Marketing | Senior | Clean | read | **Deny** (dept target miss) |

### How DenyOverrides Works Here

1. The root `organization-root` evaluates both child policy sets.
2. `org-global-security` runs first. If the account is suspended or the IP is blocked, it returns **Deny**.
3. `dept-engineering` runs next. Its target only matches Engineering users; others get NotApplicable.
4. `DenyOverrides` merges: if either returns Deny, the final result is **Deny**. Both must avoid Deny for a Permit to propagate.

This layered architecture cleanly separates organization-wide concerns from department-specific logic while guaranteeing that global security rules cannot be bypassed.
