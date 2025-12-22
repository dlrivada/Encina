// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// CA1812: Internal class MongoDbIndexCreator is never instantiated
// It is instantiated by the DI container via AddHostedService
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI container", Scope = "type", Target = "~T:Encina.MongoDB.MongoDbIndexCreator")]
