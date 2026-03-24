using System.Reflection;
using Encina.Redis.PubSub;
using LanguageExt;
using NSubstitute;

namespace Encina.ContractTests.Messaging.RedisPubSub;

/// <summary>
/// Contract tests verifying that the <see cref="IRedisPubSubMessagePublisher"/> interface
/// shape and <see cref="RedisPubSubMessagePublisher"/> implementation conform to the
/// expected contract for Redis Pub/Sub message publishing, subscribing, and pattern-based
/// subscriptions.
/// </summary>
[Trait("Category", "Contract")]
public sealed class RedisPubSubPublisherContractTests
{
    #region IRedisPubSubMessagePublisher Interface Shape

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher"/> must define exactly 3 methods.
    /// </summary>
    [Fact]
    public void Contract_IRedisPubSubMessagePublisher_ShouldHave_ThreeMethods()
    {
        // Arrange
        var type = typeof(IRedisPubSubMessagePublisher);

        // Act
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        methods.Length.ShouldBe(3,
            "IRedisPubSubMessagePublisher must define exactly 3 methods: " +
            "PublishAsync, SubscribeAsync, SubscribePatternAsync");
    }

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher.PublishAsync{TMessage}"/>
    /// must have the expected parameter signature.
    /// </summary>
    [Fact]
    public void Contract_PublishAsync_ParameterSignature()
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod("PublishAsync");

        // Assert
        method.ShouldNotBeNull("PublishAsync must exist on IRedisPubSubMessagePublisher");
        method.IsGenericMethodDefinition.ShouldBeTrue(
            "PublishAsync must be a generic method with TMessage type parameter");

        var genericMethod = method.MakeGenericMethod(typeof(TestMessage));
        var parameters = genericMethod.GetParameters();
        parameters.Length.ShouldBe(3,
            "PublishAsync must accept (TMessage message, string? channel, CancellationToken)");

        parameters[0].Name.ShouldBe("message");
        parameters[1].Name.ShouldBe("channel");
        parameters[1].HasDefaultValue.ShouldBeTrue("channel parameter must have a default value");
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
        parameters[2].HasDefaultValue.ShouldBeTrue("CancellationToken must have a default value");
    }

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher.PublishAsync{TMessage}"/>
    /// must return <c>ValueTask&lt;Either&lt;EncinaError, long&gt;&gt;</c>.
    /// </summary>
    [Fact]
    public void Contract_PublishAsync_ReturnType()
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod("PublishAsync")!;
        var genericMethod = method.MakeGenericMethod(typeof(TestMessage));
        var returnType = genericMethod.ReturnType;

        // Assert
        returnType.IsGenericType.ShouldBeTrue();
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));

        var innerType = returnType.GetGenericArguments()[0];
        innerType.IsGenericType.ShouldBeTrue();

        var eitherArgs = innerType.GetGenericArguments();
        eitherArgs[0].ShouldBe(typeof(EncinaError),
            "PublishAsync Left type must be EncinaError");
        eitherArgs[1].ShouldBe(typeof(long),
            "PublishAsync Right type must be long (subscriber count)");
    }

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher.SubscribeAsync{TMessage}"/>
    /// must have the expected parameter signature.
    /// </summary>
    [Fact]
    public void Contract_SubscribeAsync_ParameterSignature()
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod("SubscribeAsync");

        // Assert
        method.ShouldNotBeNull("SubscribeAsync must exist on IRedisPubSubMessagePublisher");
        method.IsGenericMethodDefinition.ShouldBeTrue(
            "SubscribeAsync must be a generic method with TMessage type parameter");

        var genericMethod = method.MakeGenericMethod(typeof(TestMessage));
        var parameters = genericMethod.GetParameters();
        parameters.Length.ShouldBe(3,
            "SubscribeAsync must accept (Func<TMessage, ValueTask> handler, string? channel, CancellationToken)");

        parameters[0].Name.ShouldBe("handler");
        parameters[1].Name.ShouldBe("channel");
        parameters[1].HasDefaultValue.ShouldBeTrue("channel parameter must have a default value");
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher.SubscribeAsync{TMessage}"/>
    /// must return <c>ValueTask&lt;IAsyncDisposable&gt;</c>.
    /// </summary>
    [Fact]
    public void Contract_SubscribeAsync_ReturnType()
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod("SubscribeAsync")!;
        var genericMethod = method.MakeGenericMethod(typeof(TestMessage));
        var returnType = genericMethod.ReturnType;

        // Assert
        returnType.IsGenericType.ShouldBeTrue();
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
        returnType.GetGenericArguments()[0].ShouldBe(typeof(IAsyncDisposable),
            "SubscribeAsync must return ValueTask<IAsyncDisposable>");
    }

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher.SubscribePatternAsync{TMessage}"/>
    /// must have the expected parameter signature.
    /// </summary>
    [Fact]
    public void Contract_SubscribePatternAsync_ParameterSignature()
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod("SubscribePatternAsync");

        // Assert
        method.ShouldNotBeNull("SubscribePatternAsync must exist on IRedisPubSubMessagePublisher");
        method.IsGenericMethodDefinition.ShouldBeTrue(
            "SubscribePatternAsync must be a generic method with TMessage type parameter");

        var genericMethod = method.MakeGenericMethod(typeof(TestMessage));
        var parameters = genericMethod.GetParameters();
        parameters.Length.ShouldBe(3,
            "SubscribePatternAsync must accept (string pattern, Func<string, TMessage, ValueTask> handler, CancellationToken)");

        parameters[0].Name.ShouldBe("pattern");
        parameters[0].ParameterType.ShouldBe(typeof(string));
        parameters[1].Name.ShouldBe("handler");
        parameters[2].ParameterType.ShouldBe(typeof(CancellationToken));
    }

    /// <summary>
    /// Contract: <see cref="IRedisPubSubMessagePublisher.SubscribePatternAsync{TMessage}"/>
    /// must return <c>ValueTask&lt;IAsyncDisposable&gt;</c>.
    /// </summary>
    [Fact]
    public void Contract_SubscribePatternAsync_ReturnType()
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod("SubscribePatternAsync")!;
        var genericMethod = method.MakeGenericMethod(typeof(TestMessage));
        var returnType = genericMethod.ReturnType;

        // Assert
        returnType.IsGenericType.ShouldBeTrue();
        returnType.GetGenericTypeDefinition().ShouldBe(typeof(ValueTask<>));
        returnType.GetGenericArguments()[0].ShouldBe(typeof(IAsyncDisposable),
            "SubscribePatternAsync must return ValueTask<IAsyncDisposable>");
    }

    #endregion

    #region Implementation Conformance

    /// <summary>
    /// Contract: <see cref="RedisPubSubMessagePublisher"/> must implement
    /// <see cref="IRedisPubSubMessagePublisher"/>.
    /// </summary>
    [Fact]
    public void Contract_RedisPubSubMessagePublisher_ImplementsInterface()
    {
        typeof(IRedisPubSubMessagePublisher)
            .IsAssignableFrom(typeof(RedisPubSubMessagePublisher))
            .ShouldBeTrue("RedisPubSubMessagePublisher must implement IRedisPubSubMessagePublisher");
    }

    /// <summary>
    /// Contract: <see cref="RedisPubSubMessagePublisher"/> must be sealed.
    /// </summary>
    [Fact]
    public void Contract_RedisPubSubMessagePublisher_IsSealed()
    {
        typeof(RedisPubSubMessagePublisher).IsSealed.ShouldBeTrue(
            "RedisPubSubMessagePublisher must be sealed");
    }

    #endregion

    #region Null Guard Contract

    /// <summary>
    /// Contract: <see cref="RedisPubSubMessagePublisher.PublishAsync{TMessage}"/>
    /// with null message must throw <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Contract_PublishAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var redis = NSubstitute.Substitute.For<StackExchange.Redis.IConnectionMultiplexer>();
        var subscriber = NSubstitute.Substitute.For<StackExchange.Redis.ISubscriber>();
        redis.GetSubscriber().Returns(subscriber);

        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RedisPubSubMessagePublisher>>();
        var options = Microsoft.Extensions.Options.Options.Create(new EncinaRedisPubSubOptions());
        var publisher = new RedisPubSubMessagePublisher(redis, logger, options);

        // Act & Assert
        Should.ThrowAsync<ArgumentNullException>(
            async () => await publisher.PublishAsync<TestMessage>(null!))
            .Result.ParamName.ShouldBe("message");
    }

    /// <summary>
    /// Contract: <see cref="RedisPubSubMessagePublisher.SubscribeAsync{TMessage}"/>
    /// with null handler must throw <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Contract_SubscribeAsync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var redis = NSubstitute.Substitute.For<StackExchange.Redis.IConnectionMultiplexer>();
        var subscriber = NSubstitute.Substitute.For<StackExchange.Redis.ISubscriber>();
        redis.GetSubscriber().Returns(subscriber);

        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RedisPubSubMessagePublisher>>();
        var options = Microsoft.Extensions.Options.Options.Create(new EncinaRedisPubSubOptions());
        var publisher = new RedisPubSubMessagePublisher(redis, logger, options);

        // Act & Assert
        Should.ThrowAsync<ArgumentNullException>(
            async () => await publisher.SubscribeAsync<TestMessage>(null!))
            .Result.ParamName.ShouldBe("handler");
    }

    /// <summary>
    /// Contract: <see cref="RedisPubSubMessagePublisher.SubscribePatternAsync{TMessage}"/>
    /// with null pattern must throw <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Contract_SubscribePatternAsync_NullPattern_ThrowsArgumentNullException()
    {
        // Arrange
        var redis = NSubstitute.Substitute.For<StackExchange.Redis.IConnectionMultiplexer>();
        var subscriber = NSubstitute.Substitute.For<StackExchange.Redis.ISubscriber>();
        redis.GetSubscriber().Returns(subscriber);

        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RedisPubSubMessagePublisher>>();
        var options = Microsoft.Extensions.Options.Options.Create(new EncinaRedisPubSubOptions());
        var publisher = new RedisPubSubMessagePublisher(redis, logger, options);

        // Act & Assert
        Should.ThrowAsync<ArgumentNullException>(
            async () => await publisher.SubscribePatternAsync<TestMessage>(
                null!, (_, _) => ValueTask.CompletedTask))
            .Result.ParamName.ShouldBe("pattern");
    }

    /// <summary>
    /// Contract: <see cref="RedisPubSubMessagePublisher.SubscribePatternAsync{TMessage}"/>
    /// with null handler must throw <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void Contract_SubscribePatternAsync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var redis = NSubstitute.Substitute.For<StackExchange.Redis.IConnectionMultiplexer>();
        var subscriber = NSubstitute.Substitute.For<StackExchange.Redis.ISubscriber>();
        redis.GetSubscriber().Returns(subscriber);

        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<RedisPubSubMessagePublisher>>();
        var options = Microsoft.Extensions.Options.Options.Create(new EncinaRedisPubSubOptions());
        var publisher = new RedisPubSubMessagePublisher(redis, logger, options);

        // Act & Assert
        Should.ThrowAsync<ArgumentNullException>(
            async () => await publisher.SubscribePatternAsync<TestMessage>(
                "events.*", null!))
            .Result.ParamName.ShouldBe("handler");
    }

    #endregion

    #region TMessage Constraint Contract

    /// <summary>
    /// Contract: All generic methods on <see cref="IRedisPubSubMessagePublisher"/>
    /// must constrain TMessage to <c>class</c> (reference types only).
    /// </summary>
    [Theory]
    [InlineData("PublishAsync")]
    [InlineData("SubscribeAsync")]
    [InlineData("SubscribePatternAsync")]
    public void Contract_AllGenericMethods_ConstrainTMessage_ToClass(string methodName)
    {
        // Arrange
        var method = typeof(IRedisPubSubMessagePublisher).GetMethod(methodName)!;
        var typeParam = method.GetGenericArguments()[0];

        // Assert
        var constraints = typeParam.GenericParameterAttributes;
        (constraints & System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint)
            .ShouldBe(System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint,
                $"{methodName} TMessage type parameter must have a 'class' constraint");
    }

    #endregion

    /// <summary>
    /// Test message class used for generic method instantiation in contract tests.
    /// </summary>
    private sealed class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
