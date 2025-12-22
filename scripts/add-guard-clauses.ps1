# PowerShell script to add guard clauses to all database providers
# This script adds ArgumentNullException and ArgumentException guards

$providers = @(
    "Encina.Dapper.MySQL",
    "Encina.Dapper.Sqlite",
    "Encina.Dapper.Oracle",
    "Encina.ADO.SqlServer",
    "Encina.ADO.PostgreSQL",
    "Encina.ADO.MySQL",
    "Encina.ADO.Sqlite",
    "Encina.ADO.Oracle"
)

$rootPath = "D:\Proyectos\Encina\src"

function Add-GuardClauses {
    param (
        [string]$FilePath
    )

    $content = Get-Content $FilePath -Raw

    # Fix constructor tableName guard (ArgumentNullException -> ArgumentException.ThrowIfNullOrWhiteSpace)
    $content = $content -replace 'ArgumentNullException\.ThrowIfNull\(tableName\);', 'ArgumentException.ThrowIfNullOrWhiteSpace(tableName);'

    # Add AddAsync message guard if missing
    if ($content -match 'public async Task AddAsync\(I(?:Outbox|Inbox|Scheduled)Message message,' -and $content -notmatch 'AddAsync[^}]+?ArgumentNullException\.ThrowIfNull\(message\)') {
        $content = $content -replace '(public async Task AddAsync\(I(?:Outbox|Inbox|Scheduled)Message message[^\r\n]+\r?\n\s+{)', "`$1`n        ArgumentNullException.ThrowIfNull(message);"
    }

    # Add AddAsync sagaState guard if missing
    if ($content -match 'public async Task AddAsync\(ISagaState sagaState,' -and $content -notmatch 'AddAsync[^}]+?ArgumentNullException\.ThrowIfNull\(sagaState\)') {
        $content = $content -replace '(public async Task AddAsync\(ISagaState sagaState[^\r\n]+\r?\n\s+{)', "`$1`n        ArgumentNullException.ThrowIfNull(sagaState);"
    }

    # Add UpdateAsync sagaState guard if missing
    if ($content -match 'public async Task UpdateAsync\(ISagaState sagaState,' -and $content -notmatch 'UpdateAsync[^}]+?ArgumentNullException\.ThrowIfNull\(sagaState\)') {
        $content = $content -replace '(public async Task UpdateAsync\(ISagaState sagaState[^\r\n]+\r?\n\s+{)', "`$1`n        ArgumentNullException.ThrowIfNull(sagaState);"
    }

    # Add GetAsync sagaId guard if missing
    if ($content -match 'public async Task<ISagaState\?> GetAsync\(Guid sagaId,' -and $content -notmatch 'GetAsync[^}]+?sagaId == Guid\.Empty') {
        $content = $content -replace '(public async Task<ISagaState\?> GetAsync\(Guid sagaId[^\r\n]+\r?\n\s+{)', "`$1`n        if (sagaId == Guid.Empty)`n            throw new ArgumentException(`"Saga ID cannot be empty.`", nameof(sagaId));"
    }

    # Add GetPendingMessagesAsync/GetDueMessagesAsync batchSize and maxRetries guards
    if ($content -match 'public async Task<IEnumerable<I(?:Outbox|Scheduled)Message>> Get(?:Pending|Due)MessagesAsync\(' -and $content -notmatch 'batchSize <= 0') {
        $content = $content -replace '(public async Task<IEnumerable<I(?:Outbox|Scheduled)Message>> Get(?:Pending|Due)MessagesAsync\([^}]+?\r?\n\s+{)', "`$1`n        if (batchSize <= 0)`n            throw new ArgumentException(`"Batch size must be greater than zero.`", nameof(batchSize));`n        if (maxRetries < 0)`n            throw new ArgumentException(`"Max retries cannot be negative.`", nameof(maxRetries));"
    }

    # Add MarkAsProcessedAsync messageId guard
    if ($content -match 'public async Task MarkAsProcessedAsync\(Guid messageId,' -and $content -notmatch 'MarkAsProcessedAsync[^}]+?messageId == Guid\.Empty') {
        $content = $content -replace '(public async Task MarkAsProcessedAsync\(Guid messageId[^\r\n]+\r?\n\s+{)', "`$1`n        if (messageId == Guid.Empty)`n            throw new ArgumentException(`"Message ID cannot be empty.`", nameof(messageId));"
    }

    # Add MarkAsFailedAsync guards
    if ($content -match 'public async Task MarkAsFailedAsync\([^)]*Guid messageId' -and $content -notmatch 'MarkAsFailedAsync[^}]+?messageId == Guid\.Empty') {
        $content = $content -replace '(public async Task MarkAsFailedAsync\([^}]+?\r?\n\s+{)', "`$1`n        if (messageId == Guid.Empty)`n            throw new ArgumentException(`"Message ID cannot be empty.`", nameof(messageId));`n        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);"
    }

    # Add GetStuckSagasAsync guards
    if ($content -match 'public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync\(' -and $content -notmatch 'olderThan <= TimeSpan\.Zero') {
        $content = $content -replace '(public async Task<IEnumerable<ISagaState>> GetStuckSagasAsync\([^}]+?\r?\n\s+{)', "`$1`n        if (olderThan <= TimeSpan.Zero)`n            throw new ArgumentException(`"OlderThan must be greater than zero.`", nameof(olderThan));`n        if (batchSize <= 0)`n            throw new ArgumentException(`"Batch size must be greater than zero.`", nameof(batchSize));"
    }

    # Add RescheduleRecurringMessageAsync guards
    if ($content -match 'public async Task RescheduleRecurringMessageAsync\(' -and $content -notmatch 'RescheduleRecurringMessageAsync[^}]+?messageId == Guid\.Empty') {
        $content = $content -replace '(public async Task RescheduleRecurringMessageAsync\([^}]+?\r?\n\s+{)', "`$1`n        if (messageId == Guid.Empty)`n            throw new ArgumentException(`"Message ID cannot be empty.`", nameof(messageId));`n        if (nextScheduledAtUtc < DateTime.UtcNow)`n            throw new ArgumentException(`"Next scheduled date cannot be in the past.`", nameof(nextScheduledAtUtc));"
    }

    # Add CancelAsync messageId guard
    if ($content -match 'public async Task CancelAsync\(Guid messageId,' -and $content -notmatch 'CancelAsync[^}]+?messageId == Guid\.Empty') {
        $content = $content -replace '(public async Task CancelAsync\(Guid messageId[^\r\n]+\r?\n\s+{)', "`$1`n        if (messageId == Guid.Empty)`n            throw new ArgumentException(`"Message ID cannot be empty.`", nameof(messageId));"
    }

    # Add GetMessageAsync messageId guard (Inbox)
    if ($content -match 'public async Task<IInboxMessage\?> GetMessageAsync\(string messageId,' -and $content -notmatch 'GetMessageAsync[^}]+?ArgumentException\.ThrowIfNullOrWhiteSpace\(messageId\)') {
        $content = $content -replace '(public async Task<IInboxMessage\?> GetMessageAsync\(string messageId[^\r\n]+\r?\n\s+{)', "`$1`n        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);"
    }

    # Add MarkAsProcessedAsync messageId guard (Inbox - string version)
    if ($content -match 'public async Task MarkAsProcessedAsync\(\s*string messageId,' -and $content -notmatch 'MarkAsProcessedAsync[^}]+?ArgumentException\.ThrowIfNullOrWhiteSpace\(messageId\)') {
        $content = $content -replace '(public async Task MarkAsProcessedAsync\(\s*string messageId[^\r\n]+\r?\n[^\r\n]+\r?\n\s+{)', "`$1`n        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);"
    }

    # Add MarkAsFailedAsync messageId guard (Inbox - string version)
    if ($content -match 'public async Task MarkAsFailedAsync\(\s*string messageId,\s*string errorMessage,' -and $content -notmatch 'MarkAsFailedAsync[^}]+?ArgumentException\.ThrowIfNullOrWhiteSpace\(messageId\)') {
        $content = $content -replace '(public async Task MarkAsFailedAsync\([^}]+?\r?\n[^\r\n]+\r?\n[^\r\n]+\r?\n\s+{)', "`$1`n        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);`n        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);"
    }

    # Add GetExpiredMessagesAsync batchSize guard
    if ($content -match 'public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync\(' -and $content -notmatch 'GetExpiredMessagesAsync[^}]+?batchSize <= 0') {
        $content = $content -replace '(public async Task<IEnumerable<IInboxMessage>> GetExpiredMessagesAsync\([^}]+?\r?\n\s+{)', "`$1`n        if (batchSize <= 0)`n            throw new ArgumentException(`"Batch size must be greater than zero.`", nameof(batchSize));"
    }

    # Add RemoveExpiredMessagesAsync guards
    if ($content -match 'public async Task RemoveExpiredMessagesAsync\(' -and $content -notmatch 'RemoveExpiredMessagesAsync[^}]+?!messageIds\.Any\(\)') {
        $content = $content -replace '(public async Task RemoveExpiredMessagesAsync\([^}]+?\r?\n\s+{)', "`$1`n        ArgumentNullException.ThrowIfNull(messageIds);`n        if (!messageIds.Any())`n            throw new ArgumentException(`"Collection cannot be empty.`", nameof(messageIds));"
    }

    # Replace existing ArgumentNullException.ThrowIfNull(errorMessage) with ArgumentException.ThrowIfNullOrWhiteSpace
    $content = $content -replace 'ArgumentNullException\.ThrowIfNull\(errorMessage\);', 'ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);'

    Set-Content $FilePath $content -NoNewline
    Write-Host "Updated: $FilePath"
}

foreach ($provider in $providers) {
    $providerPath = Join-Path $rootPath $provider

    if (Test-Path $providerPath) {
        Write-Host "`n=== Processing $provider ===" -ForegroundColor Cyan

        # Process Outbox store
        $outboxPath = Join-Path $providerPath "Outbox\OutboxStoreDapper.cs"
        if (Test-Path $outboxPath) {
            Add-GuardClauses $outboxPath
        }

        # Process Inbox store
        $inboxPath = Join-Path $providerPath "Inbox\InboxStoreDapper.cs"
        if (Test-Path $inboxPath) {
            Add-GuardClauses $inboxPath
        }

        # Process Saga store (Dapper only)
        if ($provider -like "*Dapper*") {
            $sagaPath = Join-Path $providerPath "Sagas\SagaStoreDapper.cs"
            if (Test-Path $sagaPath) {
                Add-GuardClauses $sagaPath
            }

            # Process Scheduling store (Dapper only)
            $schedulingPath = Join-Path $providerPath "Scheduling\ScheduledMessageStoreDapper.cs"
            if (Test-Path $schedulingPath) {
                Add-GuardClauses $schedulingPath
            }
        }

        # Process ADO stores (different class names)
        if ($provider -like "*ADO*") {
            $outboxADOPath = Join-Path $providerPath "Outbox\OutboxStoreADO.cs"
            if (Test-Path $outboxADOPath) {
                Add-GuardClauses $outboxADOPath
            }

            $inboxADOPath = Join-Path $providerPath "Inbox\InboxStoreADO.cs"
            if (Test-Path $inboxADOPath) {
                Add-GuardClauses $inboxADOPath
            }
        }
    } else {
        Write-Host "$provider not found at $providerPath" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Guard clauses update completed! ===" -ForegroundColor Green
