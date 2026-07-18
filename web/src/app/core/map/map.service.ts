import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ColdChainKind, ColdChainUnit, DistrictShade, FieldForce, FieldStatus, GeoNetwork,
  MapWarehouse, Sensor, Store, Toolbox, VaccineStock
} from './map.models';

// Uttar Pradesh districts with approximate centroids [name, lat, lng].
// Representative set (~50 of UP's districts); each gets TWO stores per the
// requirement ("assume every district has two stores").
const DISTRICTS: [string, number, number][] = [
  ['Lucknow', 26.85, 80.95], ['Kanpur Nagar', 26.45, 80.33], ['Agra', 27.18, 78.01],
  ['Varanasi', 25.32, 82.97], ['Prayagraj', 25.44, 81.85], ['Meerut', 28.98, 77.71],
  ['Ghaziabad', 28.67, 77.45], ['Gorakhpur', 26.76, 83.37], ['Bareilly', 28.36, 79.42],
  ['Aligarh', 27.88, 78.08], ['Moradabad', 28.84, 78.77], ['Saharanpur', 29.97, 77.55],
  ['Jhansi', 25.45, 78.57], ['Ayodhya', 26.80, 82.20], ['Firozabad', 27.15, 78.40],
  ['Mathura', 27.49, 77.67], ['Muzaffarnagar', 29.47, 77.70], ['Rampur', 28.81, 79.03],
  ['Sambhal', 28.58, 78.55], ['Amroha', 28.90, 78.47], ['Bulandshahr', 28.40, 77.85],
  ['Gautam Buddh Nagar', 28.53, 77.39], ['Bijnor', 29.37, 78.14], ['Sitapur', 27.57, 80.68],
  ['Hardoi', 27.40, 80.13], ['Unnao', 26.55, 80.49], ['Rae Bareli', 26.22, 81.24],
  ['Sultanpur', 26.26, 82.07], ['Barabanki', 26.93, 81.19], ['Fatehpur', 25.93, 80.81],
  ['Pratapgarh', 25.90, 81.95], ['Jaunpur', 25.75, 82.68], ['Ghazipur', 25.58, 83.58],
  ['Ballia', 25.76, 84.15], ['Azamgarh', 26.07, 83.18], ['Mau', 25.94, 83.56],
  ['Deoria', 26.50, 83.79], ['Basti', 26.81, 82.75], ['Gonda', 27.13, 81.96],
  ['Bahraich', 27.57, 81.60], ['Lakhimpur Kheri', 27.95, 80.78], ['Shahjahanpur', 27.88, 79.91],
  ['Budaun', 28.03, 79.12], ['Etawah', 26.78, 79.02], ['Mainpuri', 27.23, 79.02],
  ['Etah', 27.63, 78.66], ['Banda', 25.48, 80.34], ['Mirzapur', 25.15, 82.57],
  ['Sonbhadra', 24.69, 83.07], ['Chandauli', 25.26, 83.27]
];

const FIRST = ['Rajesh', 'Sunil', 'Anil', 'Amit', 'Vijay', 'Manoj', 'Sanjay', 'Ravi', 'Pramod',
  'Dinesh', 'Alok', 'Neeraj', 'Suresh', 'Kailash', 'Arvind', 'Deepak', 'Ramesh', 'Ashok', 'Vinod', 'Yogendra'];
const LAST = ['Kumar', 'Singh', 'Yadav', 'Sharma', 'Verma', 'Mishra', 'Tripathi', 'Pandey',
  'Gupta', 'Srivastava', 'Chauhan', 'Rawat', 'Dubey', 'Tiwari', 'Saxena'];

const VACCINES: { v: string; sp: string; frozen: boolean }[] = [
  { v: 'FMD Vaccine (Raksha Ovac)', sp: 'Cattle & Buffalo', frozen: false },
  { v: 'HS Vaccine (Haemorrhagic Septicaemia)', sp: 'Cattle & Buffalo', frozen: false },
  { v: 'Black Quarter (BQ) Vaccine', sp: 'Cattle', frozen: false },
  { v: 'Brucella S19 Vaccine', sp: 'Female calves', frozen: false },
  { v: 'PPR Vaccine (Peste des Petits Ruminants)', sp: 'Sheep & Goat', frozen: true },
  { v: 'Goat Pox Vaccine', sp: 'Sheep & Goat', frozen: false },
  { v: 'Classical Swine Fever Vaccine', sp: 'Pigs', frozen: false },
  { v: 'Anti-Rabies Vaccine', sp: 'Dogs & all species', frozen: false },
  { v: 'Enterotoxaemia (ET) Vaccine', sp: 'Sheep & Goat', frozen: false }
];

// Deterministic PRNG so the demo network is stable across reloads.
function mulberry32(seed: number) {
  return function () {
    seed |= 0; seed = (seed + 0x6D2B79F5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

@Injectable({ providedIn: 'root' })
export class MapService {
  private readonly http = inject(HttpClient);
  private cache: GeoNetwork | null = null;

  /** Real backend warehouses + live stock + cold-chain equipment (Network Map "Live" layer). */
  liveWarehouses(): Observable<MapWarehouse[]> {
    return this.http.get<MapWarehouse[]>(`${environment.apiUrl}/map/warehouses`);
  }

  /** True UP district boundary polygons — all 75 current districts, served as an app asset. */
  boundaries(): Observable<any> {
    return this.http.get<any>('up-districts.geojson');
  }

  /**
   * Canonical district key for joining boundary names ↔ our/backend names.
   * The current 75-district set already uses modern names (Prayagraj, Ayodhya,
   * Amroha…); the legacy aliases are kept so old backend names still match.
   */
  static canon(name?: string | null): string {
    const k = (name ?? '').toLowerCase().replace(/[^a-z]/g, '');
    const alias: Record<string, string> = {
      allahabad: 'prayagraj', faizabad: 'ayodhya', kanpur: 'kanpurnagar',
      badaun: 'budaun', jyotibaphulenagar: 'amroha', gautambuddhanagar: 'gautambuddhnagar',
      santravidasnagar: 'bhadohi', barabanki: 'barabanki'
    };
    return alias[k] ?? k;
  }

  /** Place a real warehouse on the map from its district (or a district name inside its title). */
  geocode(district?: string | null, name?: string | null): { lat: number; lng: number } | null {
    const hay = `${district ?? ''} ${name ?? ''}`.toLowerCase();
    const hit = DISTRICTS.find(([d]) => hay.includes(d.toLowerCase()))
      ?? (district ? DISTRICTS.find(([d]) => district.toLowerCase().includes(d.toLowerCase())) : undefined);
    return hit ? { lat: hit[1], lng: hit[2] } : null;
  }

  /** Per-district cold-chain health roll-up (from the full-state demo network) for choropleth shading. */
  districtShades(): DistrictShade[] {
    const net = this.network();
    return DISTRICTS.map(([district, lat, lng]) => {
      const inDistrict = net.stores.filter(s => s.district === district);
      const units = inDistrict.reduce((n, s) => n + s.units.length, 0);
      const alarms = inDistrict.reduce((n, s) => n + s.alarms, 0);
      return {
        district, lat, lng,
        stores: inDistrict.length, units, alarms,
        alarmRatio: units ? alarms / units : 0
      };
    });
  }

  network(): GeoNetwork {
    if (this.cache) return this.cache;
    const stores: Store[] = [];
    const fieldForce: FieldForce[] = [];

    DISTRICTS.forEach(([district, lat, lng], di) => {
      const rnd = mulberry32(1000 + di * 97);
      const pick = <T,>(arr: T[]) => arr[Math.floor(rnd() * arr.length)];
      const round = (n: number, d = 1) => Math.round(n * 10 ** d) / 10 ** d;

      // ---- two stores per district ----
      stores.push(this.buildStore(district, di, 0, 'District Vaccine Store',
        lat + (rnd() - 0.5) * 0.05, lng + (rnd() - 0.5) * 0.05, rnd, pick, round));
      stores.push(this.buildStore(district, di, 1, 'Block Supply Store',
        lat + (rnd() - 0.5) * 0.18, lng + (rnd() - 0.5) * 0.18, rnd, pick, round));

      // ---- one field-force unit per district ----
      const carrierTemp = round(2 + rnd() * 8);            // 2–10°C
      const carrierStatus = carrierTemp > 8 ? 'alarm' : carrierTemp > 7 ? 'warn' : 'ok';
      const battery = Math.floor(35 + rnd() * 64);
      const humidity = Math.floor(35 + rnd() * 45);
      const role = pick(['Veterinary Field Officer', 'MVU Team', 'Block Vaccinator', 'AI Worker']);
      const sensors: Sensor[] = [
        { kind: 'Vaccine-carrier temp', value: `${carrierTemp} °C`, status: carrierStatus },
        { kind: 'Battery', value: `${battery} %`, status: battery < 40 ? 'warn' : 'ok' },
        { kind: 'GPS lock', value: `${round(lat, 3)}, ${round(lng, 3)}`, status: 'ok' },
        { kind: 'Carrier humidity', value: `${humidity} %RH`, status: humidity > 70 ? 'warn' : 'ok' }
      ];
      const toolbox: Toolbox = {
        id: `TBX-${di}`,
        code: `TBX-${district.slice(0, 3).toUpperCase()}-${100 + di}`,
        model: pick(['SmartVax Carrier v2', 'ColdGuard IoT Box', 'VaxTrack Pro']),
        sensors,
        contents: [
          { item: 'Vaccine carrier (cold box)', qty: 1, ok: carrierStatus !== 'alarm' },
          { item: 'Conditioned ice packs', qty: pick([4, 6, 8]), ok: carrierStatus !== 'alarm' },
          { item: 'FMD vaccine vials (loaded)', qty: pick([20, 30, 40]), ok: true },
          { item: 'Disposable syringes', qty: pick([50, 100]), ok: true },
          { item: 'AI gun + sheath kit', qty: 1, ok: rnd() > 0.15 },
          { item: 'Digital thermometer', qty: 1, ok: rnd() > 0.1 },
          { item: 'Vaccination register / tablet', qty: 1, ok: true }
        ]
      };
      const status: FieldStatus = carrierStatus === 'alarm' ? 'Cold-chain alert'
        : pick(['On Duty', 'En route', 'On Duty', 'Idle']);
      fieldForce.push({
        id: `FF-${di}`,
        name: `${pick(FIRST)} ${pick(LAST)}`,
        role,
        district,
        lat: lat + (rnd() - 0.5) * 0.22,
        lng: lng + (rnd() - 0.5) * 0.22,
        phone: `+91 9${Math.floor(100000000 + rnd() * 899999999)}`,
        vehicle: role === 'MVU Team' ? 'MVU Bolero (mobile clinic)' : pick(['Motorcycle', 'e-Scooter', 'Pickup']),
        status,
        lastPing: this.minsAgo(Math.floor(rnd() * 55) + 1),
        todaysVaccinations: Math.floor(rnd() * 120),
        toolbox
      });
    });

    this.cache = { stores, fieldForce };
    return this.cache;
  }

  private buildStore(
    district: string, di: number, si: number, type: Store['type'],
    lat: number, lng: number, rnd: () => number,
    pick: <T,>(a: T[]) => T, round: (n: number, d?: number) => number
  ): Store {
    const isHub = type === 'District Vaccine Store';
    const unitDefs: { kind: ColdChainKind; make: string }[] = isHub
      ? [
          { kind: 'IceLinedRefrigerator', make: 'Vestfrost VLS-404' },
          { kind: 'IceLinedRefrigerator', make: 'Blue Star BCV-300' },
          { kind: 'DeepFreezer', make: 'Vestfrost VLS-054 (-25°C)' },
          ...(rnd() > 0.5 ? [{ kind: 'WalkInCooler' as ColdChainKind, make: 'Blue Star Walk-in 12m³' }] : [])
        ]
      : [
          { kind: 'IceLinedRefrigerator', make: 'Vestfrost VLS-054' },
          { kind: 'DeepFreezer', make: 'Blue Star CF3-200 (-20°C)' }
        ];

    const units: ColdChainUnit[] = unitDefs.map((u, ui) => {
      const frozen = u.kind === 'DeepFreezer';
      const targetMin = frozen ? -25 : 2;
      const targetMax = frozen ? -15 : 8;
      // mostly in-band; ~15% drift to warn/alarm
      const drift = rnd();
      let tempC: number;
      if (drift > 0.9) tempC = frozen ? round(-12 - rnd() * 3) : round(9 + rnd() * 3);        // alarm (too warm)
      else if (drift > 0.8) tempC = frozen ? round(-15.5 - rnd()) : round(7.5 + rnd() * 0.9); // warn (edge)
      else tempC = frozen ? round(-22 + rnd() * 5) : round(3 + rnd() * 4);                    // ok
      const status = tempC < targetMin - 0.01 || tempC > targetMax + 0.01 ? 'alarm'
        : tempC >= targetMax - 0.6 || tempC <= targetMin + 0.6 ? 'warn' : 'ok';
      const prefix = u.kind === 'DeepFreezer' ? 'DF' : u.kind === 'WalkInCooler' ? 'WIC' : 'ILR';
      return {
        id: `CCU-${di}-${si}-${ui}`,
        name: `${prefix}-${ui + 1}`,
        kind: u.kind,
        make: u.make,
        tempC, targetMin, targetMax, status,
        capacityLitres: u.kind === 'WalkInCooler' ? 12000 : frozen ? 300 : 240,
        powerBackup: pick(['Grid + Solar ILR', 'Grid + DG set', 'Grid + Solar + battery']),
        lastReading: this.minsAgo(Math.floor(rnd() * 20) + 1)
      };
    });

    const ilrs = units.filter(u => u.kind !== 'DeepFreezer');
    const dfs = units.filter(u => u.kind === 'DeepFreezer');

    // assign vaccine stock into the appropriate cold-chain unit
    const nLines = isHub ? 6 + Math.floor(rnd() * 3) : 3 + Math.floor(rnd() * 2);
    const chosen = [...VACCINES].sort(() => rnd() - 0.5).slice(0, nLines);
    const stock: VaccineStock[] = chosen.map((c, k) => {
      const unit = c.frozen ? (dfs[0] ?? units[0]) : pick(ilrs.length ? ilrs : units);
      const doses = (isHub ? 4 : 1) * (500 + Math.floor(rnd() * 40) * 100);
      const monthsToExpiry = 2 + Math.floor(rnd() * 22);
      const expiry = new Date(Date.now() + monthsToExpiry * 30 * 86400000).toISOString().slice(0, 10);
      const vvm = (rnd() > 0.85 ? 3 : rnd() > 0.5 ? 2 : 1) as 1 | 2 | 3;
      return {
        vaccine: c.v, species: c.sp,
        batch: `${c.v.split(' ')[0].toUpperCase()}-${2400 + di * 3 + k}`,
        doses, expiry, vvmStage: vvm,
        unitId: unit.id, unitName: unit.name
      };
    });

    const districtCode = district.replace(/[^A-Za-z]/g, '').slice(0, 3).toUpperCase();
    return {
      id: `ST-${di}-${si}`,
      code: `${districtCode}-${isHub ? 'DVS' : 'BSS'}-${100 + si}`,
      name: `${district} ${isHub ? 'District Vaccine Store' : 'Block Supply Store'}`,
      type, district, lat, lng,
      incharge: `${pick(FIRST)} ${pick(LAST)}`,
      designation: isHub ? 'Chief Veterinary Officer (i/c)' : 'Veterinary Pharmacist (i/c)',
      phone: `+91 9${Math.floor(100000000 + rnd() * 899999999)}`,
      address: `${isHub ? 'CVO Office Campus' : 'Block Veterinary Hospital'}, ${district}, Uttar Pradesh`,
      units, stock,
      totalDoses: stock.reduce((s, x) => s + x.doses, 0),
      alarms: units.filter(u => u.status === 'alarm').length
    };
  }

  private minsAgo(m: number): string {
    return new Date(Date.now() - m * 60000).toISOString();
  }
}
