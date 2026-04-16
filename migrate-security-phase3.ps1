# Migration Phase 3: Handle remaining complex .Should() patterns

$files = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.GuardTests\Security' |
    Where-Object { (Get-Content $_.FullName -Raw) -match '\.Should\(\)' }

Write-Host "Files with remaining .Should(): $($files.Count)"
$processedFiles = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $original = $content

    # ---------------------------------------------------------------
    # Pattern: act.Should().Throw<X>()\r?\n\s+.WithMessage("*...*");
    # -> Should.Throw<X>(act).Message.ShouldContain("...");
    # ---------------------------------------------------------------
    # Single-wildcard-surrounded pattern: extract first meaningful segment
    $content = [regex]::Replace(
        $content,
        '(\s+)act\.Should\(\)\.Throw<([^>]+)>\(\)\s*\r?\n(\s+)\.WithMessage\(\$?"([^"]+)"\);',
        {
            param($m)
            $indent1 = $m.Groups[1].Value
            $exType  = $m.Groups[2].Value
            $indent2 = $m.Groups[3].Value
            $msg     = $m.Groups[4].Value
            # Remove * wildcards, keep first meaningful part
            $cleanMsg = $msg -replace '^\*', '' -replace '\*$', '' -replace '\*', ' '
            # Trim and take substring up to first space chunk
            $parts = ($cleanMsg -split '\s+\*\s+|\*') | Where-Object { $_ -ne '' } | Select-Object -First 1
            $firstPart = $parts.Trim().Trim('*')
            "${indent1}Should.Throw<${exType}>(act).Message.ShouldContain(`"${firstPart}`");"
        }
    )

    # Same-line version: act.Should().Throw<X>().WithMessage("*...*");
    $content = [regex]::Replace(
        $content,
        'act\.Should\(\)\.Throw<([^>]+)>\(\)\.WithMessage\(\$?"([^"]+)"\);',
        {
            param($m)
            $exType  = $m.Groups[1].Value
            $msg     = $m.Groups[2].Value
            $cleanMsg = $msg -replace '^\*', '' -replace '\*$', '' -replace '\*', ' '
            $parts = ($cleanMsg -split '\s+') | Where-Object { $_ -ne '' } | Select-Object -First 1
            $firstPart = $parts.Trim()
            "Should.Throw<${exType}>(act).Message.ShouldContain(`"${firstPart}`");"
        }
    )

    # ---------------------------------------------------------------
    # Pattern: act.Should().ThrowAsync<X>(); (WITHOUT await - sync void test methods)
    # These are void test methods where the async assertion is NOT awaited.
    # Convert to: _ = act.Should().ThrowAsync<X>(); -> Should.NotThrow(() => act());
    # Actually: since these are "act" variables (Func<Task>), the correct Shouldly is:
    # act.ShouldThrowAsync<X>().GetAwaiter().GetResult(); -> or just mark as async
    # Best approach: change to act.Should().ThrowAsync -> _ = act.ShouldThrowAsync<X>();
    # But that still needs await. The cleanest fix is:
    # Change method return type or use .Wait()
    # For now: Should.Throw<X>(act.Invoke) won't work for async.
    # Use: act().ShouldThrow<X>() if the lambda wraps the call
    # Actually for non-awaited async assertions: just use Should.ThrowAsync(...).GetAwaiter().GetResult()
    # ---------------------------------------------------------------

    # Pattern: act.Should().ThrowAsync<X>(); (NOT awaited, in void method)
    # -> Should.ThrowAsync<X>(act).GetAwaiter().GetResult();
    $content = [regex]::Replace(
        $content,
        '(\s+)act\.Should\(\)\.ThrowAsync<([^>]+)>\(\);',
        '${1}Should.ThrowAsync<$2>(act).GetAwaiter().GetResult();'
    )

    # ---------------------------------------------------------------
    # Pattern: (await act.Should().ThrowAsync<X>()).WithParameterName("y");
    # -> (await Should.ThrowAsync<X>(act)).ParamName.ShouldBe("y");
    # ---------------------------------------------------------------
    $content = [regex]::Replace(
        $content,
        '\(await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\)\s*\r?\n(\s+)\.WithParameterName\("([^"]+)"\);',
        {
            param($m)
            $exType = $m.Groups[1].Value
            $indent = $m.Groups[2].Value
            $param  = $m.Groups[3].Value
            "(await Should.ThrowAsync<${exType}>(act))`n${indent}.ParamName.ShouldBe(`"${param}`");"
        }
    )

    # Same-line version
    $content = [regex]::Replace(
        $content,
        '\(await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\)\.WithParameterName\("([^"]+)"\);',
        '(await Should.ThrowAsync<$1>(act)).ParamName.ShouldBe("$2");'
    )

    # ---------------------------------------------------------------
    # Pattern: await act.Should().ThrowAsync<X>() with WithParameterName on next line
    # (still remaining from phase 2 that had different whitespace)
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

    # Remaining standalone: await act.Should().ThrowAsync<X>();
    $content = [regex]::Replace(
        $content,
        '(\s+)await act\.Should\(\)\.ThrowAsync<([^>]+)>\(\);',
        '${1}await Should.ThrowAsync<$2>(act);'
    )

    # ---------------------------------------------------------------
    # Multiline .Should().BeTrue( "message"\n) and .Should().BeFalse(
    # ---------------------------------------------------------------
    # Pattern: .Should().BeTrue(\n    "message");
    $content = [regex]::Replace(
        $content,
        '\.Should\(\)\.BeTrue\(\s*\r?\n([^)]*)\);',
        {
            param($m)
            $inner = $m.Groups[1].Value.Trim()
            if ($inner -match '^"') {
                ".ShouldBeTrue(${inner});"
            } else {
                ".ShouldBeTrue();"
            }
        }
    )

    # Pattern: .Should().BeFalse(\n    "message");
    $content = [regex]::Replace(
        $content,
        '\.Should\(\)\.BeFalse\(\s*\r?\n([^)]*)\);',
        {
            param($m)
            $inner = $m.Groups[1].Value.Trim()
            if ($inner -match '^"') {
                ".ShouldBeFalse(${inner});"
            } else {
                ".ShouldBeFalse();"
            }
        }
    )

    # Pattern: .Should().NotBeNullOrWhiteSpace(\n    "message");
    $content = [regex]::Replace(
        $content,
        '\.Should\(\)\.NotBeNullOrWhiteSpace\(\s*\r?\n([^)]*)\);',
        {
            param($m)
            $inner = $m.Groups[1].Value.Trim()
            ".ShouldNotBeNullOrWhiteSpace(${inner});"
        }
    )

    # ---------------------------------------------------------------
    # Pattern: services.Should().NotContain(d => ...) (multiline lambda)
    # -> services.ShouldNotContain(d => ...)
    # ---------------------------------------------------------------
    $content = $content -replace '\.Should\(\)\.NotContain\(', '.ShouldNotContain('

    # ---------------------------------------------------------------
    # Pattern: .Should().BeAssignableTo<T>()
    # -> .ShouldBeAssignableTo<T>()
    # ---------------------------------------------------------------
    $content = $content -replace '\.Should\(\)\.BeAssignableTo<([^>]+)>\(\)', '.ShouldBeAssignableTo<$1>()'

    # ---------------------------------------------------------------
    # Pattern: .Should().BeEquivalentTo(...)
    # -> .ShouldBe(...) (for simple cases)
    # ---------------------------------------------------------------
    $content = $content -replace '\.Should\(\)\.BeEquivalentTo\(', '.ShouldBe('

    # ---------------------------------------------------------------
    # Pattern: await act.Should().NotThrowAsync("message")
    # -> await Should.NotThrowAsync(act)
    # ---------------------------------------------------------------
    $content = [regex]::Replace(
        $content,
        '(\s+)await act\.Should\(\)\.NotThrowAsync\([^)]*\);',
        '${1}await Should.NotThrowAsync(act);'
    )

    # ---------------------------------------------------------------
    # Pattern: .Should().Contain(d => ...) multiline (services.Should().Contain)
    # -> .ShouldContain(d => ...)
    # ---------------------------------------------------------------
    # Already handled by previous step but ensure it's done

    # Write modified content if changed
    if ($content -ne $original) {
        Set-Content -Path $file.FullName -Value $content -Encoding UTF8 -NoNewline
        $processedFiles++
        Write-Host "Modified: $($file.Name)"
    }
}

Write-Host ""
Write-Host "Phase 3 complete: $processedFiles files modified"
