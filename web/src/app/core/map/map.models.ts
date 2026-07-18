// Geo-network model for the Map view. This is a demo/visualisation dataset
// (deterministically generated in map.service) — "assume every district has two
// stores" per the requirement — modelling the vaccine cold-chain network and the
// sensor-equipped field force across Uttar Pradesh.

export type ColdChainKind = 'IceLinedRefrigerator' | 'DeepFreezer' | 'WalkInCooler';
export type UnitStatus = 'ok' | 'warn' | 'alarm';

export interface ColdChainUnit {
  id: string;
  name: string;             // e.g. "ILR-1"
  kind: ColdChainKind;
  make: string;
  tempC: number;            // current sensor reading
  targetMin: number;        // acceptable band
  targetMax: number;
  status: UnitStatus;       // derived from band
  capacityLitres: number;
  powerBackup: string;      // "Grid + Solar ILR", "DG set", ...
  lastReading: string;      // ISO
}

export interface VaccineStock {
  vaccine: string;          // "FMD Vaccine (Raksha)"
  species: string;          // "Cattle & Buffalo"
  batch: string;
  doses: number;
  expiry: string;           // ISO date
  vvmStage: 1 | 2 | 3 | 4;  // vaccine vial monitor
  unitId: string;           // which cold-chain unit it sits in
  unitName: string;
}

export type StoreType = 'District Vaccine Store' | 'Block Supply Store';

export interface Store {
  id: string;
  code: string;
  name: string;
  type: StoreType;
  district: string;
  lat: number;
  lng: number;
  incharge: string;
  designation: string;
  phone: string;
  address: string;
  units: ColdChainUnit[];
  stock: VaccineStock[];
  totalDoses: number;
  alarms: number;           // count of units out of band
}

export type SensorStatus = 'ok' | 'warn' | 'alarm';

export interface Sensor {
  kind: string;             // "Vaccine-carrier temp", "GPS", "Battery", "Humidity"
  value: string;            // formatted with unit
  status: SensorStatus;
}

export interface ToolboxItem {
  item: string;
  qty: number;
  ok: boolean;
}

export interface Toolbox {
  id: string;
  code: string;
  model: string;            // "SmartVax Carrier v2"
  sensors: Sensor[];
  contents: ToolboxItem[];
}

export type FieldStatus = 'On Duty' | 'En route' | 'Idle' | 'Cold-chain alert';

export interface FieldForce {
  id: string;
  name: string;
  role: string;             // "Veterinary Field Officer", "MVU Team", "Vaccinator"
  district: string;
  lat: number;
  lng: number;
  phone: string;
  vehicle: string;
  status: FieldStatus;
  lastPing: string;         // ISO
  todaysVaccinations: number;
  toolbox: Toolbox;
}

export interface GeoNetwork {
  stores: Store[];
  fieldForce: FieldForce[];
}

// ---- Live layer (real backend warehouses/stock) ----

export interface MapStockLine {
  drug: string;
  code: string;
  isVaccine: boolean;
  coldChainRequired: boolean;
  storageTempMin?: number;
  storageTempMax?: number;
  batch: string;
  quantity: number;
  unit: string;
  expiry: string;
  daysToExpiry: number;
}

export interface MapColdUnit {
  assetTag: string;
  name: string;
  model?: string;
  status: string;
  condition: string;
}

export interface MapWarehouse {
  id: string;
  code: string;
  name: string;
  type: string;
  district?: string;
  division?: string;
  coldChainCapable: boolean;
  incharge?: string;
  phone?: string;
  address?: string;
  totalStock: number;
  stockLines: number;
  coldChainUnits: MapColdUnit[];
  stock: MapStockLine[];
  // populated client-side after geocoding
  lat?: number;
  lng?: number;
}

// Per-district roll-up used for choropleth shading.
export interface DistrictShade {
  district: string;
  lat: number;
  lng: number;
  stores: number;
  alarms: number;      // cold-chain units in alarm across the district
  units: number;
  alarmRatio: number;  // 0..1
}
