# MCP Servers Configuration Guide

This guide documents the Model Context Protocol (MCP) servers configured for Encina development, enabling AI-assisted code analysis, error monitoring, and infrastructure management.

## Overview

MCP servers extend Claude's capabilities by providing direct access to external tools and services.

| MCP Server | Purpose | Status |
|------------|---------|--------|
| **SonarQube** | Code quality analysis (SonarCloud) | Configured |
| **Sentry** | Error monitoring and analysis | Configured |
| **Kubernetes** | Cluster management | Configured |
| **Docker** | Container management | Built-in |

## Prerequisites

- **Docker Desktop** with MCP Toolkit enabled
- Active accounts for each service (SonarCloud, Sentry)
- API tokens/credentials for authentication

## SonarQube / SonarCloud

Provides code quality and security analysis directly from SonarCloud.

### Configuration

```bash
# Find the server
docker mcp gateway  # Then use mcp-find in Claude

# Configure organization (for SonarCloud)
# Use mcp-config-set with: {"org": "your-org-name"}

# Add the server
# Use mcp-add with name: sonarqube
```

### Required Secrets

| Secret | Description | How to Obtain |
|--------|-------------|---------------|
| `sonarqube.token` | API authentication token | [SonarCloud Security](https://sonarcloud.io/account/security) |

### Configuration Options

| Option | Required | Description |
|--------|----------|-------------|
| `org` | Yes (Cloud) | Organization key for SonarCloud |
| `url` | No | URL for SonarQube Server (not needed for Cloud) |

### Available Tools (17 tools)

| Tool | Description |
|------|-------------|
| `search_my_sonarqube_projects` | Find projects in your organization |
| `search_sonar_issues_in_projects` | Search for code quality issues |
| `get_project_quality_gate_status` | Check quality gate pass/fail |
| `analyze_code_snippet` | Analyze code for issues |
| `show_rule` | Get details about a specific rule |
| `get_component_measures` | Get metrics (coverage, complexity, etc.) |
| `change_sonar_issue_status` | Accept, false-positive, or reopen issues |
| `list_quality_gates` | List all quality gates |
| `list_languages` | List supported languages |
| `list_rule_repositories` | List rule repositories |
| `get_scm_info` | Get SCM information for files |
| `get_raw_source` | Get source code from SonarQube |
| `search_metrics` | Search available metrics |
| `create_webhook` | Create webhooks for notifications |
| `list_webhooks` | List configured webhooks |
| `list_enterprises` | List available enterprises |
| `list_portfolios` | List enterprise portfolios |

### Usage Examples

```
# Check project quality gate
"Check the quality gate status for project dlrivada_Encina"

# Find issues
"Search for HIGH severity issues in the Encina project"

# Analyze code
"Analyze this code snippet for issues: [paste code]"

# Get metrics
"Get the code coverage and complexity metrics for Encina"
```

## Sentry

Provides access to error monitoring and crash analysis from Sentry.io.

### Configuration

```bash
# Add the server
# Use mcp-add with name: sentry
```

### Required Secrets

| Secret | Description | How to Obtain |
|--------|-------------|---------------|
| `sentry.auth_token` | API authentication token | [Sentry Auth Tokens](https://sentry.io/settings/account/api/auth-tokens/) |

### Available Tools (1 tool)

| Tool | Description |
|------|-------------|
| `get_sentry_issue` | Retrieve and analyze a Sentry issue by ID or URL |

### Usage Examples

```
# Analyze an issue by URL
"Analyze this Sentry issue: https://sentry.io/organizations/myorg/issues/12345/"

# Analyze by ID
"Get details for Sentry issue 12345"
```

### Token Permissions

When creating your Sentry auth token, ensure these scopes are enabled:
- `project:read`
- `event:read`
- `issue:read`

## Kubernetes

Provides direct access to Kubernetes cluster management.

### Configuration

```bash
# Configure kubeconfig path
# Use mcp-config-set with: {"config_path": "C:/Users/username/.kube/config"}

# Add the server
# Use mcp-add with name: kubernetes
```

### Required Configuration

| Option | Description |
|--------|-------------|
| `config_path` | Path to your `.kube/config` file |

### Prerequisites

1. **Docker Desktop Kubernetes** (recommended for local development):
   - Open Docker Desktop
   - Go to Settings > Kubernetes
   - Click "Enable Kubernetes"
   - Wait for cluster to start

2. **Cloud Kubernetes** (AKS, EKS, GKE):
   - Configure `kubectl` to connect to your cluster
   - Ensure `.kube/config` has valid credentials

### Available Tools

| Tool | Description |
|------|-------------|
| `get_pods` | List pods in namespace |
| `get_deployments` | List deployments |
| `get_services` | List services |
| `get_namespaces` | List namespaces |
| `describe_pod` | Get pod details |
| `get_logs` | Get container logs |
| `apply_manifest` | Apply Kubernetes manifests |
| `delete_resource` | Delete resources |

### Usage Examples

```
# List pods
"Show all pods in the default namespace"

# Get logs
"Get logs from the api-server pod"

# Check deployment status
"Describe the web-app deployment"
```

## Docker (Built-in)

The Docker MCP server is automatically available through Docker Desktop's MCP Toolkit.

### Available Tools

| Tool | Description |
|------|-------------|
| `docker` | Execute Docker CLI commands |
| `mcp-find` | Search MCP server catalog |
| `mcp-add` | Add MCP server to session |
| `mcp-config-set` | Configure MCP server |
| `mcp-remove` | Remove MCP server |

### Usage Examples

```
# List containers
"List all running Docker containers"

# Check images
"Show Docker images"

# View logs
"Get logs from the postgres container"
```

## Troubleshooting

### MCP Server Won't Start

1. **Check Docker Desktop is running**
   ```bash
   docker --version
   ```

2. **Verify MCP Gateway**
   ```bash
   docker mcp gateway run
   ```

3. **Check secrets are configured**
   - Secrets are prompted when adding a server
   - Re-add the server to re-enter secrets

### SonarCloud Connection Issues

1. **Verify token permissions**
   - Token needs `Execute Analysis` permission
   - Check token hasn't expired

2. **Verify organization name**
   - Must match exactly (case-sensitive)
   - Find it at: `https://sonarcloud.io/organizations/YOUR_ORG`

### Sentry Connection Issues

1. **Verify auth token scopes**
   - Needs `project:read`, `event:read`, `issue:read`

2. **Check organization access**
   - Token must have access to the organization containing the issues

### Kubernetes Connection Issues

1. **Verify kubeconfig path**
   ```bash
   kubectl config view
   ```

2. **Check cluster connectivity**
   ```bash
   kubectl cluster-info
   ```

3. **For Docker Desktop Kubernetes**
   - Ensure Kubernetes is enabled and running (green icon)
   - Restart Docker Desktop if issues persist

## Adding New MCP Servers

To discover and add new MCP servers:

```
# Search the catalog
"Find MCP servers for [capability]"

# This uses mcp-find internally

# Add a found server
"Add the [server-name] MCP server"

# This uses mcp-add internally
```

## Security Best Practices

1. **Token Management**
   - Use tokens with minimal required permissions
   - Rotate tokens periodically
   - Never commit tokens to version control

2. **Kubernetes Access**
   - Use read-only access for analysis
   - Avoid admin-level kubeconfig in development

3. **SonarCloud**
   - Use project-scoped tokens when possible
   - Avoid tokens with `Administer` permission unless needed

## Related Documentation

- [Docker Infrastructure Guide](docker-infrastructure.md)
- [Integration Testing](../testing/integration-tests.md)
- [Docker MCP Toolkit](https://docs.docker.com/desktop/features/mcp-toolkit/)
