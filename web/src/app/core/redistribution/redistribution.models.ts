export type RedistributionUrgency = 'Routine' | 'Watch' | 'Urgent';

export interface RedistributionSuggestion {
  donorBatchId: string;
  batchNumber: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  coldChainRequired: boolean;
  sourceWarehouseId: string;
  sourceWarehouseCode: string;
  sourceWarehouseName: string;
  donorQuantity: number;
  expiryDate: string;
  daysToExpiry: number;
  recipientWarehouseId: string;
  recipientWarehouseCode: string;
  recipientWarehouseName: string;
  recipientExistingStock: number;
  suggestedQuantity: number;
  urgency: RedistributionUrgency;
  rationale: string;
}

export interface CreateRedistributionIndentRequest {
  donorBatchId: string;
  recipientWarehouseId: string;
  quantity: number;
}

export interface CreateRedistributionIndentResponse {
  indentId: string;
  indentNumber: string;
}
