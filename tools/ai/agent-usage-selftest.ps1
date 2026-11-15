# Self-test of agent-usage-reconstruct.ps1 (#2734). Fixture transcripts and a ledger live in a temp folder; the real
# ledger and transcripts are never touched.
#   pwsh -NoProfile -File tools/ai/agent-usage-selftest.ps1
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'agent-usage-reconstruct.ps1'
$work = Join-Path ([IO.Path]::GetTempPath()) ('agent-usage-selftest-' + [guid]::NewGuid().ToString('N'))
$root = Join-Path $work 'projects'
$ledger = Join-Path $work 'ledger.csv'
New-Item -ItemType Directory -Force -Path $root | Out-Null

function ConvertTo-Line($o) { $o | ConvertTo-Json -Compress -Depth 10 }
function New-Fixture([string]$Session, [string]$Id, [string]$Cwd, [string]$Brief, [object[]]$Turns, [string]$LastStamp, [string]$AgentType) {
    $dir = Join-Path $root "$Session\subagents"
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $lines = @(ConvertTo-Line @{ type = 'user'; timestamp = $LastStamp; cwd = $Cwd; message = @{ role = 'user'; content = $Brief } })
    foreach ($t in $Turns) {
        $lines += ConvertTo-Line @{ type = 'assistant'; timestamp = $LastStamp; cwd = $Cwd; message = @{ role = 'assistant'; model = $t.Model; usage = @{ input_tokens = $t.In; cache_creation_input_tokens = $t.Cc; cache_read_input_tokens = $t.Cr; output_tokens = $t.Out } } }
    }
    Set-Content -LiteralPath (Join-Path $dir "agent-$Id.jsonl") -Value $lines -Encoding utf8
    if ($AgentType) { Set-Content -LiteralPath (Join-Path $dir "agent-$Id.meta.json") -Value (ConvertTo-Line @{ agentType = $AgentType; description = 'x' }) -Encoding utf8 }
}
function Turn($m, $i, $c, $r, $o) { @{ Model = $m; In = $i; Cc = $c; Cr = $r; Out = $o } }

$script:failed = 0
function Test-Case([string]$Name, [scriptblock]$Check) {
    try { if (& $Check) { "PASS $Name" } else { $script:failed++; "FAIL $Name" } }
    catch { $script:failed++; "FAIL $Name ($($_.Exception.Message))" }
}
function Invoke-Tool([string[]]$ToolArgs) { @(& pwsh -NoProfile -File $script @ToolArgs -Ledger $ledger -TranscriptRoot $root 2>&1 | ForEach-Object { "$_" }) }

try {
    $win = '2026-10-15T10:00:00Z'
    New-Fixture 'abcdef12-0000' 'normal1' 'D:\x' 'Issue #123 do a thing' @((Turn 'claude-sonnet-5' 1 2 3 4), (Turn 'claude-sonnet-5' 10 20 30 40)) $win 'issue-worker'
    New-Fixture 'abcdef12-0000' 'synth1' 'D:\x' 'Issue #124 next' @((Turn 'claude-haiku-4-5' 5 5 5 5), (Turn '<synthetic>' 0 0 0 0)) '2026-10-15T11:00:00Z' 'mechanical-fixer'
    New-Fixture 'abcdef12-0000' 'nometa1' 'D:\x\.claude\worktrees\w55' 'No issue number here' @((Turn 'claude-opus-4' 1 1 1 1)) '2026-10-16T09:00:00Z' $null
    New-Fixture 'abcdef12-0000' 'nowt1' 'D:\x' 'No issue number here either' @((Turn 'claude-opus-4' 2 2 2 2)) '2026-10-16T09:30:00Z' 'Explore'
    New-Fixture 'abcdef12-0000' 'dup1' 'D:\x' 'Issue #9 dup' @((Turn 'claude-opus-4' 3 3 3 3)) '2026-10-16T10:00:00Z' 'issue-worker'
    New-Fixture 'abcdef12-0000' 'late1' 'D:\x' 'Issue #8 late' @((Turn 'claude-opus-4' 3 3 3 3)) '2026-11-01T10:00:00Z' 'issue-worker'
    New-Fixture 'abcdef12-0000' 'onlysynth1' 'D:\x' 'Issue #7 none' @((Turn '<synthetic>' 0 0 0 0)) '2026-10-16T12:00:00Z' 'issue-worker'
    Set-Content -LiteralPath $ledger -Value @('timestampUtc,agent,task,model,subagentTokens,notes', '2026-10-16T10:00:00Z,issue-worker,issue-9,claude-opus-4,12,"hook-recorded agent-dup1"') -Encoding utf8

    $dry = Invoke-Tool @('-From', '2026-10-15', '-To', '2026-10-16')
    $before = (Get-Content $ledger).Count
    Test-Case 'dry run reports 4 rows, 1 skipped, 1 already, 1 outside, and writes nothing' { ($dry -contains 'rows to add: 4') -and ($dry -contains 'skipped: 1') -and ($dry -contains 'already in ledger: 1') -and ($dry -contains 'outside window: 1') -and (Get-Content $ledger).Count -eq $before }
    Test-Case 'dry run prints per-day counts' { ($dry -contains '  2026-10-15: 2') -and ($dry -contains '  2026-10-16: 2') }

    $null = Invoke-Tool @('-From', '2026-10-15', '-To', '2026-10-16', '-Append')
    $rows = @(Get-Content $ledger)
    Test-Case 'append adds 4 rows after the 2 existing lines' { $rows.Count -eq 6 }
    Test-Case 'existing rows keep their place' { $rows[1] -like '2026-10-16T10:00:00Z,issue-worker,issue-9,*' }
    Test-Case 'normal transcript: last message tokens, model, agent, task from the brief, stop time' { $rows -contains '2026-10-15T10:00:00Z,issue-worker,issue-123,claude-sonnet-5,100,"reconstructed agent-normal1"' }
    Test-Case 'synthetic last line is ignored for tokens and model, kept for the stop time' { $rows -contains '2026-10-15T11:00:00Z,mechanical-fixer,issue-124,claude-haiku-4-5,20,"reconstructed agent-synth1"' }
    Test-Case 'missing meta gives unknown and the worktree folder as task' { $rows -contains '2026-10-16T09:00:00Z,unknown,w55,claude-opus-4,4,"reconstructed agent-nometa1"' }
    Test-Case 'no brief issue and no worktree gives session-<8>' { $rows -contains '2026-10-16T09:30:00Z,Explore,session-abcdef12,claude-opus-4,8,"reconstructed agent-nowt1"' }
    Test-Case 'duplicate agent id, outside-window and synthetic-only transcripts are not added' { -not ($rows -like '*agent-late1*') -and -not ($rows -like '*agent-onlysynth1*') -and @($rows -like '*agent-dup1*').Count -eq 1 }
    Test-Case 'new rows are sorted by timestamp' { $new = @($rows[2..5] | ForEach-Object { $_.Substring(0, 20) }); ($new -join ',') -eq (($new | Sort-Object) -join ',') }

    $again = Invoke-Tool @('-From', '2026-10-15', '-To', '2026-10-16', '-Append')
    Test-Case 'second run is idempotent' { ($again -contains 'rows to add: 0') -and (Get-Content $ledger).Count -eq 6 }

    # Missing-day check: yesterday has activity and a ledger row, the day before has activity and none.
    $yesterday = (Get-Date).ToUniversalTime().Date.AddDays(-1)
    $twoAgo = $yesterday.AddDays(-1)
    New-Fixture 'cccccccc-0000' 'covered1' 'D:\x' 'Issue #1 a' @((Turn 'claude-opus-4' 1 1 1 1)) ($yesterday.ToString('yyyy-MM-dd') + 'T10:00:00Z') 'issue-worker'
    New-Fixture 'cccccccc-0000' 'gap1' 'D:\x' 'Issue #2 b' @((Turn 'claude-opus-4' 1 1 1 1)) ($twoAgo.ToString('yyyy-MM-dd') + 'T10:00:00Z') 'issue-worker'
    Add-Content -LiteralPath $ledger -Value ($yesterday.ToString('yyyy-MM-dd') + 'T10:00:01Z,issue-worker,issue-1,claude-opus-4,4,"hook-recorded agent-covered1"')
    $check = Invoke-Tool @('-CheckDays', '5')
    $warns = @($check | Where-Object { $_ -like 'WARN *' })
    Test-Case 'check warns for the active day without a row' { @($warns | Where-Object { $_ -like ('WARN ' + $twoAgo.ToString('yyyy-MM-dd') + ' *') }).Count -eq 1 }
    Test-Case 'check does not warn for the covered day' { @($warns | Where-Object { $_ -like ('WARN ' + $yesterday.ToString('yyyy-MM-dd') + ' *') }).Count -eq 0 }
    Test-Case 'check ignores the old fixtures outside its window' { $warns.Count -eq 1 }
}
finally { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }

if ($script:failed -gt 0) { "FAILED: $($script:failed)"; exit 1 }
'ALL PASSED'
