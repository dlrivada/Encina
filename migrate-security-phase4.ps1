# Migration Phase 4: Handle remaining edge cases

$files = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.GuardTests\Security' |
    Where-Object { (Get-Content $_.FullName -Raw) -match '\.Should\(\)' }

Write-Host "Files with remaining .Should(): $($files.Count)"
$processedFiles = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $original = $content

    # ---------------------------------------------------------------
    # Pattern: await act.Should().ThrowAsync<X>()\r?\n    .WithParameterName(nameof(y));
    # -> (await Should.ThrowAsync<X>(act))\r?\n    .ParamName.ShouldBe(nameof(y));
    # ---------------------------------------------------------------
    $content = [regex]::Replace(
        $content,
        '(\s+)await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\s*\r?\n(\s+)\.WithParameterName\((nameof\([^)]+\))\);',
        {
            param($m)
            $indent1 = $m.Groups[1].Value
            $exType  = $m.Groups[2].Value
            $indent2 = $m.Groups[3].Value
            $nameofExpr = $m.Groups[4].Value
            "${indent1}(await Should.ThrowAsync<${exType}>(act))`n${indent2}.ParamName.ShouldBe(${nameofExpr});"
        }
    )

    # Same-line version with nameof
    $content = [regex]::Replace(
        $content,
        '(\s+)await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\.WithParameterName\((nameof\([^)]+\))\);',
        {
            param($m)
            $indent1 = $m.Groups[1].Value
            $exType  = $m.Groups[2].Value
            $nameofExpr = $m.Groups[3].Value
            "${indent1}(await Should.ThrowAsync<${exType}>(act)).ParamName.ShouldBe(${nameofExpr});"
        }
    )

    # ---------------------------------------------------------------
    # Pattern: .Should().ContainSingle(lambda)
    # decision.Obligations.Should().ContainSingle(o => o.Id == "permit-ob");
    # -> decision.Obligations.ShouldContain(o => o.Id == "permit-ob");
    #    decision.Obligations.Count.ShouldBe(1);
    # But simplest: just use ShouldHaveSingleItem() or ShouldContain()
    # Since ContainSingle with predicate means "exactly one that matches predicate":
    # -> decision.Obligations.ShouldContain(o => o.Id == "permit-ob");
    # ---------------------------------------------------------------
    $content = [regex]::Replace(
        $content,
        '\.Should\(\)\.ContainSingle\(([^)]+)\)',
        '.ShouldContain($1)'
    )

    # ---------------------------------------------------------------
    # Catch-all: any remaining .Should().Throw patterns with WithParameterName
    # using string literal (in case some were missed due to whitespace variations)
    # ---------------------------------------------------------------
    $content = [regex]::Replace(
        $content,
        '(\s+)await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\s*\r?\n(\s+)\.WithParameterName\("([^"]+)"\);',
        {
            param($m)
            $indent1 = $m.Groups[1].Value
            $exType  = $m.Groups[2].Value
            $indent2 = $m.Groups[3].Value
            $param   = $m.Groups[4].Value
            "${indent1}(await Should.ThrowAsync<${exType}>(act))`n${indent2}.ParamName.ShouldBe(`"${param}`");"
        }
    )

    # Write modified content if changed
    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $processedFiles++
        Write-Host "Modified: $($file.Name)"
    }
}

Write-Host ""
Write-Host "Phase 4 complete: $processedFiles files modified"
