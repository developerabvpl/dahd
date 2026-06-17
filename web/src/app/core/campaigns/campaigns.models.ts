export type SchemeBucket =
  | 'NadcpFmd' | 'NadcpBrucellosis'
  | 'AscadHs' | 'AscadBq' | 'AscadCsf' | 'AscadRabies'
  | 'AscadRanikhet' | 'AscadGumboro' | 'AscadFowlPox'
  | 'PashuAushadhi' | 'Other';

export type CampaignStatus = 'Planned' | 'Active' | 'Completed' | 'Cancelled';

export interface ProcurementCampaign {
  id: string;
  code: string;
  name: string;
  scheme: SchemeBucket;
  drugId: string;
  drugCode: string;
  drugName: string;
  windowStart: string;
  windowEnd: string;
  leadDays: number;
  targetDoseCount: number;
  targetCohortDescription?: string;
  status: CampaignStatus;
  notes?: string;
  indentsDraftedAt?: string;
  indentsDraftedCount: number;
  daysToWindowStart: number;
  daysToProcurementStart: number;
}

export interface DraftCampaignIndentsRequest {
  sourceWarehouseId: string;
  quantityPerDestination: number;
}

export interface DraftCampaignIndentsResponse {
  indentsCreated: number;
  totalQuantityRequested: number;
}
