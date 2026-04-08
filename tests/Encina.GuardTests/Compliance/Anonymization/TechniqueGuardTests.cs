using Encina.Compliance.Anonymization.Techniques;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for all <see cref="IAnonymizationTechnique"/> implementations
/// to verify null parameter handling on CanApply and ApplyAsync methods.
/// </summary>
public class TechniqueGuardTests
{
    #region DataMaskingTechnique

    [Fact]
    public void DataMasking_CanApply_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new DataMaskingTechnique();

        Action act = () => sut.CanApply(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public async Task DataMasking_ApplyAsync_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new DataMaskingTechnique();

        var act = async () => await sut.ApplyAsync("test", null!, null);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public void DataMasking_CanApply_ValidType_DoesNotThrow()
    {
        var sut = new DataMaskingTechnique();

        var result = sut.CanApply(typeof(string));

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task DataMasking_ApplyAsync_ValidStringValue_ReturnsRight()
    {
        var sut = new DataMaskingTechnique();

        var result = await sut.ApplyAsync("hello@example.com", typeof(string), null);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region GeneralizationTechnique

    [Fact]
    public void Generalization_CanApply_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new GeneralizationTechnique();

        Action act = () => sut.CanApply(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public async Task Generalization_ApplyAsync_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new GeneralizationTechnique();

        var act = async () => await sut.ApplyAsync("test", null!, null);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public void Generalization_CanApply_ValidType_DoesNotThrow()
    {
        var sut = new GeneralizationTechnique();

        var result = sut.CanApply(typeof(int));

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task Generalization_ApplyAsync_ValidIntValue_ReturnsRight()
    {
        var sut = new GeneralizationTechnique();

        var result = await sut.ApplyAsync(34, typeof(int), null);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region PerturbationTechnique

    [Fact]
    public void Perturbation_CanApply_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new PerturbationTechnique();

        Action act = () => sut.CanApply(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public async Task Perturbation_ApplyAsync_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new PerturbationTechnique();

        var act = async () => await sut.ApplyAsync(42, null!, null);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public void Perturbation_CanApply_ValidType_DoesNotThrow()
    {
        var sut = new PerturbationTechnique();

        var result = sut.CanApply(typeof(double));

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task Perturbation_ApplyAsync_ValidDoubleValue_ReturnsRight()
    {
        var sut = new PerturbationTechnique();

        var result = await sut.ApplyAsync(100.0, typeof(double), null);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region SuppressionTechnique

    [Fact]
    public async Task Suppression_ApplyAsync_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new SuppressionTechnique();

        var act = async () => await sut.ApplyAsync("test", null!, null);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public void Suppression_CanApply_AnyType_ReturnsTrue()
    {
        var sut = new SuppressionTechnique();

        sut.CanApply(typeof(string)).ShouldBeTrue();
        sut.CanApply(typeof(int)).ShouldBeTrue();
    }

    [Fact]
    public async Task Suppression_ApplyAsync_ValueTypeInput_ReturnsRight()
    {
        var sut = new SuppressionTechnique();

        var result = await sut.ApplyAsync(42, typeof(int), null);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region SwappingTechnique

    [Fact]
    public async Task Swapping_ApplyAsync_NullValueType_ThrowsArgumentNullException()
    {
        var sut = new SwappingTechnique();

        var act = async () => await sut.ApplyAsync("test", null!, null);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("valueType");
    }

    [Fact]
    public void Swapping_CanApply_AnyType_ReturnsTrue()
    {
        var sut = new SwappingTechnique();

        sut.CanApply(typeof(string)).ShouldBeTrue();
        sut.CanApply(typeof(int)).ShouldBeTrue();
    }

    [Fact]
    public async Task Swapping_ApplyAsync_ValueTypeInput_ReturnsRight()
    {
        var sut = new SwappingTechnique();

        var result = await sut.ApplyAsync(42, typeof(int), null);

        result.IsRight.ShouldBeTrue();
    }

    #endregion
}
