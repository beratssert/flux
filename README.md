# flux-time-tracker
Flux is a dual-platform (Web &amp; Mobile) productivity suite designed to track work hours, manage project-related expenses, and generate real-time analytics. Built with a .NET backend to support complex data synchronization between standard users and managers.

## Getting Started

### Backend Setup

1. Copy the example settings file to create your local development config:
   ```bash
   cp backend/CleanArchitecture/CleanArchitecture.WebApi/appsettings.Development.example.json \
      backend/CleanArchitecture/CleanArchitecture.WebApi/appsettings.Development.json
   ```
2. Edit `appsettings.Development.json` with your local SMTP credentials and other settings. This file is listed in `.gitignore` and will **not** be committed.
3. Set the `JWTSettings:Key` in your local `appsettings.Development.json` to a strong secret string (minimum 32 characters).

### Branch Workflow

When switching between branches, ensure you have no uncommitted changes to local-only config files (e.g. `appsettings.Development.json`). Use `git stash` or commit your changes before switching:

```bash
git switch <branch-name>
```

> **Note:** If you see a conflict on `appsettings.Development.json` when switching branches, it means your local copy has changes. Stash them with `git stash`, switch branches, then restore with `git stash pop`.
