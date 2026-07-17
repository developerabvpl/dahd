# 11 — Asset Maintenance: gap analysis (Work121 → dahd)

> Maps every capability in the [Work121 reference](10-work121-asset-maintenance-spec.md)
> to dahd's current asset module (`Asset` / `MaintenanceSchedule` / `MaintenanceJob`
> / `AmcContract`, controllers `AssetsController` + `MaintenanceController`, Angular
> `/assets` + `/maintenance`). Status legend: ✅ Have · 🟡 Partial · ❌ Missing.
> The **Build** column is what this pass implements.

## A. Masters

| Work121 capability | dahd today | Status | Build |
|---|---|---|---|
| Folio No. (unique code) | `Asset.AssetTag` | ✅ | — |
| Item Name / Serial / Model / Make | `Name` / `SerialNumber` / `Model` / `Manufacturer` | ✅ | — |
| Asset Group (~88 named types) | `AssetCategory` (9 broad classes) | 🟡 | Keep broad classes (vet ≠ 88 lab types) |
| Store / Place / Building-Room | `WarehouseId` / `FacilityId` / `LocationNote` | ✅ | — |
| Status incl. **Deprecated/Obsolete** | `AssetStatus` (Active/UnderMaint/Breakdown/Condemned/Disposed) | ✅ | — (Condemned/Disposed cover it) |
| **Category A / B / C (criticality)** | — | ❌ | **`AssetCriticality` A/B/C + `Asset.Criticality`** |
| **Supplier** | — | ❌ | **`Asset.Supplier`** |
| **PO Number / PO Date** | — | ❌ | **`Asset.PoNumber` / `PoDate`** |
| **Invoice Number / Invoice Date** | — | ❌ | **`Asset.InvoiceNumber` / `InvoiceDate`** |
| **Date of Installation** | `PurchaseDate` only | 🟡 | **`Asset.InstallationDate`** |
| PurchaseDate / PurchaseCost / Warranty | ✅ | ✅ | — |
| Maintenance frequency (days) | `MaintenanceSchedule.FrequencyDays` | ✅ | — |
| Annual maintenance cost | `AmcContract.AnnualCost` | ✅ | — |
| **Calibration date + expiry (first-class)** | only a job *type* `Calibration` | 🟡 | **`Asset.CalibrationDate` / `CalibrationDueDate`** |
| Asset picture | — | ❌ | Out of scope (file storage) |
| Print / Export to Excel | — | ❌ | Out of scope this pass |

## B. PPM reports

| Work121 | dahd today | Status | Build |
|---|---|---|---|
| Maintenance report (register) | `GET /assets` (schedules embedded) | ✅ | — |
| Maintenance **due** | `GET /maintenance/due?withinDays` | ✅ | — |
| Maintenance due **(expired)** | overdue rows in `/due` + `OverduePpm` KPI | ✅ | — |
| Frequency-wise | client filter | 🟡 | acceptable (schedule carries `FrequencyDays`) |

## C. Compliance

| Work121 | dahd today | Status | Build |
|---|---|---|---|
| Warranty report | `Asset.WarrantyUntil` | ✅ | — |
| Warranty **expired** | derivable, no KPI | 🟡 | **KPI: warranty expiring/expired** |
| **Calibration report + expiry** | ❌ | ❌ | **`GET /maintenance/calibration-due` + calibration KPIs** |
| **AMC vs CMC** distinction | `AmcContract` (AMC only) | 🟡 | **`MaintenanceContractType` Amc/Cmc on `AmcContract`** |
| Consolidated Asset Report | `GET /assets` + `/kpis` | ✅ | — |

## D. Incident management (ITIL)

| Work121 | dahd today | Status | Build |
|---|---|---|---|
| Raise incident | `POST /maintenance/assets/{id}/breakdown` (desc + assignee) | 🟡 | **add Impact/Urgency/Problem** |
| **Impact × Urgency → Priority** | — | ❌ | **enums + matrix helper → `MaintenanceJob.Priority`** |
| **Problem type** | free-text description | 🟡 | **`IncidentProblemType` enum** |
| **SLA deadline** | — | ❌ | **`MaintenanceJob.Deadline` from priority SLA** |
| Manage/close incident | job `start`/`complete` | ✅ | — |
| Per-asset incident history | `Asset.Jobs` | ✅ | — |
| SLA/deadline queue | — | ❌ | **`/maintenance/jobs` returns priority + deadline + `slaBreached`** |

## E. Dashboard KPIs

dahd `AssetKpiDto` has 8 counts. **Build** adds: warranty expiring/expired,
calibration expiring/expired, AMC annual-cost total, and open breakdowns by
priority (High/critical count).

## Priority & SLA model (new)

`Priority = matrix(Impact, Urgency)` (3×3 → Low/Medium/High/Critical), and the
**SLA deadline** = `reportedAt + hours(priority)`:

| Priority | SLA (hours) |
|---|---|
| Critical | 4 |
| High | 24 |
| Medium | 72 |
| Low | 168 |

Impact × Urgency grid:

| | Urgency High | Urgency Medium | Urgency Low |
|---|---|---|---|
| **Impact High** | Critical | High | Medium |
| **Impact Medium** | High | Medium | Medium |
| **Impact Low** | Medium | Medium | Low |

## Out of scope (flagged, not built)

Asset picture upload / file store · Excel & PDF export · the full 88-item lab
taxonomy · Work121's separate "created" vs "raised" incident actor split (dahd
uses one job board with role gating). Depreciation / condemnation-board /
scavenged-parts remain as noted in [09](09-asset-register-and-maintenance.md).
