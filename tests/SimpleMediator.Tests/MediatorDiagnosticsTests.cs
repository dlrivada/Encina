using Shouldly;
using SimpleMediator;

namespace SimpleMediator.Tests;

public sealed class MediatorDiagnosticsTests
{
    [Fact]
    public void ActivitySource_ExposesStableMetadata()
    {
        MediatorDiagnostics.ActivitySource.Name.ShouldBe("SimpleMediator");
        MediatorDiagnostics.ActivitySource.Version.ShouldBe("1.0");
    }
}
