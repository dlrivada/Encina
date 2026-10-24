using Encina.Security.Audit;
using Encina.Security.BreakTheGlass;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.BreakTheGlass;

/// <summary>Shared builders for the break-the-glass unit tests.</summary>
internal static class BreakTheGlassTestKit
{
    public const string Scope = "clinical-record";
    public const string Justification = "Patient arrived unconscious and the treating psychologist is away.";

    public static BreakTheGlassOptions Options(Action<BreakTheGlassOptions>? configure = null)
    {
        var options = new BreakTheGlassOptions();
        options.DeclareScope(Scope, scope => scope.OpenerRoles.Add("clinician"));
        options.ReviewerRoles.Add("reviewer");
        configure?.Invoke(options);
        return options;
    }

    public static IOptions<BreakTheGlassOptions> Wrap(BreakTheGlassOptions options) => Microsoft.Extensions.Options.Options.Create(options);

    public static IRequestContext Context(RequestIdentity identity, string? tenantId = "tenant-1", string correlationId = "corr-1")
    {
        var context = Substitute.For<IRequestContext>();
        context.Identity.Returns(identity);
        context.TenantId.Returns(tenantId);
        context.CorrelationId.Returns(correlationId);
        return context;
    }

    public static IRequestContextAccessor Accessor(IRequestContext? context)
    {
        var accessor = Substitute.For<IRequestContextAccessor>();
        accessor.RequestContext.Returns(context);
        return accessor;
    }

    public static RequestIdentity Clinician(string userId = "clin-1") => TestIdentity.User(userId, roles: ["clinician"]);

    public static RequestIdentity Reviewer(string userId = "rev-1") => TestIdentity.User(userId, roles: ["reviewer"]);

    public static BreakTheGlassElevation Elevation(
        DateTimeOffset now,
        string userId = "clin-1",
        string subjectId = "patient-1",
        string? tenantId = "tenant-1",
        TimeSpan? duration = null,
        Guid? id = null) => new()
        {
            ElevationId = id ?? Guid.CreateVersion7(now),
            TenantId = tenantId,
            UserId = userId,
            SubjectId = subjectId,
            Scope = Scope,
            Justification = Justification,
            OpenedAtUtc = now,
            ExpiresAtUtc = now + (duration ?? TimeSpan.FromHours(1))
        };
}
