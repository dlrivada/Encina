$files = @(
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Builders\TargetBuilderTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\CombiningAlgorithms\OnlyOneApplicableAlgorithmTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Functions\ArithmeticFunctionsTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Functions\BagFunctionsTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Functions\DefaultFunctionRegistryTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Functions\TypeConversionFunctionsTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Persistence\PersistentPolicyAdministrationPointTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\ABAC\Persistence\Xacml\XacmlMappingExtensionsTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\AntiTampering\RequestSigningClientTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit\AuditRetentionServiceTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit\DefaultAuditEntryFactoryTests.cs',
    'D:\Proyectos\Encina\tests\Encina.UnitTests\Security\Audit\RequestMetadataExtractorTests.cs'
)

foreach ($file in $files) {
    $content = Get-Content $file -Raw
    $name = Split-Path $file -Leaf

    # 1. BeEmpty("reason") with message on next line → ShouldBeEmpty("reason")
    $content = [regex]::Replace($content, '\.Should\(\)\.BeEmpty\(\r?\n(\s*)"([^"]+)"\)', ".ShouldBeEmpty(`"`$2`")")
    $content = [regex]::Replace($content, '\.Should\(\)\.BeEmpty\(\r?\n(\s*)''([^'']+)''\)', ".ShouldBeEmpty('`$2')")

    # 2. BeEmpty with inline message
    $content = $content -replace '\.Should\(\)\.BeEmpty\("([^"]+)"\)', '.ShouldBeEmpty("$1")'
    $content = $content -replace '\.Should\(\)\.BeEmpty\(', '.ShouldBeEmpty('

    # 3. BeApproximately(expected, precision) → ShouldBeInRange(expected - precision, expected + precision)
    $content = [regex]::Replace($content, '\.Should\(\)\.BeApproximately\(([^,]+),\s*([^)]+)\)', {
        param($m)
        $expected = $m.Groups[1].Value.Trim()
        $precision = $m.Groups[2].Value.Trim()
        ".ShouldBeInRange($expected - $precision, $expected + $precision)"
    })

    # 4. AllBe (FA custom extension) → ShouldAllBe
    $content = $content -replace '\.Should\(\)\.AllBe\(', '.ShouldAllBe('

    # 5. BeInAscendingOrder → manual check with OrderBy
    $content = [regex]::Replace($content, '(\w+)\.Should\(\)\.BeInAscendingOrder\(([^)]*)\);', {
        param($m)
        $varName = $m.Groups[1].Value
        $comparer = $m.Groups[2].Value.Trim()
        if ($comparer) {
            "$varName.ShouldBe($varName.OrderBy(x => x, $comparer));"
        } else {
            "$varName.ShouldBe($varName.OrderBy(x => x));"
        }
    })

    # 6. act.Should().ThrowAsync<T>() (missing await) → await Should.ThrowAsync<T>(act)
    $content = $content -replace '(\w+)\.Should\(\)\.ThrowAsync<([^>]+)>\(\);', 'await Should.ThrowAsync<$2>($1);'

    # 7. ContainSingle("value") → ShouldHaveSingleItem().ShouldBe("value")
    $content = $content -replace '\.Should\(\)\.ContainSingle\("([^"]+)"\)', '.ShouldHaveSingleItem().ShouldBe("$1")'

    # 8. .Should().NotThrowAsync() (chained with leading dot, no variable)
    # These are: await someExpr.Invoking(x => x.Method()).Should().NotThrowAsync()
    # → await Should.NotThrowAsync(() => someExpr.Method()) - but that requires restructuring
    # Simple approach: remove .Should().NotThrowAsync() and use await directly
    $content = [regex]::Replace($content, '(\w+)\.Invoking\(([^)]+)\)\r?\n(\s*)\.Should\(\)\.NotThrowAsync\(\);', {
        param($m)
        $varName = $m.Groups[1].Value
        $lambda = $m.Groups[2].Value.Trim()
        # lambda is like: s => s.StartAsync(cts.Token)
        # convert to: await varName.StartAsync(cts.Token)
        # or just use Should.NotThrowAsync
        "await Should.NotThrowAsync(async () => { $($lambda -replace '^(\w+)\s*=>', '$varName =>') });"
    })

    # 9. BeCloseTo → range check
    $content = [regex]::Replace($content, '\.Should\(\)\.BeCloseTo\(([^,]+),\s*([^)]+)\)', {
        param($m)
        $expected = $m.Groups[1].Value.Trim()
        $tolerance = $m.Groups[2].Value.Trim()
        ".ShouldBeInRange($expected - $tolerance, $expected + $tolerance)"
    })

    # 10. Throw<T>() with chained WithInnerException<U>()
    $content = [regex]::Replace($content, '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\)\r?\n(\s*)\.WithInnerException<([^>]+)>\(\);', {
        param($m)
        $act = $m.Groups[1].Value
        $exType = $m.Groups[2].Value
        $innerType = $m.Groups[4].Value
        "Should.Throw<$exType>($act).InnerException.ShouldBeOfType<$innerType>();"
    })

    # 11. Throw<T>() with chained WithInnerException single-line
    $content = $content -replace '(\w+)\.Should\(\)\.Throw<([^>]+)>\(\)\.WithInnerException<([^>]+)>\(\);', 'Should.Throw<$2>($1).InnerException.ShouldBeOfType<$3>();'

    # 12. Throw<T>("reason") with extra param
    $content = $content -replace '(\w+)\.Should\(\)\.Throw<([^>]+)>\("([^"]+)"\);', 'Should.Throw<$2>($1);'

    Set-Content -Path $file -Value $content -NoNewline
    Write-Host "Fixed: $name"
}

Write-Host "Done fixing remaining files"
