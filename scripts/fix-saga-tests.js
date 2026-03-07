const fs = require('fs');

function transformSagaFile(path) {
    let content = fs.readFileSync(path, 'utf8');

    // Normalize line endings to LF
    content = content.replace(/\r\n/g, '\n').replace(/\r/g, '\n');

    // First, add using directives if missing
    if (!content.includes('using Encina.TestInfrastructure.Extensions;')) {
        content = content.replace(
            /(using Encina\.TestInfrastructure\.Fixtures;)/,
            'using Encina.TestInfrastructure.Extensions;\n$1'
        );
    }
    if (!content.includes('using LanguageExt;')) {
        content = content.replace(
            /(using Encina\.TestInfrastructure\.Extensions;)/,
            '$1\nusing LanguageExt;'
        );
    }
    if (!content.includes('using Shouldly;')) {
        content = content.replace(
            /(using LanguageExt;)/,
            '$1\nusing Shouldly;'
        );
    }

    let lines = content.split('\n');
    let result = [];
    let i = 0;

    while (i < lines.length) {
        let line = lines[i];
        let trimmed = line.trim();

        // Pattern: standalone await _store.AddAsync(...);
        if (/^\s+await _store\.AddAsync\([^)]+\);\s*$/.test(line) && !line.includes('.ShouldBeRight()')) {
            result.push(line.replace(/await _store\.AddAsync\(([^)]+)\);/, '(await _store.AddAsync($1)).ShouldBeRight();'));
            i++;
            continue;
        }

        // Pattern: standalone await _store.UpdateAsync(...);
        if (/^\s+await _store\.UpdateAsync\([^)]+\);\s*$/.test(line) && !line.includes('.ShouldBeRight()')) {
            result.push(line.replace(/await _store\.UpdateAsync\(([^)]+)\);/, '(await _store.UpdateAsync($1)).ShouldBeRight();'));
            i++;
            continue;
        }

        // Pattern: await _store.SaveChangesAsync();
        if (trimmed === 'await _store.SaveChangesAsync();') {
            result.push(line.replace('await _store.SaveChangesAsync();', '(await _store.SaveChangesAsync()).ShouldBeRight();'));
            i++;
            continue;
        }

        // Pattern: var stuckSagas = await _store.GetStuckSagasAsync(...);
        if (/var stuckSagas = await _store\.GetStuckSagasAsync\(/.test(line) && !line.includes('.ShouldBeRight()')) {
            result.push(line.replace(
                /var stuckSagas = await _store\.GetStuckSagasAsync\(([^;]+)\);/,
                'var stuckSagas = (await _store.GetStuckSagasAsync($1)).ShouldBeRight();'
            ));
            i++;
            continue;
        }

        // Pattern: var expiredSagas = await _store.GetExpiredSagasAsync(...);
        if (/var expiredSagas = await _store\.GetExpiredSagasAsync\(/.test(line) && !line.includes('.ShouldBeRight()')) {
            result.push(line.replace(
                /var expiredSagas = await _store\.GetExpiredSagasAsync\(([^;]+)\);/,
                'var expiredSagas = (await _store.GetExpiredSagasAsync($1)).ShouldBeRight();'
            ));
            i++;
            continue;
        }

        // Pattern: var XXX = await _store.GetAsync(YYY); with Assert.NotNull/Null
        let getAsyncMatch = line.match(/^(\s*)var (retrieved\d?|result) = await _store\.GetAsync\(([^)]+)\);\s*$/);
        if (getAsyncMatch) {
            let indent = getAsyncMatch[1];
            let varName = getAsyncMatch[2];
            let args = getAsyncMatch[3];

            // Look ahead for Assert.NotNull or Assert.Null
            let foundAssertion = false;
            let assertType = null;
            let assertLineIndex = -1;

            for (let j = i + 1; j < Math.min(i + 6, lines.length); j++) {
                let nextLine = lines[j].trim();
                if (nextLine === `Assert.NotNull(${varName});`) {
                    foundAssertion = true;
                    assertType = 'NotNull';
                    assertLineIndex = j;
                    break;
                } else if (nextLine === `Assert.Null(${varName});`) {
                    foundAssertion = true;
                    assertType = 'Null';
                    assertLineIndex = j;
                    break;
                }
            }

            if (foundAssertion && assertType === 'Null') {
                result.push(`${indent}var ${varName}Option = (await _store.GetAsync(${args})).ShouldBeRight();`);
                for (let j = i + 1; j < assertLineIndex; j++) {
                    result.push(lines[j]);
                }
                result.push(`${indent}${varName}Option.IsNone.ShouldBeTrue();`);
                i = assertLineIndex + 1;
                continue;
            } else if (foundAssertion && assertType === 'NotNull') {
                result.push(`${indent}var ${varName}Option = (await _store.GetAsync(${args})).ShouldBeRight();`);
                for (let j = i + 1; j < assertLineIndex; j++) {
                    result.push(lines[j]);
                }
                result.push(`${indent}${varName}Option.IsSome.ShouldBeTrue();`);
                result.push(`${indent}var ${varName} = ${varName}Option.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));`);
                i = assertLineIndex + 1;
                continue;
            }
        }

        result.push(line);
        i++;
    }

    fs.writeFileSync(path, result.join('\n'));
    console.log('Transformed ' + path);
}

const databases = ['Sqlite', 'SqlServer', 'PostgreSQL', 'MySQL'];
for (const db of databases) {
    transformSagaFile(`tests/Encina.IntegrationTests/ADO/${db}/Sagas/SagaStoreADOTests.cs`);
}
