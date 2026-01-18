using Encina.DistributedLock;

namespace Encina.UnitTests.DistributedLock;

public sealed class LockAcquisitionExceptionTests
{
    [Fact]
    public void Constructor_WithResource_SetsMessageAndResource()
    {
        var resource = "my-resource";

        var exception = new LockAcquisitionException(resource);

        exception.Resource.ShouldBe(resource);
        exception.Message.ShouldBe("Failed to acquire lock on resource: my-resource");
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithResourceAndInnerException_SetsAllProperties()
    {
        var resource = "my-resource";
        var innerException = new InvalidOperationException("Connection failed");

        var exception = new LockAcquisitionException(resource, innerException);

        exception.Resource.ShouldBe(resource);
        exception.Message.ShouldBe("Failed to acquire lock on resource: my-resource");
        exception.InnerException.ShouldBe(innerException);
    }

    [Fact]
    public void IsException_TypeOfException()
    {
        var exception = new LockAcquisitionException("test");

        exception.ShouldBeOfType<LockAcquisitionException>();
        exception.ShouldBeAssignableTo<Exception>();
    }
}
