using Encina.Messaging.ContentRouter;
using Shouldly;

namespace Encina.Messaging.Tests.ContentRouter;

/// <summary>
/// Unit tests for <see cref="ContentRouterOptions"/>.
/// </summary>
public sealed class ContentRouterOptionsTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new ContentRouterOptions();

        // Assert
        options.EnableRouteCaching.ShouldBeTrue();
        options.MaxCacheSize.ShouldBe(1000);
        options.ThrowOnNoMatch.ShouldBeTrue();
        options.AllowMultipleMatches.ShouldBeFalse();
        options.EvaluateInParallel.ShouldBeFalse();
        options.MaxDegreeOfParallelism.ShouldBe(Environment.ProcessorCount);
    }

    [Fact]
    public void CanSetEnableRouteCaching()
    {
        // Arrange & Act
        var options = new ContentRouterOptions { EnableRouteCaching = false };

        // Assert
        options.EnableRouteCaching.ShouldBeFalse();
    }

    [Fact]
    public void CanSetMaxCacheSize()
    {
        // Arrange & Act
        var options = new ContentRouterOptions { MaxCacheSize = 5000 };

        // Assert
        options.MaxCacheSize.ShouldBe(5000);
    }

    [Fact]
    public void CanSetThrowOnNoMatch()
    {
        // Arrange & Act
        var options = new ContentRouterOptions { ThrowOnNoMatch = false };

        // Assert
        options.ThrowOnNoMatch.ShouldBeFalse();
    }

    [Fact]
    public void CanSetAllowMultipleMatches()
    {
        // Arrange & Act
        var options = new ContentRouterOptions { AllowMultipleMatches = true };

        // Assert
        options.AllowMultipleMatches.ShouldBeTrue();
    }

    [Fact]
    public void CanSetEvaluateInParallel()
    {
        // Arrange & Act
        var options = new ContentRouterOptions { EvaluateInParallel = true };

        // Assert
        options.EvaluateInParallel.ShouldBeTrue();
    }

    [Fact]
    public void CanSetMaxDegreeOfParallelism()
    {
        // Arrange & Act
        var options = new ContentRouterOptions { MaxDegreeOfParallelism = 8 };

        // Assert
        options.MaxDegreeOfParallelism.ShouldBe(8);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        // Arrange & Act
        var options = new ContentRouterOptions
        {
            EnableRouteCaching = false,
            MaxCacheSize = 2000,
            ThrowOnNoMatch = false,
            AllowMultipleMatches = true,
            EvaluateInParallel = true,
            MaxDegreeOfParallelism = 4
        };

        // Assert
        options.EnableRouteCaching.ShouldBeFalse();
        options.MaxCacheSize.ShouldBe(2000);
        options.ThrowOnNoMatch.ShouldBeFalse();
        options.AllowMultipleMatches.ShouldBeTrue();
        options.EvaluateInParallel.ShouldBeTrue();
        options.MaxDegreeOfParallelism.ShouldBe(4);
    }
}
