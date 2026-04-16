# Migration script Phase 2: Handle throw patterns and remaining .Should() calls
# Uses multiline regex to handle patterns that span two lines

$files = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.GuardTests\Security' |
    Where-Object { (Get-Content $_.FullName -Raw) -match '\.Should\(\)' }

Write-Host "Files with remaining .Should(): $($files.Count)"
$processedFiles = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $original = $content

    # ---------------------------------------------------------------
    # THROW PATTERNS — Multiline (two-line) versions first
    # Pattern: act.Should().Throw<X>()\r?\n\s+.WithParameterName("y");
    # -> Should.Throw<X>(act)\r?\n\s+.ParamName.ShouldBe("y");
    # ---------------------------------------------------------------

    # Sync throw with WithParameterName on next line
    $content = [regex]::Replace(
        $content,
        '(\s*)act\.Should\(\)\.Throw<([^>]+)>\(\)\s*\r?\n(\s*)\.WithParameterName\("([^"]+)"\);',
        {
            param($m)
            $indent1 = $m.Groups[1].Value
            $exType  = $m.Groups[2].Value
            $indent2 = $m.Groups[3].Value
            $param   = $m.Groups[4].Value
            "${indent1}Should.Throw<${exType}>(act)`n${indent2}.ParamName.ShouldBe(`"${param}`");"
        }
    )

    # Async throw with WithParameterName on next line
    $content = [regex]::Replace(
        $content,
        '(\s*)await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\s*\r?\n(\s*)\.WithParameterName\("([^"]+)"\);',
        {
            param($m)
            $indent1 = $m.Groups[1].Value
            $exType  = $m.Groups[2].Value
            $indent2 = $m.Groups[3].Value
            $param   = $m.Groups[4].Value
            "${indent1}(await Should.ThrowAsync<${exType}>(act))`n${indent2}.ParamName.ShouldBe(`"${param}`");"
        }
    )

    # ---------------------------------------------------------------
    # THROW PATTERNS — Same-line versions
    # ---------------------------------------------------------------

    # Sync throw with WithParameterName on same line
    $content = [regex]::Replace(
        $content,
        'act\.Should\(\)\.Throw<([^>]+)>\(\)\s*\.\s*WithParameterName\("([^"]+)"\);',
        'Should.Throw<$1>(act).ParamName.ShouldBe("$2");'
    )

    # Async throw with WithParameterName on same line
    $content = [regex]::Replace(
        $content,
        'await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\s*\.\s*WithParameterName\("([^"]+)"\);',
        '(await Should.ThrowAsync<$1>(act)).ParamName.ShouldBe("$2");'
    )

    # Sync throw without WithParameterName (standalone)
    $content = $content -replace 'act\.Should\(\)\.Throw<([^>]+)>\(\);', 'Should.Throw<$1>(act);'

    # Async throw without WithParameterName (standalone)
    $content = $content -replace 'await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\);', 'await Should.ThrowAsync<$1>(act);'

    # NotThrow
    $content = $content -replace 'act\.Should\(\)\.NotThrow\(\);', 'Should.NotThrow(act);'

    # NotThrowAsync
    $content = $content -replace 'await act\.Should\(\)\.NotThrowAsync\(\);', 'await Should.NotThrowAsync(act);'

    # ---------------------------------------------------------------
    # REMAINING .Should() WITH MESSAGE ARGS
    # ---------------------------------------------------------------

    # .Should().NotBeNull("message") -> .ShouldNotBeNull("message")
    $content = $content -replace '\.Should\(\)\.NotBeNull\("([^"]+)"\)', '.ShouldNotBeNull("$1")'

    # .Should().BeTrue("message") -> .ShouldBeTrue("message")
    $content = $content -replace '\.Should\(\)\.BeTrue\("([^"]*)"[^)]*\)', '.ShouldBeTrue("$1")'

    # .Should().BeFalse("message") -> .ShouldBeFalse("message")
    $content = $content -replace '\.Should\(\)\.BeFalse\("([^"]*)"[^)]*\)', '.ShouldBeFalse("$1")'

    # .Should().NotBeNullOrWhiteSpace("message") -> .ShouldNotBeNullOrWhiteSpace("message")
    $content = $content -replace '\.Should\(\)\.NotBeNullOrWhiteSpace\("([^"]+)"\)', '.ShouldNotBeNullOrWhiteSpace("$1")'

    # .Should().BeGreaterThan(x) -> .ShouldBeGreaterThan(x)
    $content = $content -replace '\.Should\(\)\.BeGreaterThan\(([^)]+)\)[^;]*;', '.ShouldBeGreaterThan($1);'

    # .Should().OnlyHaveUniqueItems("message") -> .ShouldAllBeUnique("message")
    $content = $content -replace '\.Should\(\)\.OnlyHaveUniqueItems\("([^"]+)"\)', '.ShouldAllBeUnique()'

    # .Should().Contain(d => ...) -> .ShouldContain(d => ...)  (already done but re-apply for safety)
    # Note: Already handled in step 1

    # Write modified content if changed
    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $processedFiles++
        Write-Host "Modified: $($file.Name)"
    }
}

Write-Host ""
Write-Host "Phase 2 complete: $processedFiles files modified"
