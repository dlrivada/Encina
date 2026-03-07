const fs = require('fs');

const databases = ['SqlServer', 'PostgreSQL', 'MySQL'];
for (const db of databases) {
    const path = `tests/Encina.IntegrationTests/ADO/${db}/Inbox/InboxStoreADOTests.cs`;
    let content = fs.readFileSync(path, 'utf8');

    // Fix the RemoveExpiredMessagesAsync_ValidIds test: both msg1 and msg2 should be None (deleted)
    content = content.replace(
        /        var msg1Option = \(await _store\.GetMessageAsync\("msg-1"\)\)\.ShouldBeRight\(\);\n        var msg2 = await _store\.GetMessageAsync\("msg-2"\);\n        msg1Option\.IsNone\.ShouldBeTrue\(\);\n        Assert\.Null\(msg2\);/g,
        `        var msg1Option = (await _store.GetMessageAsync("msg-1")).ShouldBeRight();
        var msg2Option = (await _store.GetMessageAsync("msg-2")).ShouldBeRight();
        msg1Option.IsNone.ShouldBeTrue();
        msg2Option.IsNone.ShouldBeTrue();`
    );

    // Fix the RemoveExpiredMessagesAsync_PartialIds test: msg1 is None (deleted), msg2 is Some (still exists)
    content = content.replace(
        /        var msg1Option = \(await _store\.GetMessageAsync\("msg-1"\)\)\.ShouldBeRight\(\);\n        var msg2 = await _store\.GetMessageAsync\("msg-2"\);\n        msg1Option\.IsNone\.ShouldBeTrue\(\); \/\/ Deleted\n        Assert\.NotNull\(msg2\); \/\/ Still exists/g,
        `        var msg1Option = (await _store.GetMessageAsync("msg-1")).ShouldBeRight();
        var msg2Option = (await _store.GetMessageAsync("msg-2")).ShouldBeRight();
        msg1Option.IsNone.ShouldBeTrue(); // Deleted
        msg2Option.IsSome.ShouldBeTrue(); // Still exists`
    );

    fs.writeFileSync(path, content);
    console.log('Fixed ' + path);
}
