export type RateContractStatus = 'Draft' | 'Active' | 'Expired' | 'Cancelled';

export type RateContractCategory =
  | 'Medicines' | 'Vaccines' | 'Equipment' | 'ColdChain'
  | 'LabConsumables' | 'AiConsumables' | 'Services' | 'Other';

export interface RateContractItem {
  id: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  unitOfMeasure: string;
  vendorId?: string;
  vendorName?: string;
  unitRate: number;
  packSize?: string;
  minOrderQuantity?: number;
  remarks?: string;
}

export interface RateContract {
  id: string;
  contractNumber: string;
  title: string;
  category: RateContractCategory;
  leadBody: string;
  validFrom: string;
  validUntil: string;
  status: RateContractStatus;
  sourceUrl?: string;
  notes?: string;
  itemCount: number;
  daysToExpiry: number;
  items: RateContractItem[];
}

export interface CheapestRateRow {
  drugId: string;
  drugCode: string;
  drugName: string;
  rateContractId: string;
  contractNumber: string;
  contractTitle: string;
  vendorId?: string;
  vendorName?: string;
  unitRate: number;
  packSize?: string;
  contractValidUntil: string;
}
