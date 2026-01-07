using Shouldly;
using Xunit;

namespace Encina.EntityFrameworkCore.Tests;

/// <summary>
/// Unit tests for <see cref="ITransactionalCommand"/> interface.
/// </summary>
public sealed class ITransactionalCommandTests
{
    [Fact]
    public void ITransactionalCommand_IsInterface()
    {
        // Assert
        typeof(ITransactionalCommand).IsInterface.ShouldBeTrue();
    }

    [Fact]
    public void ITransactionalCommand_HasNoMembers()
    {
        // Assert - Marker interface has no members
        typeof(ITransactionalCommand).GetMembers()
            .Where(m => m.DeclaringType == typeof(ITransactionalCommand))
            .ShouldBeEmpty();
    }

    [Fact]
    public void ClassCanImplementITransactionalCommand()
    {
        // Arrange
        var command = new TestTransactionalCommand();

        // Act & Assert
        command.ShouldBeAssignableTo<ITransactionalCommand>();
    }

    [Fact]
    public void RecordCanImplementITransactionalCommand()
    {
        // Arrange
        var command = new TestTransactionalRecord();

        // Act & Assert
        command.ShouldBeAssignableTo<ITransactionalCommand>();
    }

    [Fact]
    public void MultipleInterfacesCanBeCombined()
    {
        // Arrange
        var command = new TestCombinedCommand();

        // Act & Assert
        command.ShouldBeAssignableTo<ITransactionalCommand>();
        command.ShouldBeAssignableTo<IRequest<string>>();
    }

    private sealed class TestTransactionalCommand : ITransactionalCommand
    {
    }

    private sealed record TestTransactionalRecord : ITransactionalCommand;

    private sealed record TestCombinedCommand : IRequest<string>, ITransactionalCommand;
}
