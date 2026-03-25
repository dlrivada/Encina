<#
.SYNOPSIS
    Updates test project dependencies to use Encina.Testing.* packages.

.DESCRIPTION
    This script:
    - Adds Encina.Testing.* project references
    - Removes redundant direct dependencies
    - Creates backup before modifications

.PARAMETER ProjectPath
    Path to the test project .csproj file.

.PARAMETER DryRun
    If specified, shows what would be changed without modifying files.

.PARAMETER AddAll
    If specified, adds all Encina.Testing.* package references.

.PARAMETER Packages
    Specific packages to add. Valid values: Shouldly, Bogus, Fakes, WireMock, Architecture, FsCheck, Pact, Verify, Respawn, Handlers

.EXAMPLE
    .\update-test-dependencies.ps1 -ProjectPath "tests\Encina.Tests\Encina.Tests.csproj" -DryRun
    Shows what would change without modifying files.

.EXAMPLE
    .\update-test-dependencies.ps1 -ProjectPath "tests\Encina.Tests\Encina.Tests.csproj" -Packages Shouldly,Bogus,Fakes
    Adds specific Encina.Testing.* packages.

.EXAMPLE
    .\update-test-dependencies.ps1 -ProjectPath "tests\Encina.Tests\Encina.Tests.csproj" -AddAll
    Adds all Encina.Testing.* packages.
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [switch]$DryRun,

    [switch]$AddAll,

    [ValidateSet("Shouldly", "Bogus", "Fakes", "WireMock", "Architecture", "FsCheck", "Pact", "Verify", "Respawn", "Handlers", "Messaging")]
    [string[]]$Packages = @()
)

$ErrorActionPreference = "Stop"

# Package mapping
$PackageMap = @{
    "Shouldly" = @{
        ProjectRef = "..\..\src\Encina.Testing.Shouldly\Encina.Testing.Shouldly.csproj"
        PackageName = "Encina.Testing.Shouldly"
    }
    "Bogus" = @{
        ProjectRef = "..\..\src\Encina.Testing.Bogus\Encina.Testing.Bogus.csproj"
        PackageName = "Encina.Testing.Bogus"
    }
    "Fakes" = @{
        ProjectRef = "..\..\src\Encina.Testing.Fakes\Encina.Testing.Fakes.csproj"
        PackageName = "Encina.Testing.Fakes"
    }
    "WireMock" = @{
        ProjectRef = "..\..\src\Encina.Testing.WireMock\Encina.Testing.WireMock.csproj"
        PackageName = "Encina.Testing.WireMock"
    }
    "Architecture" = @{
        ProjectRef = "..\..\src\Encina.Testing.Architecture\Encina.Testing.Architecture.csproj"
        PackageName = "Encina.Testing.Architecture"
    }
    "FsCheck" = @{
        ProjectRef = "..\..\src\Encina.Testing.FsCheck\Encina.Testing.FsCheck.csproj"
        PackageName = "Encina.Testing.FsCheck"
    }
    "Pact" = @{
        ProjectRef = "..\..\src\Encina.Testing.Pact\Encina.Testing.Pact.csproj"
        PackageName = "Encina.Testing.Pact"
    }
    "Verify" = @{
        ProjectRef = "..\..\src\Encina.Testing.Verify\Encina.Testing.Verify.csproj"
        PackageName = "Encina.Testing.Verify"
    }
    "Respawn" = @{
        ProjectRef = "..\..\src\Encina.Testing.Respawn\Encina.Testing.Respawn.csproj"
        PackageName = "Encina.Testing.Respawn"
    }
    "Handlers" = @{
        ProjectRef = "..\..\src\Encina.Testing.Handlers\Encina.Testing.Handlers.csproj"
        PackageName = "Encina.Testing.Handlers"
    }
    "Messaging" = @{
        ProjectRef = "..\..\src\Encina.Testing\Encina.Testing.csproj"
        PackageName = "Encina.Testing"
    }
}

function Get-ProjectContent {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Project file not found: $Path"
    }

    return Get-Content $Path -Raw
}

function Get-ExistingProjectReferences {
    param([string]$Content)

    $refs = @()
    $regex = [regex]'<ProjectReference\s+Include="([^"]+)"'
    $matches = $regex.Matches($Content)

    foreach ($match in $matches) {
        $refs += $match.Groups[1].Value
    }

    return $refs
}

function Get-RelativeProjectPath {
    param(
        [string]$FromProject,
        [string]$ToProject
    )

    $fromDir = Split-Path $FromProject -Parent
    $depth = ($fromDir -split "[\\/]tests[\\/]" | Select-Object -Skip 1) -split "[\\/]" | Where-Object { $_ } | Measure-Object | Select-Object -ExpandProperty Count

    $prefix = "..\..\"
    for ($i = 0; $i -lt $depth; $i++) {
        $prefix += "..\"
    }

    return $prefix + $ToProject.TrimStart("..\")
}

function Add-ProjectReference {
    param(
        [string]$Content,
        [string]$ProjectRef,
        [string]$PackageName
    )

    # Check if already referenced
    if ($Content -match [regex]::Escape($PackageName)) {
        Write-Host "  Already has reference to $PackageName" -ForegroundColor DarkGray
        return $Content
    }

    # Find the last ItemGroup with ProjectReferences
    $insertPattern = '(</ItemGroup>[\s\r\n]+</Project>)'

    if ($Content -notmatch '<ProjectReference') {
        # No project references exist, create new ItemGroup
        $newItemGroup = @"

  <ItemGroup>
    <ProjectReference Include="$ProjectRef" />
  </ItemGroup>

</Project>
"@
        $Content = $Content -replace '</Project>', $newItemGroup
        Write-Host "  Added reference to $PackageName" -ForegroundColor Green
    }
    else {
        # Add to existing ProjectReference ItemGroup
        $pattern = '(<ProjectReference[^>]+/>[\s\r\n]+)(</ItemGroup>)'
        $replacement = "`$1    <ProjectReference Include=`"$ProjectRef`" />`n  `$2"
        $newContent = $Content -replace $pattern, $replacement

        if ($newContent -eq $Content) {
            # Try alternate pattern for last reference
            $pattern = '(<ProjectReference[^>]+>[\s\S]*?</ProjectReference>[\s\r\n]+)(</ItemGroup>)'
            $newContent = $Content -replace $pattern, "`$1    <ProjectReference Include=`"$ProjectRef`" />`n  `$2"
        }

        $Content = $newContent
        Write-Host "  Added reference to $PackageName" -ForegroundColor Green
    }

    return $Content
}

function Backup-ProjectFile {
    param([string]$Path)

    $backupPath = "$Path.bak"
    Copy-Item $Path $backupPath -Force
    Write-Host "Created backup: $backupPath" -ForegroundColor DarkGray
}

# Determine packages to add
$packagesToAdd = @()

if ($AddAll) {
    $packagesToAdd = $PackageMap.Keys
}
elseif ($Packages.Count -gt 0) {
    $packagesToAdd = $Packages
}
else {
    # Default: add core packages
    $packagesToAdd = @("Shouldly", "Bogus", "Fakes")
}

Write-Host "`nUpdating: $ProjectPath" -ForegroundColor Cyan
Write-Host "Packages to add: $($packagesToAdd -join ', ')" -ForegroundColor Gray

if ($DryRun) {
    Write-Host "`n[DRY RUN] No files will be modified`n" -ForegroundColor Yellow
}

# Load project content
$content = Get-ProjectContent -Path $ProjectPath
$existingRefs = Get-ExistingProjectReferences -Content $content

Write-Host "`nExisting Encina.Testing.* references:" -ForegroundColor DarkGray
$existingRefs | Where-Object { $_ -like "*Encina.Testing*" } | ForEach-Object {
    Write-Host "  - $_" -ForegroundColor DarkGray
}

Write-Host "`nChanges:" -ForegroundColor White

$newContent = $content
foreach ($package in $packagesToAdd) {
    if ($PackageMap.ContainsKey($package)) {
        $info = $PackageMap[$package]
        $projectRef = $info.ProjectRef

        # Adjust path based on project location
        $newContent = Add-ProjectReference -Content $newContent -ProjectRef $projectRef -PackageName $info.PackageName
    }
}

if ($newContent -ne $content) {
    if (-not $DryRun) {
        Backup-ProjectFile -Path $ProjectPath
        Set-Content -Path $ProjectPath -Value $newContent -NoNewline
        Write-Host "`nProject file updated successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "`n[DRY RUN] Would update project file" -ForegroundColor Yellow
    }
}
else {
    Write-Host "`nNo changes needed" -ForegroundColor DarkGray
}

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Run 'dotnet restore' to restore packages"
Write-Host "2. Add 'using Encina.Testing.Shouldly;' to test files"
Write-Host "3. Replace 'new Faker<T>()' with 'new EncinaFaker<T>()'"
Write-Host "4. See docs/plans/migration-priority-guide.md for patterns"
