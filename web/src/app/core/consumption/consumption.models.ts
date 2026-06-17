export interface ConsumptionForecastRow {
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  drugId: string;
  drugCode: string;
  drugName: string;
  unitOfMeasure: string;
  lookbackDays: number;
  lookbackConsumption: number;
  dailyVelocity: number;
  forecastDays: number;
  projectedNeed: number;
  currentStock: number;
  shortfall: number;
  safetyStock: number;
}

export interface DraftQuarterlyIndentRequest {
  recipientWarehouseId: string;
  sourceWarehouseId: string;
  lookbackDays: number;
  forecastDays: number;
  safetyMultiplier: number;
}

export interface DraftQuarterlyIndentResponse {
  indentId?: string;
  indentNumber?: string;
  lineCount: number;
  totalQuantity: number;
}
