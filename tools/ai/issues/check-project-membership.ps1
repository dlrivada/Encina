# tools/ai/issues/check-project-membership.ps1 [-Add] (#1987)
#
# Lists every OPEN issue of dlrivada/Encina that is not on user project 1 (number and title). Read-only by default:
# exit 0 when none is missing, 1 when some are, 2 when gh itself fails (nothing was checked). With -Add each missing issue is added with
# `gh project item-add 1 --owner dlrivada --url <url>` and reported; then the exit code is 0 only when every add worked.
#
# No workflow can do this: the default GITHUB_TOKEN cannot read or write user projects and no project token is kept
# (maintainer decision 2026-10-07). It runs locally with the keyring credentials, so GITHUB_TOKEN and GH_TOKEN are
# cleared for its gh calls. This is how web-created issues that never went through open-issue are found.

param([switch]$Add)

$ErrorActionPreference = 'Stop'
$repoName = 'dlrivada/Encina'

$savedG = $env:GITHUB_TOKEN; $savedH = $env:GH_TOKEN
try {
    $env:GITHUB_TOKEN = $null; $env:GH_TOKEN = $null
    Remove-Item Env:\GITHUB_TOKEN, Env:\GH_TOKEN -ErrorAction SilentlyContinue

    try {
    $openJson = & gh issue list --repo $repoName --state open --limit 5000 --json number,title,url
    if ($LASTEXITCODE -ne 0) { Write-Host "check-project-membership: gh issue list failed (exit $LASTEXITCODE)"; exit 2 }
    $open = @("$($openJson -join "`n")" | ConvertFrom-Json)

    $itemsJson = & gh project item-list 1 --owner dlrivada --limit 5000 --format json
    if ($LASTEXITCODE -ne 0) { Write-Host "check-project-membership: gh project item-list failed (exit $LASTEXITCODE); the keyring token needs the project scope"; exit 2 }
    $items = @(("$($itemsJson -join "`n")" | ConvertFrom-Json).items)
    $onProject = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($i in $items) {
        if ($i.content -and $i.content.number -and $i.content.repository -eq $repoName) { [void]$onProject.Add([int]$i.content.number) }
    }

    $missing = @($open | Where-Object { -not $onProject.Contains([int]$_.number) })
    if ($missing.Count -eq 0) { Write-Host "All $($open.Count) open issues are on project 1."; exit 0 }

    $failed = 0
    foreach ($m in $missing) {
        if ($Add) {
            $out = & gh project item-add 1 --owner dlrivada --url $m.url 2>&1
            if ($LASTEXITCODE -ne 0) { $failed++; Write-Host "FAILED  #$($m.number) $($m.title): $($out -join ' ')" }
            else { Write-Host "ADDED   #$($m.number) $($m.title)" }
        }
        else { Write-Host "#$($m.number) $($m.title)" }
    }
    if ($Add) { exit $(if ($failed -eq 0) { 0 } else { 1 }) }
    Write-Host "$($missing.Count) open issue(s) are not on project 1; run again with -Add to add them."
    exit 1
    }
    catch {
        # gh missing, or its output is not JSON: nothing was checked, which must not look like "some are missing".
        Write-Host "check-project-membership: $($_.Exception.GetType().Name): $($_.Exception.Message)"
        exit 2
    }
}
finally {
    if ($null -ne $savedG) { $env:GITHUB_TOKEN = $savedG }
    if ($null -ne $savedH) { $env:GH_TOKEN = $savedH }
}
