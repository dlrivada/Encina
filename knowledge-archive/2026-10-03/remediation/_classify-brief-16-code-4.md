Classify ONE finding from the SPEC-003 audit of closed GitHub issue #16 of the Encina .NET library. Reply with
EXACTLY one line and nothing else:

kind: bug|test|debt|docs; duplicate-of: #m|none; keywords: k1, k2, k3

- kind: "bug" for a code defect, "test" for missing tests or a coverage gap, "debt" for messy, duplicated,
  incomplete or slow code that is not itself a defect, "docs" for documentation drift.
- duplicate-of: the number of one of the candidate open issues below ONLY if it covers the exact same
  problem as this finding; otherwise "none".
- keywords: up to 3 short keywords for the finding.

Candidate open issues (from gh issue list --search, title -- first 400 characters of body):
#1170: [DEBT] ADO.NET stores have a no-op OpenConnectionAsync that never opens the connection -- ## Type - [x] Code quality (warnings, analyzers) - [x] Refactoring needed ## Description Eighteen ADO.NET stores define a private `OpenConnectionAsync` that never opens the connection: it only checks the cancellation token. The affected stores are the Auditing (AuditLog, Audit, ReadAudit), Inbox, Sagas and Scheduling stores in `Encina.ADO.SqlServer`, `Encina.ADO.PostgreSQL` and `Encina.ADO.MySQ
