using System.Collections.Immutable;
using System.Security.Claims;
using Encina.Security;
using FluentAssertions;
using NSubstitute;

namespace Encina.UnitTests.Security;

/// <summary>
/// Unit tests for <see cref="DefaultPermissionEvaluator"/> and <see cref="DefaultResourceOwnershipEvaluator"/>.
/// </summary>
public class EvaluatorTests
{
    #region DefaultPermissionEvaluator

    public class DefaultPermissionEvaluatorTests
    {
        private static readonly string[] ReadWritePermissions = ["orders:read", "orders:write"];
        private static readonly string[] ReadPermissions = ["orders:read"];

        private readonly DefaultPermissionEvaluator _evaluator = new();

        [Fact]
        public async Task HasPermission_WithMatchingPermission_ShouldReturnTrue()
        {
            // Arrange
            var context = CreateContext(permissions: ["orders:read", "orders:write"]);

            // Act
            var result = await _evaluator.HasPermissionAsync(context, "orders:read");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasPermission_WithoutMatchingPermission_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext(permissions: ["orders:read"]);

            // Act
            var result = await _evaluator.HasPermissionAsync(context, "orders:delete");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasPermission_EmptyPermissions_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext(permissions: []);

            // Act
            var result = await _evaluator.HasPermissionAsync(context, "orders:read");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasAnyPermission_WithOneMatching_ShouldReturnTrue()
        {
            // Arrange
            var context = CreateContext(permissions: ["orders:read"]);

            // Act
            var result = await _evaluator.HasAnyPermissionAsync(
                context, ReadWritePermissions);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasAnyPermission_WithNoneMatching_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext(permissions: ["users:read"]);

            // Act
            var result = await _evaluator.HasAnyPermissionAsync(
                context, ReadWritePermissions);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasAnyPermission_WithAllMatching_ShouldReturnTrue()
        {
            // Arrange
            var context = CreateContext(permissions: ["orders:read", "orders:write"]);

            // Act
            var result = await _evaluator.HasAnyPermissionAsync(
                context, ReadWritePermissions);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasAllPermissions_WithAllPresent_ShouldReturnTrue()
        {
            // Arrange
            var context = CreateContext(permissions: ["orders:read", "orders:write", "orders:delete"]);

            // Act
            var result = await _evaluator.HasAllPermissionsAsync(
                context, ReadWritePermissions);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task HasAllPermissions_WithSomeMissing_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext(permissions: ["orders:read"]);

            // Act
            var result = await _evaluator.HasAllPermissionsAsync(
                context, ReadWritePermissions);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasAllPermissions_WithNonePresent_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext(permissions: []);

            // Act
            var result = await _evaluator.HasAllPermissionsAsync(
                context, ReadPermissions);

            // Assert
            result.Should().BeFalse();
        }

        // -- Guard clauses --

        [Fact]
        public async Task HasPermission_NullContext_ShouldThrow()
        {
            Func<Task> act = async () => await _evaluator.HasPermissionAsync(null!, "read");
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
        }

        [Fact]
        public async Task HasPermission_NullPermission_ShouldThrow()
        {
            var context = CreateContext(permissions: []);
            Func<Task> act = async () => await _evaluator.HasPermissionAsync(context, null!);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("permission");
        }

        [Fact]
        public async Task HasPermission_EmptyPermission_ShouldThrow()
        {
            var context = CreateContext(permissions: []);
            Func<Task> act = async () => await _evaluator.HasPermissionAsync(context, "");
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("permission");
        }

        [Fact]
        public async Task HasAnyPermission_NullContext_ShouldThrow()
        {
            Func<Task> act = async () => await _evaluator.HasAnyPermissionAsync(null!, ["read"]);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
        }

        [Fact]
        public async Task HasAnyPermission_NullPermissions_ShouldThrow()
        {
            var context = CreateContext(permissions: []);
            Func<Task> act = async () => await _evaluator.HasAnyPermissionAsync(context, null!);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("permissions");
        }

        [Fact]
        public async Task HasAllPermissions_NullContext_ShouldThrow()
        {
            Func<Task> act = async () => await _evaluator.HasAllPermissionsAsync(null!, ["read"]);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
        }

        [Fact]
        public async Task HasAllPermissions_NullPermissions_ShouldThrow()
        {
            var context = CreateContext(permissions: []);
            Func<Task> act = async () => await _evaluator.HasAllPermissionsAsync(context, null!);
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("permissions");
        }

        private static ISecurityContext CreateContext(string[] permissions)
        {
            var context = Substitute.For<ISecurityContext>();
            context.Permissions.Returns(permissions.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase));
            return context;
        }
    }

    #endregion

    #region DefaultResourceOwnershipEvaluator

    public class DefaultResourceOwnershipEvaluatorTests
    {
        private readonly DefaultResourceOwnershipEvaluator _evaluator = new();

        [Fact]
        public async Task IsOwner_MatchingProperty_ShouldReturnTrue()
        {
            // Arrange
            var context = CreateContext("user-1");
            var resource = new TestResource { OwnerId = "user-1" };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "OwnerId");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsOwner_NonMatchingProperty_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext("user-1");
            var resource = new TestResource { OwnerId = "user-2" };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "OwnerId");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsOwner_NonExistentProperty_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext("user-1");
            var resource = new TestResource { OwnerId = "user-1" };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "NonExistent");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsOwner_NullPropertyValue_ShouldReturnFalse()
        {
            // Arrange
            var context = CreateContext("user-1");
            var resource = new TestResource { OwnerId = null! };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "OwnerId");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsOwner_NullUserId_ShouldReturnFalse()
        {
            // Arrange
            var context = Substitute.For<ISecurityContext>();
            context.UserId.Returns((string?)null);
            var resource = new TestResource { OwnerId = "user-1" };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "OwnerId");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsOwner_GuidProperty_ShouldCompareToString()
        {
            // Arrange
            var guid = Guid.NewGuid();
            var context = CreateContext(guid.ToString());
            var resource = new GuidOwnerResource { OwnerId = guid };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "OwnerId");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsOwner_CaseSensitive_ShouldNotMatch()
        {
            // Arrange
            var context = CreateContext("User-1");
            var resource = new TestResource { OwnerId = "user-1" };

            // Act
            var result = await _evaluator.IsOwnerAsync(context, resource, "OwnerId");

            // Assert — Ordinal comparison is case-sensitive
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsOwner_SamePropertyCalledTwice_ShouldUseCachedReflection()
        {
            // Arrange — call twice for same type+property to exercise cache
            var context = CreateContext("user-1");
            var resource1 = new TestResource { OwnerId = "user-1" };
            var resource2 = new TestResource { OwnerId = "user-1" };

            // Act
            var result1 = await _evaluator.IsOwnerAsync(context, resource1, "OwnerId");
            var result2 = await _evaluator.IsOwnerAsync(context, resource2, "OwnerId");

            // Assert — both should succeed using cached PropertyInfo
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        // -- Guard clauses --

        [Fact]
        public async Task IsOwner_NullContext_ShouldThrow()
        {
            var resource = new TestResource();
            Func<Task> act = async () => await _evaluator.IsOwnerAsync<TestResource>(null!, resource, "OwnerId");
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("context");
        }

        [Fact]
        public async Task IsOwner_NullResource_ShouldThrow()
        {
            var context = CreateContext("user-1");
            Func<Task> act = async () => await _evaluator.IsOwnerAsync<TestResource>(context, null!, "OwnerId");
            await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("resource");
        }

        [Fact]
        public async Task IsOwner_NullPropertyName_ShouldThrow()
        {
            var context = CreateContext("user-1");
            var resource = new TestResource();
            Func<Task> act = async () => await _evaluator.IsOwnerAsync(context, resource, null!);
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("propertyName");
        }

        [Fact]
        public async Task IsOwner_EmptyPropertyName_ShouldThrow()
        {
            var context = CreateContext("user-1");
            var resource = new TestResource();
            Func<Task> act = async () => await _evaluator.IsOwnerAsync(context, resource, "");
            await act.Should().ThrowAsync<ArgumentException>().WithParameterName("propertyName");
        }

        private static ISecurityContext CreateContext(string userId)
        {
            var context = Substitute.For<ISecurityContext>();
            context.UserId.Returns(userId);
            return context;
        }
    }

    #endregion

    #region Test Types

    private sealed class TestResource
    {
        public string OwnerId { get; init; } = string.Empty;
    }

    private sealed class GuidOwnerResource
    {
        public Guid OwnerId { get; init; }
    }

    #endregion
}
