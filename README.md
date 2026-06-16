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

### Backend (API)

```pwsh
cd api
dotnet restore
dotnet ef database update --project Dahd.Infrastructure --startup-project Dahd.Api
dotnet run --project Dahd.Api
```

Swagger UI: `https://localhost:7080/swagger`

### Frontend (Angular)

```pwsh
cd web
npm install
npm start
```

App: `http://localhost:4200`

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
- [ ] EF Core migrations
- [ ] Seed data for 8 ASCAD + FMD + Brucella vaccines
- [ ] Auth (JWT + role-based)
- [ ] Real INAPH animal-ID sync (Phase 2)
- [ ] IoT temperature ingest (Phase 2)

## Source research

All claims in this repo are traceable to primary sources verified in deep-research passes (3-vote adversarial verification). Refuted claims and open questions are tracked in [docs/05-open-questions.md](docs/05-open-questions.md). Do **not** quote any number from this repo in a pitch deck without cross-checking that document.
