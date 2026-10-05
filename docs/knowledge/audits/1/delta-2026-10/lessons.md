- A feature-adjacent bug-fix audit still has one page worth reading: the README sample for the feature. Checking its handler signature against the interface found a return-type error that symbol-existence checks pass (all names exist, the return type is wrong).
Applied: role:docs-reviewer Even for a bug-fix issue, read the feature's README sample and compare each implemented interface's signatures (return types included), not only that names exist.
- The delta rule (b) prompt cannot tell the stage whether to compare against the package aggregate or a per-file number; since the manifest has only aggregates, state in the delta brief that findings propose per-file values and cite the measured per-file coverage to support them.
Applied: role:test-auditor In delta rule (b), the manifest has only package aggregates: propose per-file targets for the scoped files and back each with the measured per-file coverage (until #1762 adds the per-file field).
- The tests stage called the type in `src/Encina/Dispatchers/MediatorAssemblyScanner.cs` `MediatorAssemblyScanner`; the file declares `internal static class EncinaAssemblyScanner` (line 9). A finding about a type should name the declared type, not only the file name.
Applied: role:test-auditor Name the declared type (read the declaration line), not the file name, when a finding is about a type.
- The delta rule (b) prompt cannot tell the tests stage whether to compare against the package aggregate or a per-file number; since the manifest has only aggregates, the delta brief should state that findings propose per-file values and cite the measured per-file coverage to support them.
Applied: not applied: duplicate of lesson 2 (same text from the remediation stage); applied there.
- The docs finding on `README.md:365` was found by comparing the handler return type with the interface; checking every name in a sample for existence is not enough, so a docs stage should compare the signatures of any implemented interface too.
Applied: not applied: duplicate of lesson 1; applied there.
- A negative "only X and Y name Z" sentence in a docs stage must be backed by the stage's own search table; after the fix, the sentence and the table row now agree and name the history page explicitly.
Applied: role:docs-reviewer A negative claim ("only X and Y mention Z") must match the stage's own search table row by row; release-history pages count as mentions and are classified, not ignored.
- The delta rule (b) brief still lacks guidance on per-file versus aggregate targets (as tests.md and remediation.md noted); the stage handled it by proposing per-file values with measured per-file coverage, which reproduced exactly.
Applied: not applied: duplicate of lesson 2; the durable fix is #1762 (per-file target field).
