export interface ParLevelRow {
  id: string;
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  unitOfMeasure: string;
  parQuantity: number;
  reorderToQuantity?: number;
  currentStock: number;
  shortfall: number;
  belowPar: boolean;
  isActive: boolean;
}

export interface UpsertParLevelRequest {
  warehouseId: string;
  drugId: string;
  parQuantity: number;
  reorderToQuantity?: number;
}

export interface ParAutoIndentRequest { recipientWarehouseId: string; sourceWarehouseId: string; }
export interface ParAutoIndentResponse { indentId?: string; indentNumber?: string; lineCount: number; totalQuantity: number; }
