# 08 — Pitch Deck (10 slides)

> A slide-by-slide outline drawing from the verified research in [04](04-vendor-opportunity-map.md) and the live demo modules in this repo. Drop-in copy for a 20-minute pitch to UP AHD leadership.

**Audience**: ACS AH (Mukesh Kumar Meshram, IAS) → Director AH / Director Disease Control & Farms → CVO Lucknow → ZHL (MVU integration partner).
**Total time**: 20 minutes including demo.
**Deck length**: 10 slides + appendix.

---

## Slide 1 — Title

**DAHD Veterinary Pharmacy & Supply-Chain MIS**
*Closing the human-side / animal-side parity gap in UP*

Subtitle one-liner: *UPMSCL + DVDMS exists for human medicine. Nothing equivalent exists for veterinary. This is that.*

Footer: prepared for the Department of Animal Husbandry, Government of Uttar Pradesh.

---

## Slide 2 — The problem (verified)

**UPMSCL** (CIN U85310UP2018SGC102425, incorporated 23 March 2018) runs the full human-health supply chain:
- **DVDMS** for indents → POs → supply → consumption
- **EMMS** (CDAC e-Upkaran) for equipment lifecycles
- Multi-warehouse network across divisions

**Veterinary scope is explicitly out of UPMSCL's mandate.**

Today, UP AHD veterinary procurement runs through 185+ scattered rate-contract tenders. **There is no equivalent DVDMS for the veterinary side.**

> Source: upmsc.in/home/Introduction · cross-checked against animalhusb.upsdc.gov.in/en/approved-rate-list (185+ veterinary medicine tenders).

---

## Slide 3 — The cost of that gap

What we documented (and what we couldn't yet — see [05](05-open-questions.md)):

| Cost | Evidence | Status |
|---|---|---|
| FMD biannual cycle waste | UP runs **~520.36 lakh doses × 2 rounds/year** under NADCP | Verified |
| Cold-chain breaches | No central register; depends on paper logs at each ILR | Inferred |
| Near-expiry destruction | Common pattern; RMSC's *verified* mechanic is inter-warehouse redistribution | Verified template |
| Manual indent paper-trail | No system-of-record below UPLDB | Verified absent |
| 520 MVUs running blind | Launched **26 March 2023, ₹201 crore**, GVK EMRI / ZHL operators; no central dispensing app verified | Verified absent |

> Cite: docs/02 (NADCP), docs/03 (RMSC template, NDLM scope gap), docs/01 (520-MVU programme launch).

---

## Slide 4 — Our answer in one diagram

```
Central Indent → Warehouse GRN → Divisional / District Transfer → Facility / MVU Issue → Animal Dispense → Reconciliation
     │                  │                       │                          │                   │                │
     ▼                  ▼                       ▼                          ▼                   ▼                ▼
 Campaign           Batch master            Stock-by-Drug              Offline PWA          Ear-Tag         Audit trail
 calendar           with FEFO               redistribution             on MVU tablet        + INAPH ID      (voucher grade)
```

Five-tier supply-chain spine, six interlocking modules, one audit log. Built on .NET 9 + Angular 19. Postgres or SQL Server, plug-in.

---

## Slide 5 — Live demo (5 minutes)

**Single screen-record**, narrated:

1. Login as `director` → see role-specific dashboard
2. Open **Campaigns** → FMD-2026-FALL is in the pipeline → click "Draft indents to all districts"
3. Sign out, login as `cvo` → see open indent in queue → approve
4. Sign out, login as `wh` → see indent ready to issue → **FEFO picks the oldest-expiry batch automatically**
5. Sign out, login as `mvuvet` → record a dispense; ear-tag lookup pre-fills from **INAPH-style** registry; works **offline**
6. Show **Redistribution** — system flagged near-expiry stock and proposed a transfer to a zero-stock division
7. Show **Cold-Chain Analytics** — 7×24 breach heatmap reveals "every Sunday 3 AM" pattern
8. Show **Audit Log** — every actor, every IP, every state transition captured

> All six personas live in the seeded demo. Six modules. One pitch.

---

## Slide 6 — What we built that's *new* vs UPMSCL

| Module | Why it's not in UPMSCL | Demoable today |
|---|---|---|
| **Vaccine cold-chain capture + breach acknowledgement** | UPMSCL covers human cold chain; vet ILRs / MVU fridges are uncovered | ✅ |
| **MVU offline dispensing PWA** | No equivalent — the 520-MVU fleet runs on paper | ✅ |
| **Campaign-driven procurement calendar** | NADCP demand is unique to vet; UPMSCL doesn't model it | ✅ |
| **Near-expiry redistribution engine** | Mirrors verified RMSC mechanic (cited, not invented) | ✅ |
| **Animal-ID linkage at dispense** | Connects to INAPH / Bharat Pashudhan (stub today; live integration is Phase 5) | ✅ stub |
| **Vendor empanelment portal** | UPMSCL has no public-facing vendor self-service | ✅ |

---

## Slide 7 — Anchored in primary research, not assumptions

Every figure in this deck is verifiable:

- **520.36 lakh FMD doses × 2 rounds/year** — DAHD NADCP schedule
- **45-day windows** Sept–Oct and Mar–Apr — DAHD operational guidelines
- **Brucella S19, 4–8 month female calves, once-in-lifetime** — ICAR-NIVEDI 2023
- **₹201 crore for 520 MVUs, launched 26 March 2023, helpline 1962** — multi-sourced
- **₹165.89 lakh per Kanha Gaushala** — UP Urban Development Department portal
- **UP State Bio-Energy Policy 2022 — 10-year electricity duty waiver** — invest.up.gov.in

We also **track what we couldn't verify** (see [docs/05](05-open-questions.md)) — three milk-production claims, INAPH stock-management capability, and 40-50 piloted gaushala biogas units were **refuted in adversarial verification**. We won't pitch on those.

---

## Slide 8 — Phased delivery

| Phase | Outcome | Duration | Procurement channel |
|---|---|---|---|
| **0** Research | Verified evidence base | Done | n/a |
| **1** MVP scaffold | .NET + Angular reference; all 8 modules running with seed data | Done | n/a |
| **2** Hardening | JWT auth, voucher-grade audit, cold-chain ack, offline MVU PWA, FEFO stock-flow | Done | NLM Innovation sub-mission, ₹50L–2cr PoC |
| **3** Procurement integration | Vendor portal, NADCP calendar, rate-contract import, GeM/etender monitor | 6–8 wks | State AHD budget head + LH&DC, ₹5–25cr |
| **4** Analytics | FEFO automation, near-expiry redistribution, quarterly consumption forecast, role dashboards, cold-chain MKT | Done | Annual feature module |
| **5** INAPH bridge | Animal-ID validation against Bharat Pashudhan, NADCP vaccinator workflow | 4 wks (after DAHD clearance) | NDLM digital-component bundle |
| **6** Scale-out | Multi-state, gaushala vertical, biogas traceability, dairy adjacency | Ongoing | Platform licensing |

> All of Phase 0–4 already lives in this repo. Phase 5 is stubbed and ready.

---

## Slide 9 — Procurement positioning

How UP AHD can buy this **today**:

1. **GeM SaaS / Cloud Software category** — fast lane if we list there
2. **etender.up.nic.in ICT services** — standard
3. **NLM Innovation & Extension sub-mission** — frame as livestock-health innovation
4. **Direct EOI to ACS AHD** — slowest but most strategic; we'd propose UPMSCL-style corporatisation

First-meeting ask: **a 3-district pilot** under the Lucknow / Meerut / Varanasi ICCC zones for ₹50L over 6 months, contingent on the campaign → indent → MVU dispense loop running end-to-end with zero unexplained stock-outs.

Vendor empanelment is in the platform from day one, so suppliers (vaccines, equipment, cold-chain cabinets, MVU outfitting) can register against it the moment we deploy.

---

## Slide 10 — Why us

- **We don't sell on numbers we couldn't verify** — see refuted-claims register
- **We built the demo before the pitch** — every module above is callable, role-gated, audited
- **Not competing with INAPH or UPMSCL** — we integrate with the former, mirror the architecture of the latter, and fill the explicit gap between them
- **Open architecture**: clean-architecture .NET API, lazy-loaded Angular PWA, EF Core migrations, audit-first
- **Offline-first for MVUs** — the fleet doesn't lose data when the cell tower does

> Repo: https://github.com/developerabvpl/dahd
> Demo: ~20 minutes, no slide deck needed if you'd rather see the working app.

---

## Appendix — material we'd hand over

- **A1**: Full research document set (docs/00–07)
- **A2**: Refuted-claims register (docs/05)
- **A3**: API surface (Swagger at /swagger)
- **A4**: Role matrix + audit-event catalogue
- **A5**: ngsw-config + PWA install instructions for MVU tablets
- **A6**: SQL Server / Postgres deployment guide
- **A7**: Open-source license + security baseline (CERT-In appendix)

## Speaker notes (per slide)

- **Slide 2**: Pause on "explicitly out of UPMSCL's mandate". This is the entire gap.
- **Slide 3**: Don't editorialise — only the *Verified* rows have a primary source. Acknowledge the *Inferred* ones.
- **Slide 5**: This is the meeting. If they want to skip slides 6–10 and ask questions during demo, oblige.
- **Slide 8**: Phase 5 timeline is conditional on DAHD clearance — flag that.
- **Slide 10**: The refuted-claims register IS the differentiator. Most vendors won't show this.
