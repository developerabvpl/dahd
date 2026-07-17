# DAHD — Hosting on a server (SQLite)

The app uses **SQLite** by default: the entire database is a single file,
`api/Dahd.Api/dahd.db`. To host on a server you deploy the published API + the
built frontend, and ship your database file alongside the API.

---

## Quick path — one command (single-process bundle)

From the repo root:

```pwsh
.\publish.ps1
```

This produces a self-contained folder `dist-server\` that **one Kestrel process
serves entirely** — the API and the Angular UI on the same port, no reverse
proxy or CORS needed. It:

1. `dotnet publish`es the API,
2. builds the frontend in production mode (`apiUrl: '/api'`) into `wwwroot\`,
3. ships a consistent snapshot of your **current** `dahd.db` (via `VACUUM INTO`),
4. writes `appsettings.Production.json` with a **freshly generated** JWT key.

Copy `dist-server\` to the server and run:

```pwsh
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=http://0.0.0.0:8080
dotnet Dahd.Api.dll
```

Open `http://<server>:8080` (login `admin` / `admin123` — change it). Put HTTPS
(Nginx/IIS/Caddy) in front for production.

Variants: `.\publish.ps1 -FreshDb` (ship an empty DB that auto-seeds on first
run) · `.\publish.ps1 -Out D:\deploy` (output elsewhere). See
`dist-server\README-DEPLOY.txt` in the bundle for the same run notes.

The manual, component-by-component steps below are for custom setups (separate
static host + reverse proxy, IIS, systemd, SQL Server, etc.).

---

## 1. Back up the database (preserve current data)

From the repo root:

```pwsh
.\backup-db.ps1
```

This writes a consistent snapshot (via SQLite `VACUUM INTO`, safe while the app
runs — no torn writes) to `api\Dahd.Api\backups\dahd-backup-<timestamp>.db`.

Or a specific path: `.\backup-db.ps1 -Dest D:\deploy\dahd.db`

> The backup is a normal SQLite file. **Restoring = copying it into place** and
> naming it `dahd.db` next to the API. There is no separate "restore" step.

Schedule it (Task Scheduler, nightly):
`powershell -File E:\WSLProjects\dahd\backup-db.ps1`

---

## 2. Publish the API

```pwsh
cd api
dotnet publish Dahd.Api -c Release -o publish
```

`api\publish\` now holds a self-contained web app (Kestrel).

**Server config** — set these on the server (via `appsettings.Production.json`
or environment variables), do **not** ship dev secrets:

```json
{
  "Database": { "Provider": "Sqlite" },
  "ConnectionStrings": { "Sqlite": "Data Source=dahd.db" },
  "Jwt": {
    "Issuer": "DahdApi",
    "Audience": "DahdClient",
    "Key": "<generate a NEW 32+ char random secret for production>",
    "AccessTokenMinutes": 60,
    "RefreshTokenDays": 14
  }
}
```

Env-var form (Linux/systemd/containers):
`Database__Provider=Sqlite`, `ConnectionStrings__Sqlite=Data Source=/var/dahd/dahd.db`,
`Jwt__Key=<secret>`.

**Put the DB file** where the connection string points (an absolute path on a
persistent disk is best for a server, e.g. `/var/dahd/dahd.db` or
`D:\dahd\dahd.db`) and copy your backup there as `dahd.db`.

> On first run against an **empty/missing** file the app auto-creates and seeds
> a fresh DB. To keep YOUR data, put the backup file in place **before** first
> run.

---

## 3. Build + serve the frontend

Point the frontend at the server's API URL first:

`web/src/environments/environment.ts` → `apiUrl: 'https://<your-server>/api'`

Then:

```pwsh
cd web
npm ci
npm run build   # -> web/dist/web/browser
```

Serve `web/dist/web/browser` as static files. Two common setups:

- **Reverse proxy (recommended):** Nginx/IIS/Caddy serves the static frontend
  and proxies `/api` to the Kestrel process. CORS then isn't needed
  (same origin). If you serve the frontend on a different origin, add it to the
  CORS allow-list in `api/Dahd.Api/Program.cs` (`WithOrigins(...)`).
- **Kestrel serves both:** add `app.UseStaticFiles()` + a SPA fallback in the
  API and drop the built frontend into `wwwroot`.

---

## 4. Run it

- **Windows (IIS):** host the published API behind IIS with the ASP.NET Core
  Hosting Bundle; app pool identity needs read/write on the `dahd.db` folder.
- **Windows service:** `sc create DahdApi binPath= "...\publish\Dahd.Api.exe"`.
- **Linux (systemd):** a unit running `dotnet Dahd.Api.dll` with
  `ASPNETCORE_URLS=http://localhost:5070` behind Nginx.

Health check: `GET /swagger` (dev) or any `POST /api/auth/login`.

---

## SQLite on a server — know the limits

SQLite is excellent for a **single-node, low-to-moderate concurrency** pilot:
one API process, one file. It is **not** for multi-server/high-write-concurrency
(no networked DB, one writer at a time). For that scale, switch the provider:

- **SQL Server:** set `Database:Provider = "SqlServer"` +
  `ConnectionStrings:Default`. Migrations already exist; the app runs
  `Migrate()` on startup.
- **PostgreSQL:** add the `Npgsql.EntityFrameworkCore.PostgreSQL` provider and a
  Postgres migration set (ask and I'll wire it).

For now, with SQLite: **backup = the file, restore = copy the file.** Keep
nightly backups (step 1) on a separate disk/offsite.

---

## Quick checklist

- [ ] `.\backup-db.ps1` → grab the latest `dahd-backup-*.db`
- [ ] `dotnet publish Dahd.Api -c Release -o publish`
- [ ] Server `appsettings.Production.json`: SQLite provider, absolute DB path, **new** JWT key
- [ ] Copy the backup to the server as `dahd.db` (before first run)
- [ ] Set `environment.ts` apiUrl → build frontend → serve static
- [ ] Reverse-proxy `/api` to Kestrel; HTTPS in front
- [ ] Schedule nightly `backup-db.ps1`
