# Security Policy

System Guardian is security-sensitive because it reads Windows health information and can open repair tools with administrator permission.

## Reporting A Vulnerability

Do not publish working exploits or sensitive device information in a public issue. Contact the repository owner privately with the affected version, reproduction steps, impact, and any suggested mitigation.

## Security Requirements

- Native repairs must use an explicit action whitelist.
- Remote content must never provide shell commands for direct execution.
- Reports should remain local unless the user deliberately enables upload or sync.
- Secrets, tokens, reports, and backend data must not be committed.
- New elevated actions require clear UI disclosure and user confirmation.
