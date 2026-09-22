You are the Historian of the Encina repository doing archaeology on CLOSED issues. For every issue listed at the end of this message, extract the durable engineering knowledge it contains, with provenance. Output ONLY a JSON array, nothing else: no prose, no Markdown fences, no comments.

1. Output format (strict): one object per issue, in input order, exactly:
   {"number": <int>, "summary": "<what was asked and how it ended, max 30 words>", "outcome": "delivered"|"rejected"|"superseded"|"duplicate"|"moved"|"unknown", "knowledge": [ {"type": "decision"|"rejected-alternative"|"rule"|"direction-change"|"gotcha", "statement": "<one sentence>", "evidence": "<source id(s) and short quote, max 30 words>"} ], "still_relevant": "yes"|"no"|"unknown", "related": [<issue or PR numbers mentioned that matter to the statement>]}
   Every input issue appears exactly once. "knowledge" may be an empty array when the issue holds nothing durable (a routine delivery with no decision is normal).

2. What counts as durable knowledge:
   - decision: something was chosen among alternatives, with a reason (a design, a package, a naming, a policy, a scope cut).
   - rejected-alternative: an option considered and turned down, with the reason.
   - rule: a constraint the project committed to (a coding rule, a test requirement, a provider rule, a process rule).
   - direction-change: the issue shows the project changing course (a feature dropped, a package replaced, a milestone re-scoped).
   - gotcha: a non-obvious technical fact learned the hard way (a library limitation, a CI quirk, a failure mode) that a future engineer would want to know.
   Do NOT record implementation details that the code already documents, routine status updates, or anything a bot wrote.

3. Evidence sources and ids per issue: the body (`body`), comments (`c1`, `c2`, ... with author and date), and references (`r1`, `r2`, ...: pull requests or issues that mention this one, with state). Every knowledge item must cite at least one id and must not claim anything the evidence does not say. Prefer maintainer comments and merged pull requests.

4. "outcome": delivered when a merged PR or a completion comment closes it; rejected when it was closed as not planned with a reason; superseded when another issue or design replaced it; duplicate when closed as a duplicate; moved when it was closed to be tracked elsewhere; unknown otherwise (a closed issue with no comments and no references is "unknown").

5. "still_relevant": yes when the knowledge still constrains today's work (rules, decisions not reversed), no when the evidence shows it was later reversed or the feature was removed, unknown otherwise.

6. Stop when the JSON array is complete.

Closed issues and their evidence:
