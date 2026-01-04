<#
.SYNOPSIS
    Audits test project dependencies for Encina.Testing.* migration readiness.

.DESCRIPTION
    This script scans test projects to identify:
    - Direct testing dependencies that should be replaced
    - Missing Encina.Testing.* package references
    - Migration priority based on dependency usage

.PARAMETER Path
    Root path to scan for test projects. Defaults to current directory.

.PARAMETER OutputFormat
    Output format: 'Console', 'Json', or 'Markdown'. Defaults to Console.

.EXAMPLE
    .\audit-test-dependencies.ps1 -Path "D:\Proyectos\Encina" -OutputFormat Markdown
#>

param(
    [string]$Path = ".",
    [ValidateSet("Console", "Json", "Markdown")]
    [string]$OutputFormat = "Console"
)

$ErrorActionPreference = "Stop"

# Dependencies that should be replaced with Encina.Testing.* packages
$ReplacementMap = @{
    "Bogus" = @{
        Replacement = "Encina.Testing.Bogus"
        Priority = "P1"
        Reason = "Use EncinaFaker for reproducible, domain-aware test data"
    }
    "FluentAssertions" = @{
        Replacement = "Encina.Testing.Shouldly"
        Priority = "P2"
        Reason = "Shouldly is already used; FluentAssertions is redundant"
    }
    "WireMock.Net" = @{
        Replacement = "Encina.Testing.WireMock"
        Priority = "P2"
        Reason = "Use EncinaWireMockFixture for standardized HTTP mocking"
    }
    "PactNet" = @{
        Replacement = "Encina.Testing.Pact"
        Priority = "P3"
        Reason = "Use EncinaPactConsumerBuilder for CQRS-aware contracts"
    }
    "FsCheck" = @{
        Replacement = "Encina.Testing.FsCheck"
        Priority = "P2"
        Reason = "Use EncinaProperty and pre-built arbitraries"
    }
    "ArchUnitNET" = @{
        Replacement = "Encina.Testing.Architecture"
        Priority = "P2"
        Reason = "Use EncinaArchitectureRulesBuilder with pre-built rules"
    }
    "Verify" = @{
        Replacement = "Encina.Testing.Verify"
        Priority = "P3"
        Reason = "Use EncinaVerify for Encina-aware snapshots"
    }
    "Respawn" = @{
        Replacement = "Encina.Testing.Respawn"
        Priority = "P3"
        Reason = "Use EncinaRespawner for database cleanup"
    }
}

# Encina.Testing.* packages to check
$EncinaTestingPackages = @(
    "Encina.Testing"
    "Encina.Testing.Shouldly"
    "Encina.Testing.Bogus"
    "Encina.Testing.Fakes"
    "Encina.Testing.Handlers"
    "Encina.Testing.WireMock"
    "Encina.Testing.Architecture"
    "Encina.Testing.FsCheck"
    "Encina.Testing.Pact"
    "Encina.Testing.Verify"
    "Encina.Testing.Respawn"
)

function Get-TestProjects {
    param([string]$RootPath)

    Get-ChildItem -Path $RootPath -Recurse -Filter "*.csproj" |
        Where-Object {
            $_.FullName -match "tests[\\/]" -or
            $_.Name -match "\.Tests\.csproj$" -or
            $_.Name -match "\.IntegrationTests\.csproj$" -or
            $_.Name -match "\.PropertyTests\.csproj$"
        }
}

function Get-PackageReferences {
    param([string]$ProjectPath)

    [xml]$csproj = Get-Content $ProjectPath
    $packages = @()

    foreach ($itemGroup in $csproj.Project.ItemGroup) {
        foreach ($packageRef in $itemGroup.PackageReference) {
            if ($packageRef.Include) {
                $packages += @{
                    Name = $packageRef.Include
                    Version = $packageRef.Version
                }
            }
        }
    }

    return $packages
}

function Get-ProjectReferences {
    param([string]$ProjectPath)

    [xml]$csproj = Get-Content $ProjectPath
    $projects = @()

    foreach ($itemGroup in $csproj.Project.ItemGroup) {
        foreach ($projectRef in $itemGroup.ProjectReference) {
            if ($projectRef.Include) {
                $projects += $projectRef.Include
            }
        }
    }

    return $projects
}

function Analyze-Project {
    param([System.IO.FileInfo]$Project)

    $packages = Get-PackageReferences -ProjectPath $Project.FullName
    $projectRefs = Get-ProjectReferences -ProjectPath $Project.FullName

    $analysis = @{
        Name = $Project.Name.Replace(".csproj", "")
        Path = $Project.FullName
        ReplacementNeeded = @()
        EncinaPackages = @()
        MissingEncinaPackages = @()
        Priority = "P3"
    }

    # Check for packages that need replacement
    foreach ($package in $packages) {
        if ($ReplacementMap.ContainsKey($package.Name)) {
            $replacement = $ReplacementMap[$package.Name]
            $analysis.ReplacementNeeded += @{
                Current = $package.Name
                Version = $package.Version
                Replacement = $replacement.Replacement
                Priority = $replacement.Priority
                Reason = $replacement.Reason
            }

            # Upgrade priority if replacement is high priority
            if ($replacement.Priority -eq "P1" -and $analysis.Priority -ne "P0") {
                $analysis.Priority = "P1"
            }
            elseif ($replacement.Priority -eq "P2" -and $analysis.Priority -eq "P3") {
                $analysis.Priority = "P2"
            }
        }
    }

    # Check for Encina.Testing.* packages (via project references)
    foreach ($projectRef in $projectRefs) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectRef)
        if ($projectName -like "Encina.Testing*") {
            $analysis.EncinaPackages += $projectName
        }
    }

    # Check for Encina.Testing.* packages (via package references)
    foreach ($package in $packages) {
        if ($package.Name -like "Encina.Testing*") {
            $analysis.EncinaPackages += $package.Name
        }
    }

    # Determine missing Encina.Testing.* packages based on needs
    $hasShouldlyUsage = $packages | Where-Object { $_.Name -eq "Shouldly" }
    if ($hasShouldlyUsage -and "Encina.Testing.Shouldly" -notin $analysis.EncinaPackages) {
        $analysis.MissingEncinaPackages += "Encina.Testing.Shouldly"
    }

    return $analysis
}

function Format-ConsoleOutput {
    param([array]$Results)

    Write-Host "`n=== Test Dependency Audit Report ===" -ForegroundColor Cyan
    Write-Host "Scanned: $($Results.Count) test projects`n"

    $projectsNeedingWork = $Results | Where-Object { $_.ReplacementNeeded.Count -gt 0 -or $_.MissingEncinaPackages.Count -gt 0 }

    if ($projectsNeedingWork.Count -eq 0) {
        Write-Host "All projects are fully migrated!" -ForegroundColor Green
        return
    }

    # Group by priority
    $byPriority = $projectsNeedingWork | Group-Object Priority | Sort-Object Name

    foreach ($group in $byPriority) {
        Write-Host "`n[$($group.Name)] - $($group.Count) projects" -ForegroundColor Yellow
        Write-Host ("-" * 50)

        foreach ($project in $group.Group) {
            Write-Host "`n  $($project.Name)" -ForegroundColor White

            if ($project.ReplacementNeeded.Count -gt 0) {
                Write-Host "    Replacements needed:" -ForegroundColor DarkYellow
                foreach ($replacement in $project.ReplacementNeeded) {
                    Write-Host "      - $($replacement.Current) -> $($replacement.Replacement)" -ForegroundColor DarkGray
                }
            }

            if ($project.MissingEncinaPackages.Count -gt 0) {
                Write-Host "    Missing packages:" -ForegroundColor DarkYellow
                foreach ($missing in $project.MissingEncinaPackages) {
                    Write-Host "      - $missing" -ForegroundColor DarkGray
                }
            }

            if ($project.EncinaPackages.Count -gt 0) {
                Write-Host "    Already using:" -ForegroundColor DarkGreen
                foreach ($using in $project.EncinaPackages) {
                    Write-Host "      - $using" -ForegroundColor DarkGray
                }
            }
        }
    }

    Write-Host "`n=== Summary ===" -ForegroundColor Cyan
    Write-Host "Total projects: $($Results.Count)"
    Write-Host "Projects needing work: $($projectsNeedingWork.Count)"
    Write-Host "Already migrated: $($Results.Count - $projectsNeedingWork.Count)"
}

function Format-MarkdownOutput {
    param([array]$Results)

    $sb = [System.Text.StringBuilder]::new()

    [void]$sb.AppendLine("# Test Dependency Audit Report")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("## Summary")
    [void]$sb.AppendLine("")

    $projectsNeedingWork = $Results | Where-Object { $_.ReplacementNeeded.Count -gt 0 -or $_.MissingEncinaPackages.Count -gt 0 }

    [void]$sb.AppendLine("| Metric | Count |")
    [void]$sb.AppendLine("|--------|-------|")
    [void]$sb.AppendLine("| Total projects | $($Results.Count) |")
    [void]$sb.AppendLine("| Projects needing work | $($projectsNeedingWork.Count) |")
    [void]$sb.AppendLine("| Already migrated | $($Results.Count - $projectsNeedingWork.Count) |")
    [void]$sb.AppendLine("")

    if ($projectsNeedingWork.Count -gt 0) {
        [void]$sb.AppendLine("## Projects Requiring Migration")
        [void]$sb.AppendLine("")

        $byPriority = $projectsNeedingWork | Group-Object Priority | Sort-Object Name

        foreach ($group in $byPriority) {
            [void]$sb.AppendLine("### $($group.Name) Priority ($($group.Count) projects)")
            [void]$sb.AppendLine("")

            foreach ($project in $group.Group) {
                [void]$sb.AppendLine("#### $($project.Name)")
                [void]$sb.AppendLine("")

                if ($project.ReplacementNeeded.Count -gt 0) {
                    [void]$sb.AppendLine("**Replacements:**")
                    [void]$sb.AppendLine("")
                    [void]$sb.AppendLine("| Current | Replace With | Reason |")
                    [void]$sb.AppendLine("|---------|--------------|--------|")
                    foreach ($r in $project.ReplacementNeeded) {
                        [void]$sb.AppendLine("| $($r.Current) | $($r.Replacement) | $($r.Reason) |")
                    }
                    [void]$sb.AppendLine("")
                }

                if ($project.MissingEncinaPackages.Count -gt 0) {
                    [void]$sb.AppendLine("**Missing packages:** $($project.MissingEncinaPackages -join ', ')")
                    [void]$sb.AppendLine("")
                }
            }
        }
    }

    return $sb.ToString()
}

function Format-JsonOutput {
    param([array]$Results)

    return $Results | ConvertTo-Json -Depth 10
}

# Main execution
Write-Host "Scanning for test projects in: $Path" -ForegroundColor Gray

$testProjects = Get-TestProjects -RootPath $Path

if ($testProjects.Count -eq 0) {
    Write-Host "No test projects found!" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($testProjects.Count) test projects" -ForegroundColor Gray

$results = @()
foreach ($project in $testProjects) {
    $results += Analyze-Project -Project $project
}

switch ($OutputFormat) {
    "Console" { Format-ConsoleOutput -Results $results }
    "Markdown" { Format-MarkdownOutput -Results $results }
    "Json" { Format-JsonOutput -Results $results }
}
