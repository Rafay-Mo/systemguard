# System Guardian Backend

Optional local service for future account, report-sync, and repair-plan work. The current desktop release does not require or connect to this service.

```bat
.\backend\Start-Backend.cmd
```

Default URL:

```text
http://127.0.0.1:4739
```

## What It Includes

- User registration and login
- Signed auth tokens
- Device registration
- Optional scan report upload
- Repair plan endpoint
- Safe action whitelist
- App update endpoint
- Crash report endpoint
- Local JSON database in `backend\data\db.json`

Set `SYSTEM_GUARDIAN_SECRET` to a long random value before using signed auth tokens outside local development. If it is not set, the server uses an ephemeral development secret that changes on restart.

## Important Safety Boundary

The backend does not run commands on the user's PC. It returns approved action IDs only. The Windows app must map those IDs to built-in safe local actions and ask the user before running anything.

## Useful Endpoints

```text
GET  /health
POST /auth/register
POST /auth/login
GET  /me
POST /devices/register
GET  /devices
POST /reports
GET  /reports/:id
POST /reports/analyze
GET  /repair-guidance
GET  /app/update
POST /crash-report
```
