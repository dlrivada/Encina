You are triaging 20 closed GitHub issues from the Encina .NET repository for a knowledge-migration pilot.

For EACH issue in the attached JSON file (issues.json, an array of objects with number/title/body/labels/closedAt),
output one line in this exact pipe-delimited format, one issue per line, in the same order as the input:

number|type|area|packages|outcome|one_sentence_summary

Where:
- type is one of: feature, bug, debt, test, infra, refactor, spike
- area is a short slug like: core, messaging, database, testing, validation, caching, event-sourcing,
  observability, ci-cd, compliance, security, mongodb, agent-system, mutation-testing
- packages is a semicolon-separated list of the Encina.* package names the issue is about (best guess from title/body/labels)
- outcome is one of: delivered, partial, superseded, rejected, duplicate (best guess from the title/body/labels; if unclear, say "delivered")
- one_sentence_summary is a single English sentence (max 25 words) summarizing what the issue was about

Do not add headers, explanations, or extra commentary. Output exactly 20 lines.
