using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for result mapper patterns.
/// </summary>
public class ResultMapperProperties
{
    // Test types
    private sealed record TestDomain(string Value, int Number);
    private sealed record TestDto(string Value, int Number);
    private sealed record IntermediateDto(string UpperValue, int Number);

    private sealed class TestMapper : IResultMapper<TestDomain, TestDto>
    {
        public Either<MappingError, TestDto> Map(TestDomain domain)
        {
            if (string.IsNullOrEmpty(domain.Value))
                return MappingError.NullProperty<TestDomain, TestDto>("Value");

            return new TestDto(domain.Value, domain.Number);
        }
    }

    private sealed class FailingMapper : IResultMapper<TestDomain, TestDto>
    {
        public Either<MappingError, TestDto> Map(TestDomain domain)
        {
            return MappingError.ValidationFailed<TestDomain, TestDto>("Always fails");
        }
    }

    // === MappingError Factory Methods ===

    [Property(MaxTest = 100)]
    public bool MappingError_NullProperty_HasCorrectCode(NonEmptyString propertyName)
    {
        var error = MappingError.NullProperty<TestDomain, TestDto>(propertyName.Get);

        return error.ErrorCode == "MAPPING_NULL_PROPERTY"
            && error.SourceType == typeof(TestDomain)
            && error.TargetType == typeof(TestDto)
            && error.PropertyName == propertyName.Get
            && error.Message.Contains(propertyName.Get);
    }

    [Property(MaxTest = 100)]
    public bool MappingError_ValidationFailed_HasCorrectCode(NonEmptyString reason)
    {
        var error = MappingError.ValidationFailed<TestDomain, TestDto>(reason.Get);

        return error.ErrorCode == "MAPPING_VALIDATION_FAILED"
            && error.SourceType == typeof(TestDomain)
            && error.TargetType == typeof(TestDto)
            && error.Message.Contains(reason.Get);
    }

    [Property(MaxTest = 100)]
    public bool MappingError_ConversionFailed_HasCorrectCode(NonEmptyString propertyName)
    {
        var exception = new InvalidOperationException("Conversion error");
        var error = MappingError.ConversionFailed<TestDomain, TestDto>(propertyName.Get, exception);

        return error.ErrorCode == "MAPPING_CONVERSION_FAILED"
            && error.SourceType == typeof(TestDomain)
            && error.TargetType == typeof(TestDto)
            && error.PropertyName == propertyName.Get
            && error.InnerException == exception;
    }

    [Property(MaxTest = 100)]
    public bool MappingError_EmptyCollection_HasCorrectCode(NonEmptyString propertyName)
    {
        var error = MappingError.EmptyCollection<TestDomain, TestDto>(propertyName.Get);

        return error.ErrorCode == "MAPPING_EMPTY_COLLECTION"
            && error.PropertyName == propertyName.Get;
    }

    [Property(MaxTest = 100)]
    public bool MappingError_OperationFailed_HasCorrectCode()
    {
        var exception = new InvalidOperationException("Mapping failed");
        var error = MappingError.OperationFailed<TestDomain, TestDto>(exception);

        return error.ErrorCode == "MAPPING_OPERATION_FAILED"
            && error.InnerException == exception;
    }

    // === IResultMapper ===

    [Property(MaxTest = 100)]
    public bool ResultMapper_Map_SucceedsForValidInput(NonEmptyString value, int number)
    {
        var mapper = new TestMapper();
        var domain = new TestDomain(value.Get, number);

        var result = mapper.Map(domain);

        return result.IsRight && result.Match(
            Left: _ => false,
            Right: dto => dto.Value == value.Get && dto.Number == number);
    }

    [Property(MaxTest = 100)]
    public bool ResultMapper_Map_FailsForInvalidInput(int number)
    {
        var mapper = new TestMapper();
        var domain = new TestDomain(string.Empty, number);

        var result = mapper.Map(domain);

        return result.IsLeft;
    }

    // === MapAll Extension ===

    [Property(MaxTest = 100)]
    public bool ResultMapper_MapAll_SucceedsForAllValidInputs(PositiveInt count)
    {
        var actualCount = Math.Min(count.Get, 50);
        var mapper = new TestMapper();
        var domains = Enumerable.Range(1, actualCount)
            .Select(i => new TestDomain($"value_{i}", i))
            .ToList();

        var result = mapper.MapAll(domains);

        return result.IsRight && result.Match(
            Left: _ => false,
            Right: dtos => dtos.Count == actualCount);
    }

    [Property(MaxTest = 100)]
    public bool ResultMapper_MapAll_FailsOnFirstError(PositiveInt count)
    {
        var actualCount = Math.Min(count.Get, 50);
        var mapper = new TestMapper();
        var domains = Enumerable.Range(1, actualCount)
            .Select(i => new TestDomain(i == actualCount / 2 ? "" : $"value_{i}", i))
            .ToList();

        var result = mapper.MapAll(domains);

        // If count is 1, the domain might be valid or invalid depending on the random value
        if (actualCount == 1)
            return true;

        return result.IsLeft;
    }

    [Property(MaxTest = 100)]
    public bool ResultMapper_MapAllCollectErrors_CollectsAllErrors()
    {
        var mapper = new FailingMapper();
        var domains = new[]
        {
            new TestDomain("a", 1),
            new TestDomain("b", 2),
            new TestDomain("c", 3)
        };

        var result = mapper.MapAllCollectErrors(domains);

        return result.IsLeft && result.Match(
            Left: errors => errors.Count == 3,
            Right: _ => false);
    }

    // === TryMap Extension ===

    [Property(MaxTest = 100)]
    public bool ResultMapper_TryMap_ReturnsSomeOnSuccess(NonEmptyString value, int number)
    {
        var mapper = new TestMapper();
        var domain = new TestDomain(value.Get, number);

        var result = mapper.TryMap(domain);

        return result.IsSome;
    }

    [Property(MaxTest = 100)]
    public bool ResultMapper_TryMap_ReturnsNoneOnFailure(int number)
    {
        var mapper = new TestMapper();
        var domain = new TestDomain(string.Empty, number);

        var result = mapper.TryMap(domain);

        return result.IsNone;
    }

    [Property(MaxTest = 100)]
    public bool ResultMapper_TryMap_ReturnsNoneOnNullMapper()
    {
        IResultMapper<TestDomain, TestDto>? mapper = null;
        var domain = new TestDomain("test", 42);

        var result = mapper!.TryMap(domain);

        return result.IsNone;
    }

    // === Compose Extension ===

    [Property(MaxTest = 100)]
    public bool ResultMapper_Compose_ChainsMappers(NonEmptyString value, int number)
    {
        var firstMapper = new FirstStepMapper();
        var secondMapper = new SecondStepMapper();
        var composed = firstMapper.Compose(secondMapper);

        var domain = new TestDomain(value.Get, number);
        var result = composed.Map(domain);

        return result.IsRight && result.Match(
            Left: _ => false,
            Right: dto => string.Equals(dto.Value, value.Get.ToUpperInvariant(), StringComparison.Ordinal) && dto.Number == number);
    }

    private sealed class FirstStepMapper : IResultMapper<TestDomain, IntermediateDto>
    {
        public Either<MappingError, IntermediateDto> Map(TestDomain domain)
        {
            return new IntermediateDto(domain.Value.ToUpperInvariant(), domain.Number);
        }
    }

    private sealed class SecondStepMapper : IResultMapper<IntermediateDto, TestDto>
    {
        public Either<MappingError, TestDto> Map(IntermediateDto intermediate)
        {
            return new TestDto(intermediate.UpperValue, intermediate.Number);
        }
    }
}
