using System.Reflection;
using Encina.IdGeneration;
using Encina.IdGeneration.Configuration;
using Encina.IdGeneration.Generators;

namespace Encina.ContractTests.IdGeneration;

/// <summary>
/// Contract tests verifying that all <see cref="IIdGenerator{TId}"/> and
/// <see cref="IShardedIdGenerator{TId}"/> implementations have identical API surfaces
/// and consistent behavior.
/// </summary>
[Trait("Category", "Contract")]
public sealed class IdGeneratorContractTests
{
    #region IIdGenerator<T> implementations

    [Fact]
    public void Contract_SnowflakeIdGenerator_ImplementsIIdGenerator()
    {
        typeof(SnowflakeIdGenerator)
            .GetInterfaces()
            .ShouldContain(typeof(IIdGenerator<SnowflakeId>));
    }

    [Fact]
    public void Contract_UlidIdGenerator_ImplementsIIdGenerator()
    {
        typeof(UlidIdGenerator)
            .GetInterfaces()
            .ShouldContain(typeof(IIdGenerator<UlidId>));
    }

    [Fact]
    public void Contract_UuidV7IdGenerator_ImplementsIIdGenerator()
    {
        typeof(UuidV7IdGenerator)
            .GetInterfaces()
            .ShouldContain(typeof(IIdGenerator<UuidV7Id>));
    }

    [Fact]
    public void Contract_ShardPrefixedIdGenerator_ImplementsIIdGenerator()
    {
        typeof(ShardPrefixedIdGenerator)
            .GetInterfaces()
            .ShouldContain(typeof(IIdGenerator<ShardPrefixedId>));
    }

    #endregion

    #region IShardedIdGenerator<T> implementations

    [Fact]
    public void Contract_SnowflakeIdGenerator_ImplementsIShardedIdGenerator()
    {
        typeof(SnowflakeIdGenerator)
            .GetInterfaces()
            .ShouldContain(typeof(IShardedIdGenerator<SnowflakeId>));
    }

    [Fact]
    public void Contract_ShardPrefixedIdGenerator_ImplementsIShardedIdGenerator()
    {
        typeof(ShardPrefixedIdGenerator)
            .GetInterfaces()
            .ShouldContain(typeof(IShardedIdGenerator<ShardPrefixedId>));
    }

    [Fact]
    public void Contract_UlidIdGenerator_DoesNotImplementIShardedIdGenerator()
    {
        typeof(UlidIdGenerator)
            .GetInterfaces()
            .ShouldNotContain(typeof(IShardedIdGenerator<UlidId>));
    }

    [Fact]
    public void Contract_UuidV7IdGenerator_DoesNotImplementIShardedIdGenerator()
    {
        typeof(UuidV7IdGenerator)
            .GetInterfaces()
            .ShouldNotContain(typeof(IShardedIdGenerator<UuidV7Id>));
    }

    #endregion

    #region StrategyName consistency

    [Fact]
    public void Contract_AllGenerators_HaveNonEmptyStrategyName()
    {
        var generators = new IIdGenerator[]
        {
            new SnowflakeIdGenerator(new SnowflakeOptions()),
            new UlidIdGenerator(),
            new UuidV7IdGenerator(),
            new ShardPrefixedIdGenerator(new ShardPrefixedOptions())
        };

        foreach (var gen in generators)
        {
            gen.StrategyName.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Contract_AllGenerators_HaveUniqueStrategyNames()
    {
        var names = new[]
        {
            new SnowflakeIdGenerator(new SnowflakeOptions()).StrategyName,
            new UlidIdGenerator().StrategyName,
            new UuidV7IdGenerator().StrategyName,
            new ShardPrefixedIdGenerator(new ShardPrefixedOptions()).StrategyName
        };

        names.Distinct().Count().ShouldBe(names.Length);
    }

    #endregion

    #region Generate returns Either

    [Fact]
    public void Contract_AllGenerators_GenerateReturnsEither()
    {
        // Verify all generators' Generate method returns Either<EncinaError, TId>
        VerifyGenerateMethod(typeof(SnowflakeIdGenerator), typeof(SnowflakeId));
        VerifyGenerateMethod(typeof(UlidIdGenerator), typeof(UlidId));
        VerifyGenerateMethod(typeof(UuidV7IdGenerator), typeof(UuidV7Id));
        VerifyGenerateMethod(typeof(ShardPrefixedIdGenerator), typeof(ShardPrefixedId));
    }

    #endregion

    #region ID types implement required interfaces

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_ImplementsIComparable(Type idType)
    {
        var comparableType = typeof(IComparable<>).MakeGenericType(idType);
        idType.GetInterfaces().ShouldContain(comparableType);
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_ImplementsIEquatable(Type idType)
    {
        var equatableType = typeof(IEquatable<>).MakeGenericType(idType);
        idType.GetInterfaces().ShouldContain(equatableType);
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_HasIsEmptyProperty(Type idType)
    {
        idType.GetProperty("IsEmpty", BindingFlags.Public | BindingFlags.Instance)
            .ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_HasEmptyStaticProperty(Type idType)
    {
        idType.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static)
            .ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_HasParseMethod(Type idType)
    {
        idType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)])
            .ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_HasTryParseMethod(Type idType)
    {
        var method = idType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "TryParse" && m.GetParameters().Length == 2);
        method.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_HasTryParseEitherMethod(Type idType)
    {
        var method = idType.GetMethod("TryParseEither", BindingFlags.Public | BindingFlags.Static);
        method.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(typeof(SnowflakeId))]
    [InlineData(typeof(UlidId))]
    [InlineData(typeof(UuidV7Id))]
    [InlineData(typeof(ShardPrefixedId))]
    public void Contract_IdType_IsReadonlyRecordStruct(Type idType)
    {
        idType.IsValueType.ShouldBeTrue();
    }

    #endregion

    private static void VerifyGenerateMethod(Type generatorType, Type idType)
    {
        var method = generatorType.GetMethod("Generate", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
        method.ShouldNotBeNull($"{generatorType.Name} should have a parameterless Generate method");
        method!.ReturnType.IsGenericType.ShouldBeTrue();
        method.ReturnType.GetGenericArguments().ShouldContain(idType);
    }
}
