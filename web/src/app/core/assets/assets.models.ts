export type AssetCategory =
  | 'DiagnosticEquipment' | 'ColdChainEquipment' | 'SurgicalInstrument'
  | 'LabEquipment' | 'AiEquipment' | 'Vehicle' | 'ItHardware' | 'Furniture' | 'Other';

export type AssetStatus =
  | 'Active' | 'UnderMaintenance' | 'BreakdownReported' | 'Condemned' | 'Disposed';

export type AssetCondition = 'New' | 'Good' | 'Fair' | 'Poor';
export type AssetCriticality = 'A' | 'B' | 'C';

export type MaintenanceJobType = 'Preventive' | 'Breakdown' | 'Calibration' | 'Inspection';
export type MaintenanceJobStatus = 'Open' | 'InProgress' | 'Completed' | 'Cancelled';
export type AmcStatus = 'Active' | 'Expired' | 'Cancelled';
export type MaintenanceContractType = 'Amc' | 'Cmc';

export type IncidentImpact = 'Low' | 'Medium' | 'High';
export type IncidentUrgency = 'Low' | 'Medium' | 'High';
export type IncidentPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type IncidentProblemType =
  | 'NotPoweringOn' | 'ErraticReadings' | 'PhysicalDamage' | 'Overheating'
  | 'Leakage' | 'Consumable' | 'SoftwareOrControl' | 'Other';

export const ASSET_CRITICALITIES: AssetCriticality[] = ['A', 'B', 'C'];
export const INCIDENT_LEVELS: IncidentUrgency[] = ['Low', 'Medium', 'High'];
export const INCIDENT_PROBLEM_TYPES: IncidentProblemType[] = [
  'NotPoweringOn', 'ErraticReadings', 'PhysicalDamage', 'Overheating',
  'Leakage', 'Consumable', 'SoftwareOrControl', 'Other'
];

export const ASSET_CATEGORIES: AssetCategory[] = [
  'DiagnosticEquipment', 'ColdChainEquipment', 'SurgicalInstrument',
  'LabEquipment', 'AiEquipment', 'Vehicle', 'ItHardware', 'Furniture', 'Other'
];

export const ASSET_STATUSES: AssetStatus[] = [
  'Active', 'UnderMaintenance', 'BreakdownReported', 'Condemned', 'Disposed'
];

export interface AssetSchedule {
  id: string;
  taskDescription: string;
  frequencyDays: number;
  lastServiceDate?: string;
  nextDueDate: string;
  isActive: boolean;
  daysToDue: number;
}

export interface AssetJob {
  id: string;
  jobNumber: string;
  type: MaintenanceJobType;
  status: MaintenanceJobStatus;
  reportedAt: string;
  reportedBy?: string;
  description: string;
  assignedTo?: string;
  startedAt?: string;
  completedAt?: string;
  resolution?: string;
  cost?: number;
  impact?: IncidentImpact;
  urgency?: IncidentUrgency;
  priority?: IncidentPriority;
  problemType?: IncidentProblemType;
  deadline?: string;
  slaBreached: boolean;
}

export interface AssetAmc {
  id: string;
  contractNumber: string;
  contractType: MaintenanceContractType;
  vendorName: string;
  startDate: string;
  endDate: string;
  annualCost: number;
  coverage?: string;
  status: AmcStatus;
  daysToExpiry: number;
}

export interface Asset {
  id: string;
  assetTag: string;
  name: string;
  category: AssetCategory;
  criticality: AssetCriticality;
  model?: string;
  serialNumber?: string;
  manufacturer?: string;
  warehouseId?: string;
  warehouseName?: string;
  facilityId?: string;
  facilityName?: string;
  locationNote?: string;
  supplier?: string;
  poNumber?: string;
  poDate?: string;
  invoiceNumber?: string;
  invoiceDate?: string;
  installationDate?: string;
  purchaseDate?: string;
  purchaseCost?: number;
  warrantyUntil?: string;
  calibrationDate?: string;
  calibrationDueDate?: string;
  status: AssetStatus;
  condition: AssetCondition;
  notes?: string;
  openJobs: number;
  overdueSchedules: number;
  schedules: AssetSchedule[];
  jobs: AssetJob[];
  amcContracts: AssetAmc[];
}

export interface CalibrationDueRow {
  assetId: string;
  assetTag: string;
  assetName: string;
  category: AssetCategory;
  criticality: AssetCriticality;
  calibrationDate?: string;
  calibrationDueDate: string;
  daysToDue: number;
  locationName?: string;
}

export interface MaintenanceDueRow {
  assetId: string;
  assetTag: string;
  assetName: string;
  category: AssetCategory;
  scheduleId: string;
  taskDescription: string;
  nextDueDate: string;
  daysToDue: number;
  locationName?: string;
}

export interface AssetKpi {
  totalAssets: number;
  activeAssets: number;
  underMaintenance: number;
  inBreakdown: number;
  condemned: number;
  openJobs: number;
  overduePpm: number;
  amcExpiring60Days: number;
  warrantyExpiring60Days: number;
  warrantyExpired: number;
  calibrationDue60Days: number;
  calibrationOverdue: number;
  openCriticalIncidents: number;
  slaBreachedIncidents: number;
  amcAnnualCostTotal: number;
}

export interface LogBreakdownRequest {
  description: string;
  assignedTo?: string;
  impact?: IncidentImpact;
  urgency?: IncidentUrgency;
  problemType?: IncidentProblemType;
}
export interface CompleteJobRequest { resolution: string; cost?: number; }
export interface CreateScheduleRequest { taskDescription: string; frequencyDays: number; lastServiceDate?: string; }

export interface CreateAssetRequest {
  assetTag: string;
  name: string;
  category: AssetCategory;
  criticality: AssetCriticality;
  model?: string;
  serialNumber?: string;
  manufacturer?: string;
  warehouseId?: string;
  facilityId?: string;
  locationNote?: string;
  supplier?: string;
  poNumber?: string;
  poDate?: string;
  invoiceNumber?: string;
  invoiceDate?: string;
  installationDate?: string;
  purchaseDate?: string;
  purchaseCost?: number;
  warrantyUntil?: string;
  calibrationDate?: string;
  calibrationDueDate?: string;
  condition: AssetCondition;
  notes?: string;
}

export interface CreateAmcRequest {
  contractNumber: string;
  contractType: MaintenanceContractType;
  vendorName: string;
  startDate: string;
  endDate: string;
  annualCost: number;
  coverage?: string;
}
