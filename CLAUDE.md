# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Chores

Keeps track of household chores — who did what, when. This repo is the REST API; the React SPA lives in its own repo, `../choresweb`. The structure follows Mynt (`../Mynt`), except that the frontend is a separate project.

**The two repos are one project.** A change to an endpoint or DTO here usually needs the matching change in `../choresweb/src/api/` (and the reverse), so read and edit both without being asked.

It replaces a paper sheet on the fridge: chores down the side, Monday–Sunday across, and each member writes their initial in the day they take something on. Some chores have a weekly quota per person ("Köket ×3", "Laga mat ×1"), others are done when needed. Everyone plans their own week.

## Commands

```bash
dotnet run                        # Start the API (http://localhost:5072, Swagger at /swagger)
dotnet build                      # Build
dotnet ef migrations add <name>                             # Add a new EF migration
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update # Apply pending migrations (env required to pick up appsettings.Development.json)
```

Run choresweb's `npm run dev` alongside; its Vite server proxies `/api` to port 5072.

## Architecture

**ASP.NET Core 9 minimal APIs** — no controllers. Endpoints are static extension methods in `Endpoints/`, registered in `Program.cs` via `app.Map*Endpoints()`.

- Endpoints under `/api/*` require JWT Bearer auth, except `/api/auth/login` and `/api/auth/register` (both rate-limited per client IP). The user ID is read from `ClaimTypes.NameIdentifier`.
- Registration logs the user straight in — there is no email verification yet.
- `ApplicationDbContext` (EF Core, PostgreSQL via Npgsql) is the single database context; relationships and indexes go in `OnModelCreating`. Migrations are applied automatically on startup (`db.Database.Migrate()`).
- DTOs live in `Models/DTOs/<domain>/`.
- No CORS: in development the Vite proxy makes requests same-origin, and in production Caddy serves both projects from one host.
- `/health` is the health check.
- `Endpoints/CurrentUser` resolves the caller and their household; every household-scoped query filters on that, so another household's IDs come back as 404.

**Domain model:**
- `User` → own login; `Name` is required because its first letter marks their entries. `PUT /api/users/me` changes name and email (an email change needs the current password, as the email is the login); `PUT /api/users/me/password` changes the password. Belongs to at most one `Household` (`HouseholdId` null until they create or join one).
- `Household` → joined with its `InviteCode` (8 chars, no 0/O/1/I). `POST /api/household/join` is rate-limited per IP so codes can't be guessed.
- `Chore` → belongs to the household; any member may edit the list. `WeeklyTarget` is per member per week, null for chores done when needed. Deleting a chore deletes its entries from past weeks too.
- `ChoreEntry` → one member taking a chore on a `DateOnly`. Planned until `CompletedAt` is set. Several per member and day are allowed. Members see the whole household's entries (`GET /api/entries?weekStart=YYYY-MM-DD`) but create, tick off and delete only their own.

**JWT config** is under `Jwt:Key/Issuer/Audience`. The key is Base64-encoded and only set in `appsettings.Development.json` (gitignored) or the server env file.

## Local development

The dev database `chores_dev` runs on z390. Off the home network, connect to Tailscale — `appsettings.Development.json` uses `z390.tail0f08a3.ts.net` as host.

## Deployment

Deployed to antec (10.0.20.10) by GitHub Actions on a self-hosted runner (`.github/workflows/deploy.yml` → `deploy.sh` → Docker Compose). Container `choresapi`, published on `127.0.0.1:5104`, joined to the external `db-network` to reach Postgres. Environment variables live in `/home/deploy/.env.choresapi` on the server:

```
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chores;Username=chores;Password=...
Jwt__Key=<base64, e.g. openssl rand -base64 64>
```

Served at `chores.forstberg.net`. Caddy routes `/api/*` and `/health` here and everything else to choresweb on `127.0.0.1:5105`:

```caddyfile
http://chores.forstberg.net {
	import logga chores
	handle /api/* {
		reverse_proxy localhost:5104
	}
	handle /health {
		reverse_proxy localhost:5104
	}
	handle {
		reverse_proxy localhost:5105
	}
}
```

Server setup (database, env file, runner, DNS) follows `../infra/self-hosted-runner-setup.md`.
