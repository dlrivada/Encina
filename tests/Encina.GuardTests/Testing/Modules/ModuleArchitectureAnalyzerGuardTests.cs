using Encina.Testing.Modules;

namespace Encina.GuardTests.Testing.Modules;

public class ModuleArchitectureAnalyzerGuardTests
{
    public class ConstructorGuards
    {
        [Fact]
        public void NullAssemblies_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                new ModuleArchitectureAnalyzer((System.Reflection.Assembly[])null!));
        }

        [Fact]
        public void EmptyAssemblies_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                new ModuleArchitectureAnalyzer(Array.Empty<System.Reflection.Assembly>()));
        }
    }

    public class ConstructorWithLoggerGuards
    {
        [Fact]
        public void NullLogger_Throws()
        {
            Should.Throw<ArgumentNullException>(() =>
                new ModuleArchitectureAnalyzer(
                    (ILogger<ModuleArchitectureAnalyzer>)null!,
                    typeof(ModuleArchitectureAnalyzerGuardTests).Assembly));
        }

        [Fact]
        public void NullAssembliesWithLogger_Throws()
        {
            var logger = NSubstitute.Substitute.For<ILogger<ModuleArchitectureAnalyzer>>();

            Should.Throw<ArgumentException>(() =>
                new ModuleArchitectureAnalyzer(logger, (System.Reflection.Assembly[])null!));
        }

        [Fact]
        public void EmptyAssembliesWithLogger_Throws()
        {
            var logger = NSubstitute.Substitute.For<ILogger<ModuleArchitectureAnalyzer>>();

            Should.Throw<ArgumentException>(() =>
                new ModuleArchitectureAnalyzer(logger, Array.Empty<System.Reflection.Assembly>()));
        }
    }

    public class StaticAnalyzeGuards
    {
        [Fact]
        public void NullAssemblies_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                ModuleArchitectureAnalyzer.Analyze((System.Reflection.Assembly[])null!));
        }

        [Fact]
        public void EmptyAssemblies_Throws()
        {
            Should.Throw<ArgumentException>(() =>
                ModuleArchitectureAnalyzer.Analyze(Array.Empty<System.Reflection.Assembly>()));
        }
    }
}
