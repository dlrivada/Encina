// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// CA1812: Internal class MongoDbIndexCreator is never instantiated
// It is instantiated by the DI container via AddHostedService
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by DI container", Scope = "type", Target = "~T:Encina.MongoDB.MongoDbIndexCreator")]

// CA1031: General exceptions are caught in Unit of Work to prevent crashes and provide error details
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exceptions are caught in Unit of Work to provide Railway Oriented Programming error handling")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el m�todo", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.MongoDB.Inbox.InboxMessage.IsExpired~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el m�todo", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.MongoDB.MongoDbIndexCreator.StopAsync(System.Threading.CancellationToken)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el m�todo", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.MongoDB.Outbox.OutboxMessage.IsDeadLettered(System.Int32)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el m�todo", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.MongoDB.Scheduling.ScheduledMessage.IsDeadLettered(System.Int32)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el m�todo", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.MongoDB.Scheduling.ScheduledMessage.IsDue~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con �mbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.MongoDB")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con �mbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.MongoDB.Inbox")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con �mbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.MongoDB.Outbox")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con �mbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.MongoDB.Sagas")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con �mbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.MongoDB.Scheduling")]
