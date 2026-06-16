# 07 — Phased Roadmap

> A pitch-deck-ready phasing of the UP AHD Veterinary Pharmacy & Supply-Chain MIS. Each phase is independently demoable and independently saleable.
>
> Anchors: the structural gap in [03](03-pharmacy-supply-chain-mis.md), the demand map in [02](02-procurement-drugs-vaccines-assets.md), the synthesis in [04](04-vendor-opportunity-map.md).

## Phase summary

| Phase | Name | Duration | Status | Key outcome |
|---|---|---|---|---|
| **0** | Discovery & Research | 1 week | ✅ Done | This `docs/` folder |
| **1** | Reference Scaffold (MVP) | 2–3 weeks | ✅ Done | Working .NET 9 + Angular 19 demo with seed data |
| **2** | Production-Hardening | 4–6 weeks | 🔜 Next | Auth, audit log, real cold-chain capture, MVU offline-first |
| **3** | Procurement & Tender Integration | 6–8 weeks | Planned | GeM / etender.up.nic.in ingest, rate-contract management, vendor empanelment |
| **4** | FEFO Automation & Analytics | 4–6 weeks | Planned | Near-expiry redistribution (RMSC pattern), consumption-driven quarterly POs, KPI dashboards |
| **5** | INAPH / Bharat Pashudhan Bridge | 4 weeks | Planned | Animal-ID validation against NDLM, ear-tag sync, disease-event reporting |
| **6** | Scale-out | Ongoing | Future | Multi-tenant, gaushala vertical, ZHL / 1962 fleet integration, bio-energy traceability |

---

## Phase 0 — Discovery & Research ✅

**Goal**: build the evidence base before writing any code.

**Deliverables**:
- Three deep-research passes with 3-vote adversarial verification of every claim
- 7 markdown documents in `docs/`
- Verified primary sources for leadership, schemes, demand, procurement channels
- Refuted claims and open questions tracked separately ([05](05-open-questions.md))

**Why it matters for the pitch**: every number in the deck traces to a primary source. The refuted-claims section is the credibility moat — "we won't quote what we couldn't verify."

**Exit criteria** (all met):
- [x] Demand map by scheme bucket (NADCP, ASCAD, Pashu Aushadhi)
- [x] Procurement channel matrix (etender.up.nic.in, GeM, NLM, PCDF units)
- [x] Whitespace identification (UPMSCL exists for human, not for vet)
- [x] Decision-maker sequence (ACS → Directors → UPLDB → ZHL)

---

## Phase 1 — Reference Scaffold (MVP) ✅

**Goal**: a working end-to-end demo that proves the architecture and gives sales something to show in a 20-minute meeting.

**Deliverables shipped in this repo**:
- **.NET 9 Web API** (`api/`) — clean-architecture solution with Domain / Application / Infrastructure / Api projects, EF Core 9 + SQL Server, initial migration, Swagger
- **Angular 19 frontend** (`web/`) — standalone components, lazy routes, OnPush, signals, typed API client
- **Seed data** anchored in research: FMD + Brucella S19 + 8 ASCAD vaccines, 10 core veterinary drugs (oxytetracycline, enrofloxacin, ivermectin, albendazole, calcium borogluconate, B-complex, oxytocin, meloxicam, xylazine, povidone iodine), central warehouse + 5 divisional stores (Lucknow, Meerut, Varanasi, Gorakhpur, Agra matching the 1962 ICCC 5-zone model), sample facilities including 2 MVUs and the Mathura gaushala
- **8 feature views** covering: Dashboard KPIs, Drug & Vaccine master, Warehouse hierarchy, Facilities & MVU registry, Batch tracking with expiry badges (FEFO ready), Indent state machine (Draft → Submitted → Approved → Issued → Received → Closed), Cold-chain temperature log with breach detection, Dispense events with ear-tag

**Demoable in this phase**:
- Walk a vendor / department official through the formulary
- Show the 5-tier warehouse hierarchy (Central → Divisional → District → Facility)
- Issue a sample indent across warehouses and progress it through states
- POST a cold-chain reading outside 2–8 °C and see it flagged
- Record a dispense event against an MVU with an animal ear-tag

**Exit criteria** (all met):
- [x] `dotnet build` clean
- [x] `ng build` clean (lazy chunks per feature)
- [x] DB auto-creates and seeds on first API run
- [x] Frontend → API end-to-end working over HTTP at `localhost:5070`

**Known gaps (handed to Phase 2)**:
- No authentication / role-based access
- No POST UI in the frontend (everything is GET; data entry via Swagger)
- Manual cold-chain entries; no device integration
- Batch quantity reduces on dispense, but doesn't flow back through indent issue
- No audit trail on state transitions

---

## Phase 2 — Production-Hardening 🔜

**Goal**: take the scaffold from "demoable" to "deployable in a pilot district".

### 2.1 Auth & RBAC

- **JWT bearer auth** with refresh tokens
- **Roles**: `Director`, `Warehouse-Incharge`, `CVO` (district), `Facility-Vet`, `MVU-Vet`, `Admin-Readonly`
- **Permission matrix** wired to controllers via `[Authorize(Roles=...)]`
- Login + session management UI in Angular
- Password reset flow

### 2.2 Voucher-grade audit log

Every state transition (indent submit/approve/issue/receive, dispense, cold-chain breach acknowledgement, batch wastage) writes an append-only audit record with:
- Actor (userId, role, facility/warehouse)
- Before/after snapshot of mutated entity
- Timestamp, IP, request correlation id
- Optional digital signature for indent issues (Phase-3 enabler for GeM-grade traceability)

### 2.3 Cold-chain capture

- **CRUD UI** for cold-chain device registry (ILRs, deep freezers, MVU on-board fridges)
- **Mobile-friendly reading-entry form** for warehouse in-charges
- **Breach acknowledgement workflow**: breach detected → notified to in-charge → corrective action recorded → batch impact assessed (mark affected batches as `Recalled` or `Wasted`)
- Daily auto-rollup of breach count per device

### 2.4 MVU offline-first

- Wrap the dispense feature in a **PWA / Capacitor** shell
- Local IndexedDB stock + dispense queue
- Background sync when connectivity returns
- Ear-tag lookup with offline validator (Phase-5 swaps to live INAPH check)

### 2.5 Stock-flow consistency

- Issue-from-batch in the indent workflow actually reduces source batch quantity
- Receive-at-destination creates a derived batch record at the destination warehouse
- Negative-stock guard

**Exit criteria**:
- Pilot in 1 division (e.g. Lucknow): central + 1 divisional + 3 district stores + 5 facilities + 3 MVUs running for 30 days with no manual reconciliation gaps
- 0 stock-outs unexplained by the system
- 100% breach events acknowledged within 24 hours

**Procurement positioning**: pitch as a **pilot/PoC contract** — typically ₹50L–₹2 cr — fundable from Innovation & Extension sub-mission of NLM, or a discretionary AHD GO.

---

## Phase 3 — Procurement & Tender Integration

**Goal**: become the system of record for what the department procures, not just what it dispenses. This is what closes the loop with the demand map in [02](02-procurement-drugs-vaccines-assets.md).

### 3.1 Rate-contract management

- Import the published "Veterinary Medicines / Vitamins / Hormones / Minerals" rate contract from `animalhusb.upsdc.gov.in/en/approved-rate-list`
- Import the parallel "Veterinary Equipments / Machineries & Instruments" rate contract
- Track validity windows, item-level rates, vendor mapping
- Surface "cheapest available rate-contracted vendor" against any indent line

### 3.2 GeM catalogue mirror

- Pull the GeM seller-side product catalogue for catalogued drugs / vaccines / devices
- Match against the formulary
- Show parallel pricing: rate-contract vs GeM, alert when GeM L1 beats the RC by > threshold

### 3.3 etender.up.nic.in monitor

- Periodic scrape (with respectful intervals) of UP GePNIC for AHD / UPLDB / PCDF tenders
- Categorise by NIC product-code mapping to formulary items
- Tender pipeline dashboard for the procurement office

### 3.4 Vendor empanelment

- Vendor portal (`/vendor`) for registration, document upload (Drug Licence, GMP, ISO, BIS, manufacturer authorization, past-performance certs)
- AHD-side approval workflow with site-inspection scheduling
- Empanelment expiry alerts
- Blacklist registry with reason and review date

### 3.5 NADCP procurement calendar

- Pre-loaded with FMD biannual windows (Sept–Oct, Mar–Apr) and the Brucella annual cohort cycle
- Cold-chain procurement bundle reminders (ice liners, refrigerators) parallel to vaccine procurement

**Exit criteria**:
- Department can publish, approve, and award a rate contract end-to-end inside the system
- Vendor onboards via portal with no paper
- FMD biannual procurement cycle runs entirely through the system in one round

**Procurement positioning**: pitch as **scale-out contract** — typically ₹5–25 cr depending on division coverage. Fundable from state AHD budget head + LH&DC matching share.

---

## Phase 4 — FEFO Automation & Analytics

**Goal**: move from descriptive (what happened) to prescriptive (what should happen) — directly mirroring the **RMSC quarterly-PO + near-expiry-redistribution** template that survived verification in [03](03-pharmacy-supply-chain-mis.md).

### 4.1 FEFO automation

- Auto-suggest issue batch on indent approval (oldest expiry first)
- Block manual override unless reason recorded
- Wastage attribution per warehouse / facility / vet

### 4.2 Near-expiry redistribution engine

- Identify batches at any warehouse approaching expiry (< 90 / 60 / 30 days)
- Match against current consumption velocity at sibling warehouses
- Suggest inter-warehouse transfers (the *verified* RMSC mechanic)
- One-click create-transfer-indent

### 4.3 Consumption-driven quarterly indents

- Aggregate 12-month consumption per facility / warehouse / drug
- Forecast next-quarter requirement (seasonality-aware: FMD spikes around Sept–Oct and Mar–Apr campaigns)
- Auto-draft quarterly indents for warehouse in-charge approval
- Match the **verified** RMSC mechanic: "Quarterly purchase orders are issued through e-Aushadhi as per the annual demand, consumption pattern, & suggestion of respective District Warehouse in-charges"

### 4.4 KPI dashboards

- **Director view**: state-wide stock health, expiry exposure, cold-chain compliance, vendor performance
- **CVO view**: district roll-up
- **In-charge view**: facility / MVU level
- **Procurement-officer view**: RC utilisation, pending tenders, vendor pipeline

### 4.5 Cold-chain analytics

- Mean kinetic temperature (MKT) per device
- Time-out-of-spec per ILR (drives ILR replacement budgeting)
- Batch impact assessment when a breach affects stored vaccines

**Exit criteria**:
- Wastage reduces vs Phase-3 baseline (target -30% in pilot division)
- Quarterly indent generation is one-click, not data-entry
- Cold-chain breach affecting a batch auto-triggers redistribution suggestion

**Procurement positioning**: typically delivered as a **paid feature module** on top of the Phase-3 contract; renews annually.

---

## Phase 5 — INAPH / Bharat Pashudhan Bridge

**Goal**: connect the pharmacy system to the national animal-identification spine.

> ⚠️ This phase depends on DAHD / NDLM granting API access. As of the research date, INAPH stock-management capability was **refuted** ([05](05-open-questions.md)) — we are **not** competing with INAPH; we are integrating with it for animal-ID only.

### 5.1 Read-only INAPH sync

- Animal-ID and ear-tag validation at dispense time (online)
- Owner / breed / species pre-fill from INAPH
- Diagnosis vocabulary aligned with INAPH's animal-health module taxonomy
- Disease event posting back to Bharat Pashudhan (Section 39 PCICDA mandate, 12 Dec 2023)

### 5.2 NADCP vaccinator workflow

- Vaccinator login, vaccination round assignment
- Ear-tag scan → confirm animal cohort eligibility (4–8 months for Brucella; all bovines for FMD)
- Dose recording against the round
- Honorarium calculation (₹3/dose, ₹2/ear-tag floor; configurable to recommended ₹10 FMD / ₹12 Brucella)

### 5.3 Outbreak alerts

- When dispense events for a disease cluster spike in a district, auto-flag to the district epidemiologist
- Cross-reference with Bharat Pashudhan disease reporting

**Exit criteria**:
- Live INAPH sync working in pilot district with < 5 % validation failures
- A complete NADCP FMD round (one of the two annual windows) runs end-to-end inside the system

**Procurement positioning**: usually bundled into the state AHD's NDLM / NADCP digital component. Requires DAHD partnership clearance.

---

## Phase 6 — Scale-out (ongoing, beyond v1.0)

**Goal**: turn the platform from "UP AHD pharmacy MIS" into "UP livestock digital backbone".

### 6.1 Multi-tenant

- Onboard other states with the same architecture (Bihar, MP, Rajasthan AHDs as natural follow-ons)
- Tenant-level customisation: rate contracts, vaccine basket, breed master

### 6.2 Gaushala vertical

- Connect with the **UP Urban Development Department**'s Kanha Gaushala scheme (₹165.89 lakh/site envelope, administered separately from AHD)
- Asset-level tracking: chaff cutters, water troughs, fans, milking machines
- Biogas plant capex tracking under the **UP State Bio-Energy Policy 2022** (10-year electricity-duty waiver, stamp-duty waiver, dev-charge waiver)
- Cow-dung-to-wealth traceability: dung volume → biogas/compost output → revenue

### 6.3 1962 / ZHL fleet integration

- Tight integration with the 520-MVU fleet (Ziqitza Healthcare runs 92 of 520 across 14 western districts)
- Call-centre handoff: 1962 caller → vet dispatched → on-site dispense recorded → reconciliation
- ICCC dashboard for the 5-zone command structure

### 6.4 PCDF / dairy adjacency

- Inventory rails for PCDF unit milk unions (Varanasi at paragvns.com etc.)
- Cattle-feed and supplement traceability through the dairy cooperative tier

### 6.5 NLM entrepreneur partner mode

- White-label rollout for NLM-funded entrepreneurs running their own micro-dairies / poultry units
- Subsidy-claim and reimbursement workflow tied to nlm.udyamimitra.in

**Procurement positioning**: large multi-state contracts, foundation grants, or platform-licensing.

---

## Dependencies graph

```
Phase 0 ──► Phase 1 ──► Phase 2 ──┬──► Phase 3 ──► Phase 4
                                  │
                                  └──► Phase 5 (parallel to 3-4)

                                            Phase 4 ──► Phase 6
```

Phase 5 can run in parallel with Phases 3–4 because the INAPH bridge is data-flow, not a hard prerequisite for procurement features.

## Cross-cutting concerns

These apply across all phases and should be tracked from Phase 2 onward:

- **Security**: input validation, parameterised queries (already enforced via EF Core), no plain-text secrets, HTTPS everywhere in prod, OWASP top-10 review at each phase exit
- **Performance**: pagination on all list endpoints by Phase 2, indexed search by Phase 3, caching layer by Phase 4
- **Observability**: structured logging from day one, OpenTelemetry instrumentation by Phase 2, full request-tracing by Phase 3
- **Localisation**: Hindi support from Phase 2 (the UP department's working language), Devanagari font rendering, date format `dd/MM/yyyy`
- **Accessibility**: WCAG AA from Phase 2 (mandatory for government IT procurement)
- **Backup/DR**: nightly DB backups from Phase 2, geo-redundant storage by Phase 3
- **Compliance**: CERT-In empanelled audit before Phase 3 production rollout

## What we will NOT build (deliberate scope cuts)

- **No competing with INAPH / Bharat Pashudhan** for animal-ID. We integrate, we don't replace.
- **No human-health functionality**. UPMSCL owns that lane.
- **No telemedicine consultation**. The 1962 ecosystem already covers this via GVK EMRI.
- **No financial / payroll** beyond honorarium calculation.
- **No livestock insurance underwriting**. NLM Innovation sub-mission already has incumbents.

## Pitch-deck mapping

| Slide | Phase ref |
|---|---|
| "The problem" | Phase 0 evidence, [03](03-pharmacy-supply-chain-mis.md) UPMSCL gap |
| "Demo" | Phase 1 scaffold |
| "What we'll deliver in 6 months" | Phase 2 |
| "What we'll deliver in 12 months" | Phase 3 + 4 |
| "What we'll deliver in 18 months" | Phase 5 + 6 onset |
| "Why us" | Phase 0 research depth, refuted-claims transparency |
| "Risks & mitigations" | [05](05-open-questions.md) open questions, this doc's "What we will NOT build" |
