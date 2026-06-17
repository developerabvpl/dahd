export type WarehouseType = 'Central' | 'Divisional' | 'District' | 'Facility';
export type FacilityType =
  | 'VeterinaryHospital' | 'Polyclinic' | 'RuralDispensary'
  | 'AiSubCentre' | 'MobileVeterinaryUnit' | 'Gaushala';
export type BatchStatus = 'InTransit' | 'InStore' | 'Issued' | 'Expired' | 'Wasted' | 'Recalled';
export type IndentStatus = 'Draft' | 'Submitted' | 'Approved' | 'Issued' | 'Received' | 'Closed' | 'Rejected';
export type FormularyClass =
  | 'Antibiotic' | 'Antiparasitic' | 'Vaccine' | 'Vitamin' | 'Hormone'
  | 'Mineral' | 'Analgesic' | 'Anaesthetic' | 'Antiseptic' | 'Other';
export type AnimalSpecies = 'Cattle' | 'Buffalo' | 'Sheep' | 'Goat' | 'Pig' | 'Poultry' | 'Equine' | 'Other';

export interface Drug {
  id: string;
  code: string;
  name: string;
  genericName?: string;
  formularyClass: FormularyClass;
  isVaccine: boolean;
  coldChainRequired: boolean;
  storageTempMinCelsius?: number;
  storageTempMaxCelsius?: number;
  unitOfMeasure: string;
  scheduleClass?: string;
  manufacturer?: string;
  isActive: boolean;
}

export interface Warehouse {
  id: string;
  code: string;
  name: string;
  type: WarehouseType;
  parentWarehouseId?: string;
  divisionName?: string;
  districtName?: string;
  coldChainCapable: boolean;
  inchargeName?: string;
  contactPhone?: string;
  isActive: boolean;
}

export interface Facility {
  id: string;
  code: string;
  name: string;
  type: FacilityType;
  divisionName?: string;
  districtName?: string;
  blockName?: string;
  inchargeName?: string;
  contactPhone?: string;
  mvuVehicleRegistration?: string;
  isActive: boolean;
}

export interface Batch {
  id: string;
  drugId: string;
  drugName: string;
  batchNumber: string;
  manufactureDate: string;
  expiryDate: string;
  manufacturer?: string;
  quantity: number;
  unitCost: number;
  currentWarehouseId: string;
  warehouseName: string;
  status: BatchStatus;
  daysToExpiry: number;
}

export interface IndentLine {
  id: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  requestedQuantity: number;
  approvedQuantity?: number;
  issuedQuantity?: number;
  receivedQuantity?: number;
  issuedBatchId?: string;
  remarks?: string;
}

export interface Indent {
  id: string;
  indentNumber: string;
  raisedByWarehouseId: string;
  raisedByWarehouseName: string;
  fulfilledByWarehouseId: string;
  fulfilledByWarehouseName: string;
  status: IndentStatus;
  submittedAt?: string;
  approvedAt?: string;
  issuedAt?: string;
  receivedAt?: string;
  remarks?: string;
  lines: IndentLine[];
}

export interface ColdChainLog {
  id: string;
  warehouseId: string;
  warehouseName: string;
  deviceId: string;
  deviceName: string;
  readingAt: string;
  temperatureCelsius: number;
  isBreach: boolean;
  remarks?: string;
  acknowledgedAt?: string;
  acknowledgedBy?: string;
  correctiveAction?: string;
  affectedBatchIdsJson?: string;
}

export interface AcknowledgeBreachRequest {
  correctiveAction: string;
  affectedBatchIds?: string[];
}

export interface StockByDrugRow {
  drugId: string;
  drugCode: string;
  drugName: string;
  unitOfMeasure: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  totalQuantity: number;
  batchCount: number;
  batchesExpired: number;
  batchesNearExpiry30Days: number;
}

export interface DispenseEvent {
  id: string;
  batchId: string;
  drugName: string;
  batchNumber: string;
  quantity: number;
  facilityId: string;
  facilityName: string;
  animalEarTag?: string;
  animalSpecies: AnimalSpecies;
  ownerName?: string;
  ownerMobile?: string;
  diagnosis?: string;
  vetName?: string;
  dispensedAt: string;
}

export interface DashboardKpi {
  totalDrugs: number;
  totalVaccines: number;
  totalWarehouses: number;
  totalFacilities: number;
  activeBatches: number;
  batchesNearExpiry30Days: number;
  batchesExpired: number;
  openIndents: number;
  coldChainBreachesLast24h: number;
  dispenseEventsLast30Days: number;
}
