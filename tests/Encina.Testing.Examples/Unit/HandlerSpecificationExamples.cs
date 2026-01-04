using Encina.Testing.Examples.Domain;
using Encina.Testing.Handlers;

namespace Encina.Testing.Examples.Unit;

/// <summary>
/// Examples demonstrating HandlerSpecification BDD pattern for handler tests.
/// Reference: docs/plans/testing-dogfooding-plan.md Section 5.3
/// </summary>
public sealed class CreateOrderHandlerSpecs : HandlerSpecification<CreateOrderCommand, Guid>
{
    /// <summary>
    /// Creates the default request for testing.
    /// </summary>
    protected override CreateOrderCommand CreateRequest() => new()
    {
        CustomerId = "CUST-001",
        Amount = 99.99m,
        Notes = "Test order"
    };

    /// <summary>
    /// Creates the handler instance to test.
    /// </summary>
    protected override IRequestHandler<CreateOrderCommand, Guid> CreateHandler() =>
        new CreateOrderHandler();

    /// <summary>
    /// Pattern: Basic success assertion using BDD Given/When/Then.
    /// </summary>
    [Fact]
    public async Task ValidOrder_ShouldSucceed()
    {
        // Given - Use default request (valid data)

        // When
        await When();

        // Then
        ThenSuccess(orderId => orderId.ShouldNotBe(Guid.Empty));
    }

    /// <summary>
    /// Pattern: Modifying request via Given.
    /// </summary>
    [Fact]
    public async Task GivenModifiedRequest_ShouldUseNewValues()
    {
        // Given - Modify the request
        Given(r => r = r with { CustomerId = "PREMIUM-001", Amount = 500m });

        // When
        await When();

        // Then
        ThenSuccess();
    }

    /// <summary>
    /// Pattern: Inline modification via When overload.
    /// </summary>
    [Fact]
    public async Task WhenWithModification_ShouldApplyChanges()
    {
        // When with inline modification
        await When(r => r = r with { Amount = 1000m });

        // Then
        ThenSuccess();
    }

    /// <summary>
    /// Pattern: Validation error assertion.
    /// </summary>
    [Fact]
    public async Task EmptyCustomerId_ShouldReturnValidationError()
    {
        // Given - Invalid request
        Given(r => r = r with { CustomerId = "" });

        // When
        await When();

        // Then - Validation error for CustomerId
        ThenValidationError("CustomerId");
    }

    /// <summary>
    /// Pattern: Multiple validation errors.
    /// </summary>
    [Fact]
    public async Task InvalidAmountAndCustomer_ShouldReturnValidationErrors()
    {
        // Given - Multiple invalid fields
        Given(r => r = r with { CustomerId = "", Amount = 0 });

        // When
        await When();

        // Then - Validate error is returned (can check specific properties)
        ThenError(error => error.Message.ShouldNotBeNullOrWhiteSpace());
    }

    /// <summary>
    /// Pattern: Error with specific code.
    /// </summary>
    [Fact]
    public async Task ZeroAmount_ShouldReturnValidationErrorWithCode()
    {
        // Given
        Given(r => r = r with { Amount = 0 });

        // When
        await When();

        // Then - Check error starts with validation prefix
        ThenError(error =>
        {
            var code = error.GetCode().IfNone("");
            code.ShouldStartWith("encina.validation");
        });
    }

    /// <summary>
    /// Pattern: Fluent assertions with ThenSuccessAnd.
    /// </summary>
    [Fact]
    public async Task ValidRequest_ShouldReturnNewGuid()
    {
        // When
        await When();

        // Then - Fluent chaining
        ThenSuccessAnd()
            .ShouldSatisfy(id => id.ShouldNotBe(Guid.Empty));
    }

    /// <summary>
    /// Pattern: Fluent assertions with ThenErrorAnd.
    /// </summary>
    [Fact]
    public async Task InvalidRequest_ShouldReturnDescriptiveError()
    {
        // Given
        Given(r => r = r with { CustomerId = "" });

        // When
        await When();

        // Then - Fluent chaining on error
        ThenErrorAnd()
            .ShouldSatisfy(e => e.Message.ShouldContain("Customer"));
    }
}
