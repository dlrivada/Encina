namespace Encina;

/// <summary>
/// What a flow's chain of context holders has ever carried, ended holders included. Each holder
/// records its own facts plus those of the holder current when it was created, and keeps them after
/// it ends, so clearing or replacing the ambient context never forgets a fact.
/// </summary>
[Flags]
internal enum ChainFacts
{
    /// <summary>Nothing recorded.</summary>
    None = 0,

    /// <summary>A user identity was in the chain: no identity scope may open over it.</summary>
    User = 1,

    /// <summary>
    /// An inbound request (or an untrusted restored message) was in the chain: service and principal
    /// scopes need an explicit opt-in, the others are refused.
    /// </summary>
    Inbound = 2,

    /// <summary>
    /// A connection marker was in the chain: service, principal, built-in and restored scopes treat it
    /// as <see cref="Inbound"/>; inbound scopes (one per activity or invocation) are permitted.
    /// </summary>
    Connection = 4
}
