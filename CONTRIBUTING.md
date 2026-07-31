# Contributing

Keep changes focused and preserve the local-first safety boundary.

Before opening a pull request:

1. Read `docs/ARCHITECTURE.md` and `docs/DEVELOPMENT.md`.
2. Make source changes under `src/`, `backend/`, or `tests/`.
3. Run `scripts/verify.ps1`.
4. Rebuild `app/` when desktop source or UI source changes.
5. Explain any new system permission, command, or repair action in the pull request.

Do not add registry cleaners, unknown debloat scripts, third-party driver updaters, security-disabling actions, arbitrary command execution, or silent repairs.
