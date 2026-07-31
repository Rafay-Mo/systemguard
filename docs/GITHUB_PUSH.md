# Push System Guardian To GitHub

This folder is ready to become your GitHub repo.

## Option 1: Use The Script

Create an empty GitHub repo, copy its HTTPS URL, then run:

```powershell
cd path\to\systemguard
.\scripts\push-to-github.ps1 -RemoteUrl "https://github.com/YOUR-USERNAME/YOUR-REPO.git"
```

The script will:

- initialize Git if needed
- add the app files
- create a commit
- set the branch to `main`
- connect the GitHub remote
- push the repo

## Option 2: Manual Commands

```powershell
cd path\to\systemguard
git init
git add .
git commit -m "Initial System Guardian app"
git branch -M main
git remote add origin https://github.com/YOUR-USERNAME/YOUR-REPO.git
git push -u origin main
```

## What To Upload As A GitHub Release

Use this zip for the downloadable Windows app:

```text
Release\SystemGuardian-v1.3.0.zip
```

Users can download it, unzip it, and run:

```text
Start-SystemGuardian.cmd
```
