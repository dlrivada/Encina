$f1 = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC' | Select-String 'using FluentAssertions;' -List | Select-Object -ExpandProperty Path
$f2 = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\AntiTampering' | Select-String 'using FluentAssertions;' -List | Select-Object -ExpandProperty Path
$f3 = Get-ChildItem -Recurse -Filter '*.cs' -Path 'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit' | Select-String 'using FluentAssertions;' -List | Select-Object -ExpandProperty Path
$files = $f1 + $f2 + $f3
Write-Host "Processing $($files.Count) files..."

foreach ($file in $files) {
    $content = Get-Content $file -Raw

    # Rule 1: using statement
    $content = $content -replace 'using FluentAssertions;', 'using Shouldly;'

    # Throw patterns - must be done BEFORE generic .Should() replacements
    # Use [regex]::Replace for multiline patterns

    # async ThrowAsync with WithParameterName multiline (CRLF and LF)
    $content = [regex]::Replace($content, 'await (\w+)\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\r?\n(\s*)\.WithParameterName\("([^"]+)"\);', {
        param($m)
        "(await Should.ThrowAsync<$($m.Groups[2].Value)>($($m.Groups[1].Value)))`n$($m.Groups[3].Value)    .ParamName.ShouldBe(`"$($m.Groups[4].Value)`");"
    })

    # sync Throw with WithParameterName multiline
    $content = [regex]::Replace($content, '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\)\r?\n(\s*)\.WithParameterName\("([^"]+)"\);', {
        param($m)
        "Should.Throw<$($m.Groups[2].Value)>($($m.Groups[1].Value))`n$($m.Groups[3].Value)    .ParamName.ShouldBe(`"$($m.Groups[4].Value)`");"
    })

    # sync Throw with WithMessage multiline
    $content = [regex]::Replace($content, '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\)\r?\n(\s*)\.WithMessage\("(\*?)([^"]+)(\*?)"\);', {
        param($m)
        "Should.Throw<$($m.Groups[2].Value)>($($m.Groups[1].Value)).Message.ShouldContain(`"$($m.Groups[5].Value)`");"
    })

    # async ThrowAsync with WithParameterName single-line
    $content = $content -replace 'await (\w+)\.Should\(\)\.ThrowAsync<([^>]+)>\(\)\.WithParameterName\("([^"]+)"\);', '(await Should.ThrowAsync<$2>($1)).ParamName.ShouldBe("$3");'

    # sync Throw with WithParameterName single-line
    $content = $content -replace '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\)\.WithParameterName\("([^"]+)"\);', 'Should.Throw<$2>($1).ParamName.ShouldBe("$3");'

    # sync Throw with WithMessage single-line
    $content = $content -replace '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\)\.WithMessage\("\*?([^"*]+)\*?"\);', 'Should.Throw<$2>($1).Message.ShouldContain("$3");'

    # async ThrowAsync simple
    $content = $content -replace 'await (\w+)\.Should\(\)\.ThrowAsync<([^>]+)>\(\);', 'await Should.ThrowAsync<$2>($1);'

    # sync Throw simple (must come after WithParameterName/WithMessage patterns)
    $content = $content -replace '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\);', 'Should.Throw<$2>($1);'

    # NotThrow async
    $content = $content -replace 'await (\w+)\.Should\(\)\.NotThrowAsync\(\);', 'await Should.NotThrowAsync($1);'

    # NotThrow sync
    $content = $content -replace '(\w+)\.Should\(\)\.NotThrow\(([^)]*)\);', 'Should.NotThrow($1);'

    # BeTrue / BeFalse with reason string argument
    $content = $content -replace '\.Should\(\)\.BeTrue\("([^"]+)"\)', '.ShouldBeTrue("$1")'
    $content = $content -replace '\.Should\(\)\.BeFalse\("([^"]+)"\)', '.ShouldBeFalse("$1")'
    $content = $content -replace '\.Should\(\)\.BeTrue\(', '.ShouldBeTrue('
    $content = $content -replace '\.Should\(\)\.BeFalse\(', '.ShouldBeFalse('

    # BeNull / NotBeNull with reason
    $content = $content -replace '\.Should\(\)\.BeNull\("([^"]+)"\)', '.ShouldBeNull("$1")'
    $content = $content -replace '\.Should\(\)\.NotBeNull\("([^"]+)"\)', '.ShouldNotBeNull("$1")'
    $content = $content -replace '\.Should\(\)\.BeNull\(', '.ShouldBeNull('
    $content = $content -replace '\.Should\(\)\.NotBeNull\(', '.ShouldNotBeNull('

    # Be / NotBe
    $content = $content -replace '\.Should\(\)\.Be\(', '.ShouldBe('
    $content = $content -replace '\.Should\(\)\.NotBe\(', '.ShouldNotBe('

    # Contain / NotContain
    $content = $content -replace '\.Should\(\)\.Contain\(', '.ShouldContain('
    $content = $content -replace '\.Should\(\)\.NotContain\(', '.ShouldNotContain('

    # StartWith / EndWith
    $content = $content -replace '\.Should\(\)\.StartWith\(', '.ShouldStartWith('
    $content = $content -replace '\.Should\(\)\.EndWith\(', '.ShouldEndWith('

    # BeEmpty / NotBeEmpty with reason
    $content = $content -replace '\.Should\(\)\.BeEmpty\("([^"]+)"\)', '.ShouldBeEmpty("$1")'
    $content = $content -replace '\.Should\(\)\.NotBeEmpty\("([^"]+)"\)', '.ShouldNotBeEmpty("$1")'
    $content = $content -replace '\.Should\(\)\.BeEmpty\(\)', '.ShouldBeEmpty()'
    $content = $content -replace '\.Should\(\)\.NotBeEmpty\(\)', '.ShouldNotBeEmpty()'

    # NotBeNullOrWhiteSpace / NotBeNullOrEmpty
    $content = $content -replace '\.Should\(\)\.NotBeNullOrWhiteSpace\(', '.ShouldNotBeNullOrWhiteSpace('
    $content = $content -replace '\.Should\(\)\.NotBeNullOrEmpty\(\)', '.ShouldNotBeNullOrEmpty()'

    # BeOfType / BeAssignableTo
    $content = $content -replace '\.Should\(\)\.BeOfType\(', '.ShouldBeOfType('
    $content = $content -replace '\.Should\(\)\.BeOfType<', '.ShouldBeOfType<'
    $content = $content -replace '\.Should\(\)\.BeAssignableTo<', '.ShouldBeAssignableTo<'

    # BeSameAs / NotBeSameAs
    $content = $content -replace '\.Should\(\)\.BeSameAs\(', '.ShouldBeSameAs('
    $content = $content -replace '\.Should\(\)\.NotBeSameAs\(', '.ShouldNotBeSameAs('

    # Numeric comparisons
    $content = $content -replace '\.Should\(\)\.BeGreaterThan\(', '.ShouldBeGreaterThan('
    $content = $content -replace '\.Should\(\)\.BeLessThan\(', '.ShouldBeLessThan('
    $content = $content -replace '\.Should\(\)\.BeGreaterThanOrEqualTo\(', '.ShouldBeGreaterThanOrEqualTo('
    $content = $content -replace '\.Should\(\)\.BeLessThanOrEqualTo\(', '.ShouldBeLessThanOrEqualTo('
    $content = $content -replace '\.Should\(\)\.BeInRange\(', '.ShouldBeInRange('

    # HaveCount variants
    $content = $content -replace '\.Should\(\)\.HaveCountGreaterThan\(', '.Count.ShouldBeGreaterThan('
    $content = $content -replace '\.Should\(\)\.HaveCountGreaterThanOrEqualTo\(', '.Count.ShouldBeGreaterThanOrEqualTo('
    $content = $content -replace '\.Should\(\)\.HaveCountLessThanOrEqualTo\(', '.Count.ShouldBeLessThanOrEqualTo('
    $content = $content -replace '\.Should\(\)\.HaveCount\(', '.Count.ShouldBe('

    # ContainKey / NotContainKey
    $content = $content -replace '\.Should\(\)\.ContainKey\(', '.ShouldContainKey('
    $content = $content -replace '\.Should\(\)\.NotContainKey\(', '.ShouldNotContainKey('

    # OnlyHaveUniqueItems
    $content = $content -replace '\.Should\(\)\.OnlyHaveUniqueItems\(\)', '.ShouldBeUnique()'

    # AllSatisfy / OnlyContain
    $content = $content -replace '\.Should\(\)\.AllSatisfy\(', '.ShouldAllBe('
    $content = $content -replace '\.Should\(\)\.OnlyContain\(', '.ShouldAllBe('

    # ContainSingle
    $content = $content -replace '\.Should\(\)\.ContainSingle\(\)', '.ShouldHaveSingleItem()'

    # HaveLength
    $content = $content -replace '\.Should\(\)\.HaveLength\(', '.Length.ShouldBe('

    # HaveFlag
    $content = $content -replace '\.Should\(\)\.HaveFlag\(', '.ShouldHaveFlag('

    # BeEquivalentTo
    $content = $content -replace '\.Should\(\)\.BeEquivalentTo\(', '.ShouldBe('

    # DateTime comparisons
    $content = $content -replace '\.Should\(\)\.BeOnOrAfter\(', '.ShouldBeGreaterThanOrEqualTo('
    $content = $content -replace '\.Should\(\)\.BeOnOrBefore\(', '.ShouldBeLessThanOrEqualTo('
    $content = $content -replace '\.Should\(\)\.BeBefore\(', '.ShouldBeLessThan('

    # BePositive / BeNegative
    $content = $content -replace '\.Should\(\)\.BePositive\(\)', '.ShouldBeGreaterThan(0)'
    $content = $content -replace '\.Should\(\)\.BeNegative\(\)', '.ShouldBeLessThan(0)'

    # MatchRegex
    $content = $content -replace '\.Should\(\)\.MatchRegex\(', '.ShouldMatch('

    Set-Content -Path $file -Value $content -NoNewline
}

Write-Host "Done! Processed $($files.Count) files"
