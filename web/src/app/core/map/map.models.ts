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
