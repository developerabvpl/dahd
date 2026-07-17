# 10 — Work121 Asset Maintenance (reference system spec)

> Reverse-engineered from the **live FSDA "Work121" asset-maintenance system**
> at `https://asset.fsdcs.in` (walkthrough, July 2026). This is the production
> reference we benchmark dahd's asset module against — see the
> [gap analysis](11-asset-maintenance-gap-analysis.md).
>
> Work121 is the UP **Food Safety & Drug Administration (FSDA)** asset system
> (ASP.NET Web Forms). Domain = **lab / scientific instruments** across 6 Food &
> Drug testing labs (Agra, Gorakhpur, Jhansi, Lucknow, Meerut, Varanasi) plus
> FSDA division/district offices. dahd's domain is **veterinary** equipment, but
> the *maintenance discipline* is identical — that is what we lift.

## Scale (observed)

- **547 active assets** (662 including all statuses).
- **~88 equipment groups** (HPLC, GC-MS/MS, LC-MS/MS, ICP-MS, AAS, bomb
  calorimeter, muffle furnace, weighing balance, …).
- Live signals on the dashboard: 42 scheduled-maintenance due, **44 PPM overdue**,
  **21 warranties expired**, **41 calibrations expired**, 23 machines with open
  incidents.

## Module map (19 screens, 5 areas)

### A. Masters (data entry)

1. **Item / Asset Master** — asset registration, **17 fields**:
   Folio No. (unique code) · Item Name · Serial Number · Model Number ·
   Asset Group · Store Name · **Status** (Active / Non-Active / Obsolete /
   **Deprecated**) · **Category (A / B / C)** = criticality class ·
   Make · **Supplier** · **Purchase Order Number** · **Purchase Order Date** ·
   **Invoice Number** · **Invoice Date** · **Date of Installation** ·
   Place of Installation · Building & Room of Installation.
   Grid over all assets with **Print** + **Export to Excel**.

2. **Asset Maintenance Details Master** — attaches upkeep parameters to an asset:
   **Maintenance Frequency (in days)** · **Maintenance Type** ·
   **AMC / CMC Expiry Date** · **Annual Maintenance Cost** · **Calibration Date**
   · Asset Picture.

3. **Scheduled Maintenance Details Log** — record a *completed* PPM visit against
   an asset: frequency + **Maintenance Job Details done** + previously-done history.

### B. Preventive maintenance (PPM) reports

4. **Asset Wise Maintenance Report** — full register (frequency, type, calibration dates).
5. **Asset Wise Maintenance Due Report** — next due date per asset + "previously done".
6. **Asset Wise Maintenance Due (Expired)** — assets whose PPM due date has passed (overdue worklist).
7. **Asset Frequency Wise Report** — group by frequency band:
   **Weekly (7d) / Fortnightly (15d) / Monthly (30d) / Quarterly (90d) /
   Half-yearly (180d) / Yearly**.

### C. Compliance reports (warranty · calibration · contracts)

8. **Asset Wise Warranty Report** — Warranty Expiry Date per asset.
9. **Asset Wise Warranty Expired Report** — out-of-warranty list.
10. **Asset Wise Calibration Report** — Calibration Date + **Calibration Expiry Date**,
    filterable by lab. (Critical for NABL lab accreditation; the vet analogue is
    cold-chain / weighing / dosing-equipment calibration.)
11. **Asset Wise AMC / CMC Report** — contract coverage per asset, distinguishing
    **CMC (comprehensive, *with* parts)** vs **AMC (*without* parts)**, each with an
    expiry date.
12. **Asset Report** — consolidated master: Asset Type · Serial · Location ·
    AMC/CMC · Maintenance Expiry · Warranty Expiry · **Last Maintenance On**.

### D. Incident / breakdown management (ITIL-style helpdesk)

13. **Raise Incident** — pick asset, then **Impact × Urgency → Priority**,
    **Problem** (type), **Description**, Status = Open.
14. **Manage Created Incident** — the machines with open incidents you raised
    (Machines List → Incident List drill-down) to track/close.
15. **Asset Incident Report** — per-asset breakdown/incident history.
16. **Manage Raised Incident** — SLA work queue filtered by status (Open/Close),
    incident number, incident date range, and **deadline date range**.
17. **Raised Incident Report** — reporting view of the same, by SLA deadline.

### E. Admin

18. **User Management** — look up a user by id → manage details/access.

### Dashboard (screen 19)

~23 KPI tiles, filterable by **Lab** and **Asset Group**:
total assets · scheduled-maintenance due · incidents overdue/open ·
maintenance due next 30 days · scheduled-maintenance expired ·
maintenance expiry this month / expired · **warranty expiry this month / expired** ·
**calibration expiry this month / expired** · incidents raised this-month /
closed this-month / raised-till-date / closed-till-date / due ·
**AMC cost incurred this-month / till-date**.

## Concepts worth stealing

- **Criticality class A/B/C** on every asset — drives prioritisation independent of
  equipment type.
- **Planned vs unplanned split**: PPM / calibration / AMC-CMC (calendar-driven) are
  kept separate from **incidents** (event-driven, with **SLA deadlines**).
- **Impact × Urgency → Priority** matrix for incidents, with a **deadline** that
  feeds an SLA queue.
- **AMC vs CMC** is a first-class distinction (parts coverage changes the cost model).
- **Calibration** is tracked as its own date + expiry, not folded into generic PPM.
- **Procurement provenance** (Supplier, PO no./date, Invoice no./date, Installation
  date) lives on the asset record, tying the register back to purchasing.

## Numbering (observed)

Two folio schemes coexist: legacy `V-0078`, structured `LKD/DRUG/UVCABINET/03`
(lab-drug-type-serial), plus `Agra-1` style serials. dahd keeps a single
`AssetTag`.
