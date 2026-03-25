# GitHub Workflows Monitoring Scripts

These scripts help you monitor and troubleshoot GitHub Actions workflows directly from the command line, without needing to manually check the GitHub UI.

## Prerequisites

You need a GitHub Personal Access Token with `actions:read` or `repo` scope.

**Get a token:**

1. Go to <https://github.com/settings/tokens>
2. Click "Generate new token (classic)"
3. Select scopes: `repo` or `actions:read`
4. Copy the token

**Set the token:**

```bash
# Windows (PowerShell)
$env:GITHUB_TOKEN = "your_token_here"

# Linux/macOS
export GITHUB_TOKEN="your_token_here"

# Or pass via --token argument
```

## Scripts

### 1. `check-latest-workflow.cs` - Check Latest Workflow Status

Quickly check the status of the most recent workflow run(s).

**Usage:**

```bash
# Check latest workflow
dotnet run --file .github/scripts/check-latest-workflow.cs

# Check latest workflow (with explicit token)
dotnet run --file .github/scripts/check-latest-workflow.cs -- --token YOUR_TOKEN

# Check specific workflow by name
dotnet run --file .github/scripts/check-latest-workflow.cs -- --workflow "SonarCloud"
dotnet run --file .github/scripts/check-latest-workflow.cs -- --workflow "dotnet-ci"
```

**Output:**

```
✅ .NET Quality Gate
   Status: completed → success
   Branch: main
   Commit: d6a3773
   Started: 2025-12-18 13:45:22
   URL: https://github.com/dlrivada/Encina/actions/runs/12345

❌ SonarCloud Analysis
   Status: completed → failure
   Branch: main
   Commit: feeea36
   Started: 2025-12-18 14:10:15
   URL: https://github.com/dlrivada/Encina/actions/runs/12346

   📋 Failure Details:

   ❌ Job: SonarCloud Code Quality
      → Failed Step: SonarCloud Scan
```

### 2. `monitor-workflows.cs` - Continuous Monitoring

Continuously monitors GitHub Actions and automatically reports failures as they happen.

**Usage:**

```bash
# Monitor with default 60-second interval
dotnet run --file .github/scripts/monitor-workflows.cs

# Monitor with custom interval (in seconds)
dotnet run --file .github/scripts/monitor-workflows.cs -- --interval 30

# Monitor specific repository
dotnet run --file .github/scripts/monitor-workflows.cs -- --repo "owner/repo" --interval 60
```

**Output:**

```
Monitoring GitHub Actions for dlrivada/Encina every 60 seconds...
Press Ctrl+C to stop.

❌ WORKFLOW FAILED: SonarCloud Analysis
   Run ID: 12346789
   Branch: main
   Commit: feeea36
   Started: 2025-12-18T14:10:15Z
   URL: https://github.com/dlrivada/Encina/actions/runs/12346789

📋 FAILURE LOGS:
Job: SonarCloud Code Quality → Step: SonarCloud Scan

─────────────────────────────────────────────────────────────
```

## Integration with Claude Code

You can configure Claude Code to automatically run these scripts when workflows fail.

**Example workflow:**

1. User pushes code
2. GitHub Actions runs
3. If workflow fails, user tells Claude: "check the latest workflow"
4. Claude runs: `dotnet run --file .github/scripts/check-latest-workflow.cs`
5. Claude sees the failure details and fixes the issue automatically

**Or use monitoring mode:**

```bash
# In a separate terminal
dotnet run --file .github/scripts/monitor-workflows.cs
```

Claude can read the output and react to failures immediately.

## Tips

- **Store token in environment variable** to avoid passing it every time
- **Use `--workflow` filter** to focus on specific workflows
- **Run monitor in background** during development sessions
- **Check exit codes**: Scripts return 0 on success, 1 on failure

## Security Notes

- Never commit your `GITHUB_TOKEN` to the repository
- Keep tokens in environment variables or secure vaults
- Use tokens with minimal required scopes (`actions:read` is sufficient)
- Rotate tokens regularly

## Example Session

```bash
# 1. Set token once
export GITHUB_TOKEN="ghp_xxxxxxxxxxxxx"

# 2. Quick check before starting work
dotnet run --file .github/scripts/check-latest-workflow.cs

# 3. Start monitoring in background (optional)
dotnet run --file .github/scripts/monitor-workflows.cs &

# 4. Work on code...

# 5. Push changes and check status
git push
sleep 60
dotnet run --file .github/scripts/check-latest-workflow.cs
```

## Troubleshooting

**Error: "GITHUB_TOKEN required"**

- Set the environment variable or pass `--token` argument

**Error: "403 Forbidden"**

- Token doesn't have required scope (needs `actions:read` or `repo`)
- Token might be expired

**No workflows found**

- Check repository name is correct: `--repo owner/repo`
- Make sure workflows have run at least once

**Rate limiting**

- GitHub API allows 5000 requests/hour for authenticated users
- Monitor script with 60s interval = 60 requests/hour (safe)
