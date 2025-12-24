# Security Policy

## Supported Versions

Encina is currently in pre-1.0 development. Security updates will be applied to the latest version on the `main` branch.

| Version | Supported          |
| ------- | ------------------ |
| main    | :white_check_mark: |
| < 1.0   | :x: (pre-release)  |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue, please report it responsibly.

### Preferred Method: Private Vulnerability Reporting

1. Go to the [Security tab](https://github.com/dlrivada/Encina/security)
2. Click **"Report a vulnerability"**
3. Fill out the form with details about the vulnerability

This method keeps the report private until a fix is available.

### Alternative: Email

If you prefer email, contact the maintainer directly. Please include:

- Description of the vulnerability
- Steps to reproduce
- Potential impact
- Suggested fix (if any)

### What to Expect

- **Acknowledgment**: Within 48 hours
- **Initial assessment**: Within 7 days
- **Resolution timeline**: Depends on severity (critical issues prioritized)

### Scope

The following are in scope for security reports:

- Encina core library and all satellite packages
- Build and CI/CD pipeline security
- Dependencies with known vulnerabilities

### Out of Scope

- Social engineering attacks
- Denial of service attacks
- Issues in dependencies (report to upstream maintainers)

## Security Measures

Encina implements the following security practices:

- **Dependabot**: Automated dependency updates
- **CodeQL**: Static analysis for common vulnerabilities
- **Secret scanning**: Prevents accidental credential exposure
- **SBOM generation**: Software Bill of Materials for supply chain transparency

## Acknowledgments

We appreciate responsible disclosure and will acknowledge security researchers who report valid vulnerabilities (with permission).
