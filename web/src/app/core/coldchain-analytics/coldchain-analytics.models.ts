export interface DeviceAnalyticsRow {
  warehouseId: string;
  warehouseCode: string;
  warehouseName: string;
  deviceId: string;
  deviceName: string;
  readingCount: number;
  breachCount: number;
  minC: number;
  maxC: number;
  meanC: number;
  mktC: number;
  timeOutOfSpecPct: number;
  firstReading?: string;
  lastReading?: string;
}

export interface BreachHourMatrixCell {
  dayOfWeek: number;
  hour: number;
  breachCount: number;
}
