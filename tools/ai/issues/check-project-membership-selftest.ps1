# tools/ai/issues/check-project-membership-selftest.ps1 (#1987)
#
# Runs the REAL check-project-membership.ps1 in a child session with gh replaced by a stub (nothing touches GitHub).
# Asserts: none missing -> exit 0 and no add; some missing -> exit 1, lists number and title, no add; -Add -> adds
# exactly the missing ones with the keyring (token variables cleared) and exits 0; a failed add exits 1.
# Exit code: 0 when every assertion passes, 1 otherwise.

$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'check-project-membership.ps1'
$tmp = Join-Path ([IO.Path]::GetTempPath()) ("cpm-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Force $tmp | Out-Null
$log = Join-Path $tmp 'gh.log'
$failures = [System.Collections.Generic.List[string]]::new()
$savedG = $env:GITHUB_TOKEN; $savedH = $env:GH_TOKEN
function Assert-That([string]$Name, [bool]$Condition, [string]$Detail = '') {
    if ($Condition) { Write-Host "PASS  $Name" } else { Write-Host "FAIL  $Name $Detail"; $failures.Add($Name) }
}

Set-Content -LiteralPath (Join-Path $tmp 'gh-stub.ps1') -Encoding utf8 @'
Add-Content -LiteralPath $env:CPM_LOG -Value ('gh ' + ($args -join ' '))
if ($env:CPM_LIST_FAIL -and $args[0] -eq 'issue') { $global:LASTEXITCODE = 1; exit 1 }
if ($env:CPM_BAD_JSON -and $args[0] -eq 'issue') { 'not json <html>'; exit 0 }
if ($args[0] -eq 'issue') {
    $all = @(1, 2, 3) | ForEach-Object { @{ number = $_; title = "Issue $_"; url = "https://github.com/dlrivada/Encina/issues/$_" } }
    $all | ConvertTo-Json -AsArray; exit 0
}
if ($args[0] -eq 'project' -and $args[1] -eq 'item-list') {
    $nums = @($env:CPM_ON -split ',' | Where-Object { $_ })
    $items = @($nums | ForEach-Object { @{ content = @{ number = [int]$_; repository = 'dlrivada/Encina' } } })
    $items += @{ content = @{ number = 3; repository = 'other/Repo' } }
    @{ items = $items } | ConvertTo-Json -Depth 5; exit 0
}
if ($args[0] -eq 'project' -and $args[1] -eq 'item-add') {
    Add-Content -LiteralPath $env:CPM_LOG -Value ('TOKENS:[' + $env:GITHUB_TOKEN + $env:GH_TOKEN + ']')
    if ($env:CPM_ADD_FAIL) { 'denied'; $global:LASTEXITCODE = 1; exit 1 }
    exit 0
}
exit 0
'@

function Invoke-Check([string]$On, [string]$ExtraArgs = '', [string]$AddFail = '') {
    if (Test-Path $log) { Remove-Item $log -Force }
    $env:CPM_LOG = $log; $env:CPM_ON = $On; $env:CPM_ADD_FAIL = $AddFail
    $env:GITHUB_TOKEN = 'must-be-cleared'; $env:GH_TOKEN = 'must-be-cleared'
    $cmd = "function gh { & '$tmp\gh-stub.ps1' @args }; & '$script' $ExtraArgs; exit `$LASTEXITCODE"
    $out = & pwsh -NoProfile -Command $cmd 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    $adds = if (Test-Path $log) { @(Get-Content $log | Where-Object { $_ -like 'gh project item-add*' }) } else { @() }
    $tok = if (Test-Path $log) { @(Get-Content $log | Where-Object { $_ -like 'TOKENS:*' }) } else { @() }
    return @{ Exit = $code; Text = ($out -join "`n"); Adds = $adds; Tokens = $tok }
}

try {
    $r = Invoke-Check '1,2,3'
    Assert-That 'none missing: exit 0, nothing added (an issue of another repository is ignored)' ($r.Exit -eq 0 -and $r.Adds.Count -eq 0 -and $r.Text.Contains('All 3 open issues')) $r.Text

    $r = Invoke-Check '1'
    Assert-That 'some missing: exit 1, lists number and title, nothing added' ($r.Exit -eq 1 -and $r.Text.Contains('#2 Issue 2') -and $r.Text.Contains('#3 Issue 3') -and -not $r.Text.Contains('#1 Issue 1') -and $r.Adds.Count -eq 0) $r.Text

    $r = Invoke-Check '1' '-Add'
    Assert-That '-Add: adds exactly the missing issues, reports each, exit 0' ($r.Exit -eq 0 -and $r.Adds.Count -eq 2 -and $r.Adds[0] -eq 'gh project item-add 1 --owner dlrivada --url https://github.com/dlrivada/Encina/issues/2' -and $r.Adds[1].EndsWith('/issues/3') -and $r.Text.Contains('ADDED   #2') -and $r.Text.Contains('ADDED   #3')) ($r.Text + ' | ' + ($r.Adds -join ' | '))
    Assert-That 'the add runs with GITHUB_TOKEN and GH_TOKEN cleared' ($r.Tokens.Count -eq 2 -and @($r.Tokens | Where-Object { $_ -ne 'TOKENS:[]' }).Count -eq 0) ($r.Tokens -join ' | ')

    $env:CPM_LIST_FAIL = '1'
    $r = Invoke-Check '1'
    $env:CPM_LIST_FAIL = ''
    Assert-That 'a gh failure exits 2, never 1 (nothing was checked)' ($r.Exit -eq 2 -and $r.Text.Contains('failed')) $r.Text

    $env:CPM_BAD_JSON = '1'
    $r = Invoke-Check '1'
    $env:CPM_BAD_JSON = ''
    Assert-That 'gh output that is not JSON exits 2, never 1' ($r.Exit -eq 2) $r.Text

    $r = Invoke-Check '1' '-Add' '1'
    Assert-That '-Add with a failing add: reports FAILED and exits 1' ($r.Exit -eq 1 -and $r.Text.Contains('FAILED  #2')) $r.Text
}
finally {
    $env:GITHUB_TOKEN = $savedG; $env:GH_TOKEN = $savedH
    $env:CPM_LOG = $null; $env:CPM_ON = $null; $env:CPM_ADD_FAIL = $null; $env:CPM_LIST_FAIL = $null; $env:CPM_BAD_JSON = $null
    Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) { Write-Host "`n$($failures.Count) assertion(s) failed."; exit 1 }
Write-Host "`nAll check-project-membership assertions passed."
exit 0
