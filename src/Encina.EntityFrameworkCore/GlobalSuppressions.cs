// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Suppress CA2263 for now - Generic overload preferred but Type parameter needed for dynamic serialization
[assembly: SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Type parameter needed for dynamic serialization scenarios", Scope = "module")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el metodo", Justification = "Expression body preferred", Scope = "member", Target = "~M:Encina.EntityFrameworkCore.Inbox.InboxMessage.IsExpired~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el metodo", Justification = "Expression body preferred", Scope = "member", Target = "~M:Encina.EntityFrameworkCore.Outbox.OutboxMessage.IsDeadLettered(System.Int32)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el metodo", Justification = "Expression body preferred", Scope = "member", Target = "~M:Encina.EntityFrameworkCore.Scheduling.ScheduledMessage.IsDeadLettered(System.Int32)~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el metodo", Justification = "Expression body preferred", Scope = "member", Target = "~M:Encina.EntityFrameworkCore.Scheduling.ScheduledMessage.IsDue~System.Boolean")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ambito de bloque", Justification = "File-scoped namespaces preferred", Scope = "namespace", Target = "~N:Encina.EntityFrameworkCore")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ambito de bloque", Justification = "File-scoped namespaces preferred", Scope = "namespace", Target = "~N:Encina.EntityFrameworkCore.Inbox")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ambito de bloque", Justification = "File-scoped namespaces preferred", Scope = "namespace", Target = "~N:Encina.EntityFrameworkCore.Outbox")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ambito de bloque", Justification = "File-scoped namespaces preferred", Scope = "namespace", Target = "~N:Encina.EntityFrameworkCore.Sagas")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ambito de bloque", Justification = "File-scoped namespaces preferred", Scope = "namespace", Target = "~N:Encina.EntityFrameworkCore.Scheduling")]
