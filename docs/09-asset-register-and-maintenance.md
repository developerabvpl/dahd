# 09 — Asset Register & Maintenance

> Added after the research docs. Clarifies how "assets" are handled in the build,
> because [02](02-procurement-drugs-vaccines-assets.md) frames assets as a
> *procurement lane* while this module adds a *register + upkeep* capability.
>
> **Update (Jul 2026):** benchmarked against the FSDA **Work121** production system
> ([spec](10-work121-asset-maintenance-spec.md), [gap analysis](11-asset-maintenance-gap-analysis.md))
> and enriched with: **criticality class A/B/C**, procurement provenance
> (supplier / PO / invoice / installation date), **first-class calibration**
> (date + due, `/api/maintenance/calibration-due`), **AMC vs CMC** contract type,
> and **ITIL incidents** (Impact × Urgency → Priority → SLA deadline, with
> SLA-breach flags). New KPIs: warranty & calibration expiring/expired, critical &
> SLA-breached incidents, AMC/CMC annual-cost total.

## Two meanings of "asset" — both covered

1. **Procurement lane** (per [02](02-procurement-drugs-vaccines-assets.md)): equipment the
   department *buys* — cold-chain cabinets, AI guns, autoclaves, microscopes, lab/diagnostic
   kit, MVU outfitting. This is driven by the **"Veterinary Equipments / Machineries &
   Instruments" rate contract** (see the Rate Contracts module, `AHD-EQP-{year}`), GeM, and
   tenders. *What to buy and from whom.*

2. **Register + maintenance** (this module): once bought, each unit becomes a tracked
   **Asset** with location, serial, purchase cost, warranty, condition, and an upkeep
   history. *What we own, where it is, and whether it's working.*

The **Asset register is the priority**; **maintenance is the documented add-on**.

## Asset register (the priority)

`Asset` entity / `/assets` page:

| Field | Purpose |
|---|---|
| AssetTag (unique) | Physical tag / barcode id |
| Name, Category | DiagnosticEquipment, ColdChainEquipment, LabEquipment, AiEquipment, Vehicle, ItHardware, ... |
| Model, SerialNumber, Manufacturer | Identification |
| Warehouse / Facility / LocationNote | Where it physically sits |
| PurchaseDate, PurchaseCost, WarrantyUntil | Procurement + warranty record |
| Status | Active / UnderMaintenance / BreakdownReported / Condemned / Disposed |
| Condition | New / Good / Fair / Poor |

This maps directly to the equipment the department procures under the Equipment rate contract.

## Maintenance add-on

- **MaintenanceSchedule (PPM)** — preventive task, frequency (days), last service, next-due.
  Overdue and due-soon surfaced on `/maintenance`.
- **MaintenanceJob** — `Breakdown` or `Preventive` work order with a lifecycle:
  `Open → InProgress → Completed`. Completing a job returns the asset to `Active` and rolls
  the linked PPM schedule forward automatically.
- **AmcContract** — annual maintenance contract per asset (vendor, period, annual cost,
  coverage); expiry surfaced at 60 days.

## Seeded demo data

`DahdSeeder.SeedAssetsAsync` creates (idempotently, once):

- **12 assets** — 2 ILRs, deep freezer, ultrasound, microscope, autoclave, AI-gun kit,
  LN2 container, MVU vehicle, X-ray, generator, desktop PC — placed across the central
  store / hospital / MVU.
- **8 PPM schedules** — a mix of overdue (ILR calibration, deep-freezer compressor,
  autoclave seal service, X-ray radiation-safety) and upcoming.
- **2 open breakdowns** — autoclave not reaching pressure; X-ray exposure fault.
- **3 AMC contracts** — ultrasound, X-ray (expiring soon), MVU vehicle.

## API surface

| Endpoint | Purpose |
|---|---|
| `GET /api/assets` `?status=&category=` | Register list (with schedules, jobs, AMC) |
| `GET /api/assets/kpis` | Counts: active / under-maintenance / breakdown / overdue PPM / AMC expiring |
| `POST /api/assets` | Add an asset (Admin/Director) |
| `PATCH /api/assets/{id}/status` | Change status/condition (e.g. condemn) |
| `POST /api/assets/{id}/schedules` | Add a PPM schedule |
| `POST /api/assets/{id}/amc` | Add an AMC contract |
| `GET /api/maintenance/due` `?withinDays=` | PPM due/overdue |
| `GET /api/maintenance/jobs` `?status=` | Job board |
| `POST /api/maintenance/assets/{id}/breakdown` | Log a breakdown |
| `POST /api/maintenance/assets/{id}/ppm-job` | Raise a PPM job |
| `POST /api/maintenance/jobs/{id}/start` · `/complete` | Progress a job |

Every mutation is written to the audit log (Phase 2.2).

## Not built (deliberate — can be added if wanted)

Borrowed-but-not-built from the HMIS-Git D11 spec: **depreciation** (straight-line /
reducing-balance), **condemnation workflow** (Board of Survey → scrap/donate/auction →
disposal certificate), **scavenged parts** (harvest from condemned equipment), and
**asset transfer / loan** chain-of-custody. Flagged here so scope stays explicit.
