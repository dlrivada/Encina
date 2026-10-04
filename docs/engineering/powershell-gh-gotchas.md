# PowerShell and gh CLI gotchas

This is a how-to guide (Diátaxis) for a contributor or agent who automates GitHub work from PowerShell (`pwsh`) under the scripting policy of [`AGENTS.md`](../../AGENTS.md) §2, and wants the form that works for each pitfall the project has already hit. The pitfalls were observed from 2026-09-21 on; each entry gives the problem, the symptom and the working form. The reasoning behind the scripting policy is in the [engineering handbook](ENGINEERING-HANDBOOK.md) ("Scripting & Tooling Policy"); the agents' hooks are described in [`.claude/agents/README.md`](../../.claude/agents/README.md).

## Argument expansion in bare `gh` arguments

**Problem.** In a bare (unquoted) command argument, PowerShell expands only the variable, not a property or index access after it.

**Symptom.** `gh api ... -f title=$obj.new` sends the literal text `@{...}.new`; `-F milestone=$map[$key]` sends `System.Collections.Hashtable[...]`; `-f t=$p[0]` expands `$p` and appends a literal `[0]` (2026-09-25).

**Working form.** Wrap the access in a subexpression inside a double-quoted string, or compute it into a plain variable first and pass the variable:

```powershell
gh api repos/dlrivada/Encina/milestones/$number -f "title=$($obj.new)"
gh api repos/dlrivada/Encina/issues/$n -F "milestone=$($map[$key])"

$ta = "t=$id"
gh api ... -f $ta
```

Two incidents:

- 2026-09-22: the bare form renamed 25 milestones to object dumps. A repair script (`artifacts/tools/renumber-milestones-repair.ps1`) fixed them; the issue moves failed with HTTP 422 instead of moving wrongly.
- 2026-09-26: `-f body=$answers[$k]` inside a `foreach` over an `[ordered]` hashtable posted `System.Collections.Specialized.OrderedDictionary[PRRT_...]` as the reply to 7 CodeRabbit threads, and the bot answered confused.

The rule has no exception: before any `gh` argument, copy the value into a plain variable and pass the variable, then check the first result before looping:

```powershell
$b = [string]$answers[$k]
gh api ... -f body=$b
```

## `$( )` with parentheses or regex inside a double-quoted string

**Problem.** A subexpression that itself contains `(`, `)` or a regex, written inside a double-quoted string, breaks the parser. `\(` inside `--jq "..."` under double quotes breaks it too.

**Working form.** Compute into variables first. Prefer a single-quoted `--jq '...'`, and build the query by string concatenation when it needs variables.

## `[string[]]` parameters through `pwsh -File`

**Problem.** `-Areas a,b,c` passed to a `[string[]]` parameter of a script run as `pwsh -File script.ps1` arrives as one string, `"a,b,c"`.

**Working form.** Split on the comma inside the script.

## `gh` flags that behave unexpectedly

| Command | Fact |
| --- | --- |
| `gh pr comment` | has no `--jq` |
| `gh issue close --reason` | accepts `completed` or `"not planned"` (with a space) |
| `gh label create` | the description is limited to 100 characters |
| `gh run rerun <id> --failed` | re-runs the failed jobs only |

## Review threads and auto-merge

`gh` has no command to resolve a review thread. Use the GraphQL mutation `resolveReviewThread` with the thread id (it starts with `PRRT_`). `required_conversation_resolution` on `main` means an unresolved bot thread blocks auto-merge, so resolve or answer every thread before arming it.

## Monitor scripts

**Problem.** An inline `pwsh -Command "..."` run by a monitor that is hosted by a bash shell has its `$vars` expanded by that shell before PowerShell sees them.

**Working form.** Put the logic in a `.ps1` file and run it as `pwsh -NoProfile -File <script.ps1>`. The PR-watching practice (one line per event: review comment, issue comment, review, failed check, checks done, merged; act on each immediately) is described in [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md), standing rule 7.

## Background commands that end in `Select-Object -Last N`

**Symptom.** A background PowerShell command piped to `Select-Object -Last N` shows nothing until the command ends.

**Working form.** Write progress to a log file when you need to peek at a long run.

## CRLF files

**Problem.** Replacing text with `-replace` and an LF anchor is unreliable in a file with CRLF line endings.

**Working form.** Read the file with `[IO.File]::ReadAllText` and locate the anchor with `.IndexOf(anchor)`, using the anchor's exact escaping (a TOML `\\.` is two backslashes in the file). This is for scratch analysis: agents edit repository source files only with the Edit and Write tools (see [`AI-DEVELOPMENT-MODEL.md`](AI-DEVELOPMENT-MODEL.md) §19, item 4).

## Markdown with backticks in a double-quoted string

**Problem.** Inline code in a PowerShell double-quoted string is corrupted: a backtick before `f`, `t`, `n`, `r`, `0`, `a`, `b` or `v` is an escape sequence.

**Symptom.** `` `from` `` became a form feed followed by `rom`.

**Working form.** Build markdown bodies with the Write tool or with single-quoted strings, never double-quoted ones.

## Permission-classifier refusals

In auto mode the permission classifier has refused: dismissing a PR review, a `PUT` on branch protection, the `git push` of some commands, and once even a read-only `gh pr view` ("Self-Approval"). Commands whose text contains `'\'` or `"/c` were blocked by a hook.

**Working form.** Use `gh api repos/.../pulls/N` for reads, and hand the maintainer a one-line command for the rest. A hook that blocks a command is never worked around; see [`.claude/agents/README.md`](../../.claude/agents/README.md).

## Hooks and the working directory

A hook fix made in a worktree is not active until it is merged, because hooks are loaded from the main checkout; and `Test-Hooks.ps1` must run with the worktree as its working directory. The full explanation is in [`.claude/agents/README.md`](../../.claude/agents/README.md), section "Where hooks load from".
