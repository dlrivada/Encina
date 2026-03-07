const fs = require('fs');

function transformSchedulingFile(path) {
    let content = fs.readFileSync(path, 'utf8');

    // Normalize line endings to LF
    content = content.replace(/\r\n/g, '\n').replace(/\r/g, '\n');

    // Add using directives if missing
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

    // Transform standalone await _store.AddAsync(xxx); (no var assignment, no ShouldBeRight already)
    content = content.replace(
        /^(\s+)await _store\.AddAsync\(([^)]+)\);$/gm,
        '$1(await _store.AddAsync($2)).ShouldBeRight();'
    );

    // Transform var xxx = await _store.GetDueMessagesAsync(yyy);
    content = content.replace(
        /^(\s+)var (\w+) = await _store\.GetDueMessagesAsync\(([^)]+)\);$/gm,
        '$1var $2 = (await _store.GetDueMessagesAsync($3)).ShouldBeRight();'
    );

    // Transform standalone await _store.MarkAsProcessedAsync(xxx);
    content = content.replace(
        /^(\s+)await _store\.MarkAsProcessedAsync\(([^)]+)\);$/gm,
        '$1(await _store.MarkAsProcessedAsync($2)).ShouldBeRight();'
    );

    // Transform standalone await _store.MarkAsFailedAsync(xxx, yyy, zzz);
    content = content.replace(
        /^(\s+)await _store\.MarkAsFailedAsync\(([^;]+)\);$/gm,
        '$1(await _store.MarkAsFailedAsync($2)).ShouldBeRight();'
    );

    // Transform standalone await _store.RescheduleRecurringMessageAsync(xxx, yyy);
    content = content.replace(
        /^(\s+)await _store\.RescheduleRecurringMessageAsync\(([^)]+)\);$/gm,
        '$1(await _store.RescheduleRecurringMessageAsync($2)).ShouldBeRight();'
    );

    // Transform standalone await _store.CancelAsync(xxx);
    content = content.replace(
        /^(\s+)await _store\.CancelAsync\(([^)]+)\);$/gm,
        '$1(await _store.CancelAsync($2)).ShouldBeRight();'
    );

    // Transform standalone await _store.SaveChangesAsync();
    content = content.replace(
        /^(\s+)await _store\.SaveChangesAsync\(\);$/gm,
        '$1(await _store.SaveChangesAsync()).ShouldBeRight();'
    );

    fs.writeFileSync(path, content);
    console.log('Transformed ' + path);
}

const databases = ['Sqlite', 'SqlServer', 'PostgreSQL', 'MySQL'];
for (const db of databases) {
    transformSchedulingFile(`tests/Encina.IntegrationTests/ADO/${db}/Scheduling/ScheduledMessageStoreADOTests.cs`);
}
