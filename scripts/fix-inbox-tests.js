const fs = require('fs');

function transformInboxFile(path) {
    let content = fs.readFileSync(path, 'utf8');

    // Pattern for NotNull assertions: var XXX = await _store.GetMessageAsync(YYY);  \n  Assert.NotNull(XXX);
    content = content.replace(
        /var (retrieved|result|msg1|msg2) = await _store\.GetMessageAsync\(([^)]+)\);\s*\n(\s*)Assert\.NotNull\(\1\);/g,
        (match, varName, args, indent) => {
            return `var ${varName}Option = (await _store.GetMessageAsync(${args})).ShouldBeRight();
${indent}${varName}Option.IsSome.ShouldBeTrue();
${indent}var ${varName} = ${varName}Option.Match(Some: s => s, None: () => throw new InvalidOperationException("Expected Some"));`;
        }
    );

    // Pattern for Null assertions: var XXX = await _store.GetMessageAsync(YYY);  \n  Assert.Null(XXX);
    content = content.replace(
        /var (retrieved|result|msg1|msg2) = await _store\.GetMessageAsync\(([^)]+)\);\s*\n(\s*)Assert\.Null\(\1\);/g,
        (match, varName, args, indent) => {
            return `var ${varName}Option = (await _store.GetMessageAsync(${args})).ShouldBeRight();
${indent}${varName}Option.IsNone.ShouldBeTrue();`;
        }
    );

    fs.writeFileSync(path, content);
    console.log('Transformed ' + path);
}

const databases = ['Sqlite', 'SqlServer', 'PostgreSQL', 'MySQL'];
for (const db of databases) {
    transformInboxFile(`tests/Encina.IntegrationTests/ADO/${db}/Inbox/InboxStoreADOTests.cs`);
}
