# 03 — Pharmacy Supply-Chain MIS: The Whitespace

> This is the strategic finding that anchors the HMIS pitch. The veterinary side of UP has no equivalent to UPMSCL+DVDMS. That gap is the opportunity.

## The structural gap (verified)

**UPMSCL** — Uttar Pradesh Medical Supplies Corporation Ltd.
- Incorporated **23 March 2018** (CIN U85310UP2018SGC102425)
- Registered office: SUDA Bhawan, Gomti Nagar Extension, Lucknow
- Runs the full **human-health** supply chain across UP using:
  - **DVDMS** (Drug & Vaccine Distribution Management System) — indents, purchase orders, supply, consumption
  - **EMMS / e-Upkaran** (CDAC-built) — equipment maintenance & management. Live portal: https://emmsup.prd.dcservices.in
  - Multi-warehouse distribution network: divisional + district drug warehouses (e.g. Basti, Banda)

**But UPMSCL's mandate is explicitly limited to public health.** It does NOT cover veterinary drugs.

**Veterinary procurement in UP runs separately** through AHD direct rate contracts (185+ veterinary medicine tenders tracked, including FY 2024-25 vaccine rate contracts and ASCAD scheme rabies / blue-tongue tenders). **There is no verified UPMSCL-equivalent on the AHD side.**

## What about Bharat Pashudhan / NDLM?

**Bharat Pashudhan (NDLM)** — the national-level DAHD-mandated livestock digitisation portal:
- Live at https://bharatpashudhan.ndlm.co.in
- Formal **Section 39 PCICDA directions dated 12 December 2023** require registration on the Bharat Pashudhan app
- Scope: animal-ID, breeding, health registration

**Pharmacy, inventory, cold-chain, MVU dispensing are OUT of NDLM scope.** This is the explicit whitespace.

## What about INAPH?

INAPH (Information Network for Animal Productivity & Health) is real and used for NADCP animal registration / ear-tagging / vaccinator workflow. But:

> The claim that INAPH has a "stock management module suitable for pharmacy/inventory use" was **refuted (vote 0-2)** in adversarial verification.

**Do NOT position your product as "INAPH-adjacent" without primary verification.** Position it as gap-filler beyond INAPH's animal-health-record scope.

## Closest functional template: RMSC e-Aushadhi (Rajasthan, human-side)

From Rajasthan Medical Services Corporation's official supply-chain page:

> "Quarterly purchase orders are issued through e-Aushadhi as per the annual demand, consumption pattern, & suggestion of respective District Warehouse in-charges."

> "Near expiry drugs are taken care of through inter-warehouse transfers to ensure optimum utilization of drugs within the shelf life."

These are the **two verified mechanics** worth porting:

1. Quarterly PO cycle driven by consumption + warehouse-in-charge input
2. Near-expiry inter-warehouse redistribution

**Three RMSC claims were refuted** in our verification and should NOT be overstated in a pitch:

- "e-Aushadhi enables real-time central monitoring of stock levels" — **0-3 refuted**
- "RMSC resolves stock imbalances via inter-warehouse transfers managed via e-Aushadhi" — **1-0 refuted (insufficient quorum)**
- "District Drug Warehouses are the primary stocking tier" — **1-0 refuted (insufficient quorum)**

## The flow we'd be digitising

```
Central indent
  (AHD Directorate, Lucknow)
        │
        ▼
Warehouse GRN
  (state CMS — to be established or co-opted; UPMSCL parallel)
        │
        ▼
Divisional / district transfer
  (5 zones from the 1962 ICCC structure are natural divisions)
        │
        ▼
Hospital / dispensary / MVU issue
  (vet hospitals, polyclinics, dispensaries, 520 MVUs, AI sub-centres, gaushalas)
        │
        ▼
Animal dispensing
  (ear-tag / INAPH ID-linked record)
        │
        ▼
Reconciliation
  (consumption, expiry, wastage, audit trail)
```

## Three quick-win modules (verified whitespace)

| # | Module | Why it wins |
|---|---|---|
| **1** | **Vaccine cold-chain batch/expiry tracking** | NADCP explicitly bundles cold-chain procurement; FMD biannual cycles create wastage; gap NOT covered by Bharat Pashudhan |
| **2** | **MVU mobile dispensing app** | 520 MVUs deployed with 1962 helpline; no integrated dispensing/inventory app verified; ZHL ICCC is a natural integration partner |
| **3** | **Central CMS warehouse mgmt with FEFO** | Mirrors UPMSCL+DVDMS for human side; no vet incumbent. RMSC quarterly-PO + consumption-pattern logic is the template |

## Procurement positioning for your software

- **GeM SaaS / Cloud Software category** — fast lane if catalogued
- **UP e-tender ICT services tenders** — slower but standard
- **NLM Innovation & Extension sub-mission** — possible angle if positioned as livestock-health innovation
- **Direct EOI to ACS AHD office** — slowest but most strategic if framed as policy proposal modelled on UPMSCL

## Verified primary sources

- https://upmsc.in/home/Introduction
- https://cdac.in/index.aspx?productId=e-UpkaranEquipmentMaintenanceandManagementSystem(EMMS)
- https://emmsup.prd.dcservices.in
- http://rmsc.health.rajasthan.gov.in/content/raj/medical/rajasthan-medical-services-corporation-ltd-/en/services/Supply.html
- https://bharatpashudhan.ndlm.co.in/auth/login
- https://dahd.gov.in/node/2634 and https://dahd.gov.in/node/2635 (Section 39 PCICDA mandate)
