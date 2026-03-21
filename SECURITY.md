# Security Policy

## Project Status

Encina is currently in **pre-1.0 development**. There are no stable releases yet. The API surface is subject to breaking changes between versions.

## Supported Versions

| Version | Supported |
| ------- | --------- |
| main (pre-1.0) | Best-effort security fixes |
| 1.x (future) | Will receive full security support once released |

## Reporting a Vulnerability

**Do not report security vulnerabilities through public GitHub issues.**

Use [GitHub Private Vulnerability Reporting](https://github.com/dlrivada/Encina/security/advisories/new) to submit your report. This is the only supported channel for security disclosures.

### What to Include

- A clear description of the vulnerability
- Steps to reproduce the issue (minimal code or configuration)
- Which package(s) and version(s) are affected
- The potential impact if exploited
- Suggested fix (if any)

## Response Process

Encina is maintained by a single developer in spare time. I will do my best to:

1. Acknowledge receipt of your report
2. Investigate and confirm the vulnerability
3. Develop and release a fix
4. Credit the reporter in the fix commit (unless anonymity is requested)

I cannot commit to specific response timelines at this stage. If you have not received a response after a reasonable period, you may follow up through the same private reporting channel.

## Disclosure Policy

This project follows **coordinated disclosure**:

- Vulnerabilities are reported privately first
- Fixes are developed before public disclosure
- Once a fix is released, the vulnerability is disclosed through a [GitHub Security Advisory](https://github.com/dlrivada/Encina/security/advisories)

I ask that reporters allow reasonable time to address the issue before any public disclosure.

## Scope

### In Scope

- Encina core library and all satellite packages
- Build and CI/CD pipeline security
- Dependencies with known vulnerabilities

### Out of Scope

- Social engineering attacks
- Denial of service attacks
- Issues in third-party dependencies (report to upstream maintainers)

## Security Measures

Encina implements the following security practices:

- **Dependabot**: Automated dependency updates
- **CodeQL**: Static analysis for common vulnerabilities
- **Secret scanning**: Prevents accidental credential exposure
- **SBOM generation**: Software Bill of Materials for supply chain transparency
- **Branch protection**: Required reviews, status checks, and linear history on `main`

## Receiving Security Updates

- **Watch** this repository for new releases
- Check [GitHub Security Advisories](https://github.com/dlrivada/Encina/security/advisories) for known vulnerabilities
- Enable [Dependabot alerts](https://docs.github.com/en/code-security/dependabot/dependabot-alerts) on your forks

## Acknowledgments

We appreciate responsible disclosure and will acknowledge security researchers who report valid vulnerabilities (with permission).
