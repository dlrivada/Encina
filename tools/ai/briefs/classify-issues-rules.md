Classify every GitHub issue listed at the end of this message into exactly one backlog priority for the Encina 1.0 release. Follow every numbered point without omitting any. Output ONLY a JSON array, nothing else: no prose, no Markdown fences, no comments.

1. Output format (strict): a JSON array with one object per issue, in the same order as the input, each object exactly `{"number": <int>, "priority": "P0"|"P1"|"P2"|"P3", "reason": "<one sentence, max 25 words>", "confidence": "high"|"medium"|"low"}`. Every input issue must appear exactly once. If unsure, choose the most plausible priority and set confidence to "low".

2. Priority definitions (from ENCINA-1.0-RECONCILIATION.md §4):
   - P0, mandatory for 1.0: defects affecting contractual behaviour; relevant security problems; CI or release failures; inconsistent public API; documentation needed to use the public API correctly; explicit release-gate requirements; broken or incomplete NuGet packaging; licence, security or provenance requirements; problems that invalidate published claims.
   - P1, recommended for 1.0, deferrable with a reason: important ergonomic improvements; additional documentation; relevant benchmarks; maintenance automation; coverage of secondary cases.
   - P2, post-1.0, must not block the first stable release: new providers; new optional integrations; AI/LLM features not needed by the core; exhaustive instrumentation of every adapter; benchmarks for every package or provider; experimental features; scope extensions not tied to an existing requirement.
   - P3, obsolete, duplicate or superseded: already implemented but still open; duplicates; proposals absorbed by another architecture; features whose direction changed; work that no longer belongs to the product.

3. Scope rules that override the general definitions (from SPEC-000, decided by the maintainer on 2026-09-21):
   - Encina 1.0 is the product that exists today, hardened: same packages, same public API, verified, documented, released professionally. There is no target date.
   - EU regulatory compliance is part of the 1.0 contract: the AI Act work of EPIC #881 (issues #836 to #847) and the NIS2 plus Digital Omnibus work of EPIC #880 (issues #822 to #827 and #810 to #816) are P0 or P1, never P2. The five new regulation packages of EPIC #880 (DORA, eIDAS2, Data Act, ENS, EHDS: issues #804 to #808) are post-1.0, so P2.
   - Provider sets: Memcached caching (#277, the Memcached part only), PostgreSQL locks (#207) and MySQL locks (#208) are P1. Google Cloud Functions (#205) and any other new provider, transport, cloud target or database backend are P2.
   - Feature milestones v0.14.0 to v0.20.x are outside the 1.0 contract: their issues are P2 unless the issue itself is a defect, a security problem, a public-API inconsistency, a release or CI requirement, or documentation needed to use an existing public API (then P0 or P1 by the definitions above).
   - Milestones v0.21.0 (testing and CI), v0.22.0 (documentation) and v0.23.0 (release gate) are close to 1.0: classify by the definitions, expecting mostly P0 and P1.
   - Release engineering (packaging, signing, provenance, NuGet publish, evidence report, adversarial review, branch protection, version pinning) is P0.
   - Coverage flags below their manifest targets for a shipped package are P1; mutation score work is P1; benchmarks or load tests for a single package are P2 unless the package's docs make a performance claim (then P1).
   - Anything that reads as already done, superseded by a later design, or a duplicate of another issue is P3; say which in the reason when you can.

4. Use only the information given for each issue (number, title, milestone, labels, created date, comment count, body excerpt). Do not invent details. Titles carry a prefix such as [BUG], [FEATURE], [DEBT], [TEST], [SPIKE], [INFRA], [EPIC], [REFACTOR]; use it as a strong hint ([BUG] and [INFRA] lean P0/P1, [FEATURE] in a feature milestone leans P2, [SPIKE] leans P1/P2).

5. Reasons must be specific to the issue and cite the rule that applied (for example "feature milestone v0.16.0, new provider, not in 1.0 contract" or "EPIC #881 child, AI Act obligation in 1.0 contract").

6. Stop when the JSON array is complete.

Issues to classify:
