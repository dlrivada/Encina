using LanguageExt;

namespace SimpleMediator;

/// <summary>
/// Represents the continuation of the pipeline inside a behavior while honouring the Zero Exceptions policy.
/// </summary>
/// <typeparam name="TResponse">Type returned by the terminal handler.</typeparam>
public delegate ValueTask<Either<MediatorError, TResponse>> RequestHandlerCallback<TResponse>();
