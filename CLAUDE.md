# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is Chores

Keeps track of household chores — who did what, when. This repo is the REST API; the React SPA lives in its own repo, `../choresweb`. The structure follows Mynt (`../Mynt`), except that the frontend is a separate project.

Current state: a foundation only — users, registration, login. The domain (households, chores, log) is still to be built.

## Commands

```bash
dotnet run                        # Start the API (http://localhost:5070, Swagger at /swagger)
dotnet build                      # Build
dotnet ef migrations add <name>                             # Add a new EF migration
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update # Apply pending migrations (env required to pick up appsettings.Development.json)
```

Run choresweb's `npm run dev` alongside; its Vite server proxies `/api` to port 5070.

## Architecture

**ASP.NET Core 9 minimal APIs** — no controllers. Endpoints are static extension methods in `Endpoints/`, registered in `Program.cs` via `app.Map*Endpoints()`.

- Endpoints under `/api/*` require JWT Bearer auth, except `/api/auth/login` and `/api/auth/register` (both rate-limited per client IP). The user ID is read from `ClaimTypes.NameIdentifier`.
- Registration logs the user straight in — there is no email verification yet.
- `ApplicationDbContext` (EF Core, PostgreSQL via Npgsql) is the single database context; relationships and indexes go in `OnModelCreating`. Migrations are applied automatically on startup (`db.Database.Migrate()`).
- DTOs live in `Models/DTOs/<domain>/`.
- No CORS: in development the Vite proxy makes requests same-origin, and in production Caddy serves both projects from one host.
- `/health` is the health check.

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
