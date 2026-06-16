# 06 — Reference Solution Architecture

> The .NET 9 API + Angular 19 scaffold in this repo is a **reference implementation** of the three-quick-win-module pitch from [03](03-pharmacy-supply-chain-mis.md) and [04](04-vendor-opportunity-map.md).

## Modules covered (Phase 1 scaffold)

| # | Module | API endpoints | Angular feature |
|---|---|---|---|
| 1 | **Drug & Vaccine Master** | `/api/drugs`, `/api/vaccines` | `/master/drugs`, `/master/vaccines` |
| 2 | **Warehouse / Store** | `/api/warehouses` | `/warehouse` |
| 3 | **Indent flow** (central → division → district → facility) | `/api/indents` | `/indent` |
| 4 | **Cold-chain batch/expiry tracking** | `/api/batches`, `/api/coldchain` | `/coldchain` |
| 5 | **MVU dispensing** | `/api/mvu`, `/api/dispense` | `/mvu` |
| 6 | **Dashboard** | `/api/dashboard` | `/dashboard` |

## The flow this models

```
Central Indent (AHD Directorate)
        │
        ▼
Warehouse GRN (Central CMS — Phase-2 build)
        │
        ▼
Divisional / District Transfer (5 zones from ICCC)
        │
        ▼
Facility Issue (Hospital / Dispensary / MVU / AI sub-centre / Gaushala)
        │
        ▼
Animal Dispensing (ear-tag / INAPH ID linked)
        │
        ▼
Reconciliation (consumption / expiry / wastage / audit)
```

## Tech stack

### Backend (`api/`)

- **.NET 9 Web API** with minimal/controller hybrid
- **EF Core 9** with SQL Server provider (configurable for PostgreSQL via DI swap)
- **Clean-architecture layering**: `Domain` → `Application` → `Infrastructure` → `Api`
- **Swagger / OpenAPI** for vendor demo
- JWT-ready auth scaffold (not enforced in Phase 1)
- Seed data for the 8 ASCAD vaccines + FMD + Brucella per [02](02-procurement-drugs-vaccines-assets.md)

### Frontend (`web/`)

- **Angular 19 standalone components** (no NgModule for new code)
- **Lazy-loaded routes** per module (matches CLAUDE.md guidance)
- **OnPush change detection** as default
- **takeUntil destroy$** subscription pattern
- **TrackBy** on all *ngFor
- Material-style baseline (clean for now; theming later)
- Mock backend toggle for offline demo

## Domain model (Phase 1)

```
Drug                       Warehouse
 ├ Id (Guid)                ├ Id (Guid)
 ├ Code (string)            ├ Name
 ├ Name                     ├ Type (Central/Divisional/District/Facility)
 ├ FormularyClass           ├ DivisionId
 ├ IsVaccine (bool)         ├ DistrictId
 ├ ColdChainRequired (bool) ├ ColdChainCapable (bool)
 ├ UnitOfMeasure            └ InchargeName
 └ ScheduleClass

Batch
 ├ Id (Guid)
 ├ DrugId
 ├ BatchNumber
 ├ ManufactureDate
 ├ ExpiryDate
 ├ ManufacturerName
 ├ Quantity
 ├ WarehouseId (current location)
 └ Status (InTransit/InStore/Issued/Expired/Wasted)

Indent
 ├ Id (Guid)
 ├ IndentNumber
 ├ RaisedByWarehouseId
 ├ FulfilledByWarehouseId
 ├ Status (Draft/Submitted/Approved/Issued/Received/Closed)
 ├ Lines: List<IndentLine>
 └ Timestamps

DispenseEvent (MVU / facility)
 ├ Id (Guid)
 ├ BatchId
 ├ Quantity
 ├ FacilityId (or MVU registration)
 ├ AnimalEarTag (INAPH-style)
 ├ AnimalSpecies
 ├ Owner / FarmerName
 ├ Diagnosis
 ├ VetUserId
 └ Timestamp
```

## What's NOT in Phase 1 (deliberate scope cut)

- No real INAPH / Bharat Pashudhan integration (animal ID validated locally; integration is a Phase-2 SoW item)
- No NDDB API integration
- No payment / billing flow
- No real cold-chain IoT integration — temperature is captured as manual reading; IoT ingest is Phase-2
- No production-grade auth / RBAC — JWT scaffold present, role enforcement is Phase-2
- No multi-tenant org partitioning — single-org for the demo

## Running locally

See repo root [README.md](../README.md).

## Future modules (Phase 2+)

- IoT temperature logger ingest for ILRs and MVU fridges
- INAPH read-only sync for animal ID validation
- NLM Udyami Mitra integration for entrepreneur subsidy tracking
- GeM rate-contract import for catalogued items
- Voucher-grade audit log
- Mobile-first PWA / Capacitor wrap for MVU offline-first dispensing
