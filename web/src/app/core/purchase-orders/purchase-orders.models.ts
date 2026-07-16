export type PoStatus =
  | 'Draft' | 'Issued' | 'Acknowledged' | 'PartiallyReceived' | 'Received' | 'Cancelled';

export const PO_STATUSES: PoStatus[] =
  ['Draft', 'Issued', 'Acknowledged', 'PartiallyReceived', 'Received', 'Cancelled'];

export interface PoLine {
  id: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  unitOfMeasure: string;
  orderedQuantity: number;
  unitRate: number;
  receivedQuantity: number;
  lineTotal: number;
  remarks?: string;
}

export interface PurchaseOrder {
  id: string;
  poNumber: string;
  vendorId?: string;
  vendorName?: string;
  rateContractId?: string;
  rateContractNumber?: string;
  destinationWarehouseId: string;
  destinationWarehouseName: string;
  status: PoStatus;
  expectedDelivery?: string;
  issuedAt?: string;
  acknowledgedAt?: string;
  fullyReceivedAt?: string;
  cancelledAt?: string;
  cancelReason?: string;
  remarks?: string;
  totalAmount: number;
  lines: PoLine[];
}

export interface CreatePoLineRequest {
  drugId: string;
  orderedQuantity: number;
  unitRate: number;
  remarks?: string;
}

export interface CreatePoRequest {
  vendorId?: string;
  vendorName?: string;
  rateContractId?: string;
  destinationWarehouseId: string;
  expectedDelivery?: string;
  remarks?: string;
  lines: CreatePoLineRequest[];
}

export interface GrnLineRequest {
  lineId: string;
  quantity: number;
  batchNumber: string;
  manufactureDate: string;
  expiryDate: string;
  manufacturer?: string;
}

export interface GrnRequest {
  warehouseId?: string;
  lines: GrnLineRequest[];
}
