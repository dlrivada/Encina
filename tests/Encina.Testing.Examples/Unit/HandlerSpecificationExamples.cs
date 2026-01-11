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
    /// Pattern: Modifying request via GivenRequest (for immutable records).
    /// </summary>
    [Fact]
    public async Task GivenModifiedRequest_ShouldUseNewValues()
    {
        // Given - Use GivenRequest for immutable records
        GivenRequest(new CreateOrderCommand
        {
            CustomerId = "PREMIUM-001",
            Amount = 500m,
            Notes = "Modified order"
        });

        // When
        await When();

        // Then
        ThenSuccess();
    }

    /// <summary>
    /// Pattern: GivenRequest with modified amount.
    /// </summary>
    [Fact]
    public async Task WhenWithHighAmount_ShouldSucceed()
    {
        // Given - Use GivenRequest for immutable records
        GivenRequest(new CreateOrderCommand
        {
            CustomerId = "CUST-001",
            Amount = 1000m
        });

        // When
        await When();

        // Then
        ThenSuccess();
    }

    /// <summary>
    /// Pattern: Validation error assertion.
    /// </summary>
    [Fact]
    public async Task EmptyCustomerId_ShouldReturnValidationError()
    {
        // Given - Invalid request (empty customer ID)
        GivenRequest(new CreateOrderCommand
        {
            CustomerId = "",
            Amount = 100m
        });

        // When
        await When();

        // Then - Check that error message contains "Customer ID"
        ThenError(error => error.Message.ShouldContain("Customer ID"));
    }

    /// <summary>
    /// Pattern: Multiple validation errors.
    /// </summary>
    [Fact]
    public async Task InvalidAmountAndCustomer_ShouldReturnValidationErrors()
    {
        // Given - Multiple invalid fields
        GivenRequest(new CreateOrderCommand
        {
            CustomerId = "",
            Amount = 0
        });

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
        // Given - Zero amount is invalid
        GivenRequest(new CreateOrderCommand
        {
            CustomerId = "CUST-001",
            Amount = 0
        });

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
        // Given - Empty customer ID causes validation error
        GivenRequest(new CreateOrderCommand
        {
            CustomerId = "",
            Amount = 100m
        });

        // When
        await When();

        // Then - Fluent chaining on error
        ThenErrorAnd()
            .ShouldSatisfy(e => e.Message.ShouldContain("Customer"));
    }
}
