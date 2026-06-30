export type AssetCategory =
  | 'DiagnosticEquipment' | 'ColdChainEquipment' | 'SurgicalInstrument'
  | 'LabEquipment' | 'AiEquipment' | 'Vehicle' | 'ItHardware' | 'Furniture' | 'Other';

export type AssetStatus =
  | 'Active' | 'UnderMaintenance' | 'BreakdownReported' | 'Condemned' | 'Disposed';

export type AssetCondition = 'New' | 'Good' | 'Fair' | 'Poor';

export type MaintenanceJobType = 'Preventive' | 'Breakdown' | 'Calibration' | 'Inspection';
export type MaintenanceJobStatus = 'Open' | 'InProgress' | 'Completed' | 'Cancelled';
export type AmcStatus = 'Active' | 'Expired' | 'Cancelled';

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
}

export interface AssetAmc {
  id: string;
  contractNumber: string;
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
  model?: string;
  serialNumber?: string;
  manufacturer?: string;
  warehouseId?: string;
  warehouseName?: string;
  facilityId?: string;
  facilityName?: string;
  locationNote?: string;
  purchaseDate?: string;
  purchaseCost?: number;
  warrantyUntil?: string;
  status: AssetStatus;
  condition: AssetCondition;
  notes?: string;
  openJobs: number;
  overdueSchedules: number;
  schedules: AssetSchedule[];
  jobs: AssetJob[];
  amcContracts: AssetAmc[];
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
}

export interface LogBreakdownRequest { description: string; assignedTo?: string; }
export interface CompleteJobRequest { resolution: string; cost?: number; }
export interface CreateScheduleRequest { taskDescription: string; frequencyDays: number; lastServiceDate?: string; }
