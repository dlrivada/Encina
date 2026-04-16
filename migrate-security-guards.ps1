# Migration script: FluentAssertions -> Shouldly for Security guard tests

$files = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.GuardTests\Security' |
    Where-Object { (Get-Content $_.FullName -Raw) -match 'using FluentAssertions;' }

$totalFiles = $files.Count
$processedFiles = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $original = $content

    # Step 1: Replace using statement
    $content = $content -replace 'using FluentAssertions;', 'using Shouldly;'

    # Step 2: Simple bool/null assertions
    $content = $content -replace '\.Should\(\)\.BeTrue\(\)', '.ShouldBeTrue()'
    $content = $content -replace '\.Should\(\)\.BeFalse\(\)', '.ShouldBeFalse()'
    $content = $content -replace '\.Should\(\)\.BeNull\(\)', '.ShouldBeNull()'
    $content = $content -replace '\.Should\(\)\.NotBeNull\(\)', '.ShouldNotBeNull()'
    $content = $content -replace '\.Should\(\)\.BeEmpty\(\)', '.ShouldBeEmpty()'
    $content = $content -replace '\.Should\(\)\.NotBeEmpty\(\)', '.ShouldNotBeEmpty()'
    $content = $content -replace '\.Should\(\)\.NotBeNullOrWhiteSpace\(\)', '.ShouldNotBeNullOrWhiteSpace()'

    # Step 3: Value equality
    $content = $content -replace '\.Should\(\)\.Be\(', '.ShouldBe('
    $content = $content -replace '\.Should\(\)\.NotBe\(', '.ShouldNotBe('
    $content = $content -replace '\.Should\(\)\.BeSameAs\(', '.ShouldBeSameAs('

    # Step 4: Collection assertions
    $content = $content -replace '\.Should\(\)\.BeEmpty\(\)', '.ShouldBeEmpty()'
    $content = $content -replace '\.Should\(\)\.NotBeEmpty\(\)', '.ShouldNotBeEmpty()'
    $content = $content -replace '\.Should\(\)\.Contain\(', '.ShouldContain('
    $content = $content -replace '\.Should\(\)\.ContainKey\(', '.ShouldContainKey('
    $content = $content -replace '\.Should\(\)\.ContainSingle\(\)', '.ShouldHaveSingleItem()'
    $content = $content -replace '\.Should\(\)\.HaveCount\(', '.Count.ShouldBe('
    $content = $content -replace '\.Should\(\)\.BeOfType<([^>]+)>\(\)', '.ShouldBeOfType<$1>()'
    $content = $content -replace '\.Should\(\)\.BeInAscendingOrder\(([^)]+)\)', '.ShouldBeInOrder(SortDirection.Ascending)'
    $content = $content -replace '\.Should\(\)\.BeApproximately\(([^,]+),\s*([^)]+)\)', '.ShouldBeInRange($1 - $2, $1 + $2)'

    # Write modified content if changed
    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $processedFiles++
        Write-Host "Modified (step 1): $($file.Name)"
    }
}

Write-Host ""
Write-Host "Step 1 complete: $processedFiles of $totalFiles files modified"
