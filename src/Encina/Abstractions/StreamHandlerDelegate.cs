using System.Collections.Generic;
using LanguageExt;

namespace Encina;

/// <summary>
/// Represents the continuation of the streaming pipeline inside a behavior while honouring the Zero Exceptions policy.
/// </summary>
/// <typeparam name="TItem">Type of each item yielded by the stream.</typeparam>
/// <remarks>
/// This delegate returns an async enumerable of <c>Either&lt;EncinaError, TItem&gt;</c>,
/// allowing behaviors to enumerate the stream from the next step and yield items with
/// Railway Oriented Programming semantics.
/// </remarks>
public delegate IAsyncEnumerable<Either<EncinaError, TItem>> StreamHandlerCallback<TItem>();
