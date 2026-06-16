# DAHD — UP Department of Animal Husbandry Pharmacy MIS

Reference solution scaffold for a **veterinary pharmacy / drug / asset supply-chain MIS** targeted at the Uttar Pradesh Department of Animal Husbandry (Pashudhan Vibhag).

The research that anchors this build — what UP AHD actually procures, the structural gap that no UPMSCL-equivalent exists on the veterinary side, and the three quick-win modules — is in **[docs/](docs/00-index.md)**.

## Structure

```
dahd/
├─ docs/                    Research, findings, opportunity map, solution architecture
├─ api/                     .NET 9 Web API (clean architecture)
│   ├─ Dahd.Api/
│   ├─ Dahd.Application/
│   ├─ Dahd.Domain/
│   └─ Dahd.Infrastructure/
└─ web/                     Angular 19 standalone frontend
```

## Quick start

> **Run the API first, then the frontend.** Open two terminals.

### Terminal 1 — Backend (API)

```pwsh
cd api
dotnet restore
dotnet run --project Dahd.Api --launch-profile http
```

- API: `http://localhost:5070`
- Swagger UI: `http://localhost:5070/swagger`
- The database is auto-created and seeded on first run via `MigrateAsync` + `DahdSeeder`. No manual `dotnet ef database update` needed.
- Default connection string targets **SQL Server LocalDB** (`(localdb)\MSSQLLocalDB`). Comes with Visual Studio; if you don't have it, install SQL Server Express LocalDB or change the connection string in `api/Dahd.Api/appsettings.json`.

### Terminal 2 — Frontend (Angular)

```pwsh
cd web
npm install
npm start
```

App: `http://localhost:4200`

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Sidebar renders, dashboard says **"Failed to reach API"** | API is not running, or running on a different port | Start the API in Terminal 1 with `--launch-profile http`. Open `http://localhost:5070/swagger` to confirm it's up. |
| API crashes on startup with a SQL connection error | LocalDB not installed | Install SQL Server Express LocalDB, or change `ConnectionStrings:Default` in `appsettings.json` to a SQL Server instance you have. |
| Browser blocks the request with a cert / mixed-content error | You ran the API on HTTPS (`--launch-profile https`) but the frontend targets HTTP | Either use `--launch-profile http` (recommended for dev), or run `dotnet dev-certs https --trust` and change `web/src/environments/environment.ts` `apiUrl` to `https://localhost:7277/api`. |
| CORS error in console | You changed the Angular port | Update the allowed origin in `api/Dahd.Api/Program.cs` (search for `WithOrigins("http://localhost:4200")`). |

## Phase-1 modules (in this scaffold)

1. **Drug & Vaccine Master** — formulary including FMD, Brucella S19, and the 8 ASCAD vaccines
2. **Warehouse / Store** — Central / Divisional / District / Facility tiers (5 zones from the 1962 ICCC structure)
3. **Indent flow** — central → division → district → facility
4. **Cold-chain batch / expiry tracking** — anchored in NADCP cold-chain mandate
5. **MVU dispensing** — supports the 520-MVU fleet under the 1962 helpline
6. **Dashboard** — stock, expiry, dispense, indent KPIs

## Why this product

UP runs a mature human-health supply chain via **UPMSCL + DVDMS + EMMS**. No equivalent exists on the veterinary side. **Bharat Pashudhan (NDLM)** covers animal-ID and breeding registration, but pharmacy / inventory / cold-chain / MVU dispensing is explicitly out of scope. This scaffold is the smallest demoable version of the gap-filler.

For the full pitch context, read **[docs/04-vendor-opportunity-map.md](docs/04-vendor-opportunity-map.md)**.

## Status

- [x] Research docs committed
- [x] .NET 9 API scaffold
- [x] Angular 19 frontend scaffold
- [x] EF Core initial migration
- [x] Seed data: FMD + Brucella S19 + 8 ASCAD vaccines + 10 core vet drugs, central + 5 divisional warehouses, sample MVUs and a gaushala
- [ ] Auth (JWT + role-based)
- [ ] Real INAPH animal-ID sync (Phase 2)
- [ ] IoT temperature ingest (Phase 2)

## Source research

All claims in this repo are traceable to primary sources verified in deep-research passes (3-vote adversarial verification). Refuted claims and open questions are tracked in [docs/05-open-questions.md](docs/05-open-questions.md). Do **not** quote any number from this repo in a pitch deck without cross-checking that document.
