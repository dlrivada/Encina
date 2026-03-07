const fs = require('fs');

function transformInboxFile(path) {
    let content = fs.readFileSync(path, 'utf8');
    let lines = content.split('\n');
    let result = [];
    let i = 0;

    while (i < lines.length) {
        let line = lines[i];

        // Check if this line is: var XXX = await _store.GetMessageAsync(YYY);
        let getMessageMatch = line.match(/^(\s*)var (retrieved|result|msg1|msg2) = await _store\.GetMessageAsync\(([^)]+)\);$/);

        if (getMessageMatch) {
            let indent = getMessageMatch[1];
            let varName = getMessageMatch[2];
            let args = getMessageMatch[3];

            // Look ahead for Assert.NotNull or Assert.Null within the next 5 lines (allowing blank/comment lines)
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
                } else if (nextLine === `Assert.Null(${varName}); // Deleted`) {
                    foundAssertion = true;
                    assertType = 'NullWithComment';
                    assertLineIndex = j;
                    break;
                } else if (nextLine === `Assert.NotNull(${varName}); // Still exists`) {
                    foundAssertion = true;
                    assertType = 'NotNullWithComment';
                    assertLineIndex = j;
                    break;
                } else if (nextLine === `Assert.Null(${varName}); // Should still exist` ||
                           nextLine.startsWith(`Assert.Null(${varName}); //`) ||
                           nextLine.startsWith(`Assert.NotNull(${varName}); //`)) {
                    // Handle comments after assertion
                    if (nextLine.startsWith(`Assert.Null(${varName})`)) {
                        foundAssertion = true;
                        assertType = 'NullWithComment';
                        assertLineIndex = j;
                        break;
                    } else {
                        foundAssertion = true;
                        assertType = 'NotNullWithComment';
                        assertLineIndex = j;
                        break;
                    }
                }
            }

            if (foundAssertion && (assertType === 'Null' || assertType === 'NullWithComment')) {
                // Replace with Option IsNone pattern
                result.push(`${indent}var ${varName}Option = (await _store.GetMessageAsync(${args})).ShouldBeRight();`);
                // Copy lines between the GetMessage call and the Assert, excluding the Assert line
                for (let j = i + 1; j < assertLineIndex; j++) {
                    result.push(lines[j]);
                }
                let commentSuffix = '';
                if (assertType === 'NullWithComment') {
                    let m = lines[assertLineIndex].match(/Assert\.\w+\([^)]+\);\s*(\/\/.*)$/);
                    if (m) commentSuffix = ' ' + m[1];
                }
                result.push(`${indent}${varName}Option.IsNone.ShouldBeTrue();${commentSuffix}`);
                i = assertLineIndex + 1;
                continue;
            } else if (foundAssertion && (assertType === 'NotNull' || assertType === 'NotNullWithComment')) {
                // Replace with Option IsSome + Match pattern
                result.push(`${indent}var ${varName}Option = (await _store.GetMessageAsync(${args})).ShouldBeRight();`);
                // Copy lines between the GetMessage call and the Assert, excluding the Assert line
                for (let j = i + 1; j < assertLineIndex; j++) {
                    result.push(lines[j]);
                }
                let commentSuffix = '';
                if (assertType === 'NotNullWithComment') {
                    let m = lines[assertLineIndex].match(/Assert\.\w+\([^)]+\);\s*(\/\/.*)$/);
                    if (m) commentSuffix = ' ' + m[1];
                }
                result.push(`${indent}${varName}Option.IsSome.ShouldBeTrue();${commentSuffix}`);
                result.push(`${indent}var ${varName} = ${varName}Option.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));`);
                i = assertLineIndex + 1;
                continue;
            } else {
                // No matching assertion found - this shouldn't happen in well-structured tests
                // but just emit the line as-is
                result.push(line);
                i++;
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
    transformInboxFile(`tests/Encina.IntegrationTests/ADO/${db}/Inbox/InboxStoreADOTests.cs`);
}
