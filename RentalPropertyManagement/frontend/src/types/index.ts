// Enums
export enum RoomStatus {
  Available = 0,
  Occupied = 1,
  Maintenance = 2,
}

export enum PaymentType {
  Rent = 0,
  SecurityDeposit = 1,
  Electricity = 2,
  Water = 3,
  Maintenance = 4,
  Miscellaneous = 5,
}

export enum PaymentMethod {
  Cash = 0,
  BankTransfer = 1,
  OnlinePayment = 2,
  Check = 3,
}

export enum MaintenanceStatus {
  Pending = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

export enum MaintenanceType {
  Plumbing = 0,
  Electrical = 1,
  Cleaning = 2,
  Repair = 3,
  Replacement = 4,
  Painting = 5,
  Other = 6,
}

export enum MaintenancePriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Urgent = 3,
}

// Interfaces
export interface Tenant {
  id: number;
  fullName: string;
  phoneNumber: string;
  email: string;
  permanentAddress: string;
  moveInDate: string;
  moveOutDate?: string;
  securityDeposit: number;
  isActive: boolean;
  roomId: number;
  roomNumber?: string;
  createdAt: string;
}

export interface CreateTenant {
  fullName: string;
  phoneNumber: string;
  email: string;
  permanentAddress: string;
  moveInDate: string;
  securityDeposit: number;
  roomId: number;
}

export interface Room {
  id: number;
  roomNumber: string;
  monthlyRent: number;
  status: RoomStatus;
  electricMeterNumber: string;
  tenantCount: number;
  currentTenantName?: string;
  createdAt: string;
}

export interface Payment {
  id: number;
  tenantId: number;
  tenantName: string;
  roomNumber: string;
  type: PaymentType;
  amount: number;
  paymentDate: string;
  method: PaymentMethod;
  transactionReference?: string;
  description?: string;
  billingPeriodStart: string;
  billingPeriodEnd: string;
  createdAt: string;
}

export interface CreatePayment {
  tenantId: number;
  type: PaymentType;
  amount: number;
  paymentDate: string;
  method: PaymentMethod;
  transactionReference?: string;
  description?: string;
  billingPeriodStart: string;
  billingPeriodEnd: string;
}

export interface ElectricityReading {
  id: number;
  roomId: number;
  roomNumber: string;
  reading: number;
  readingDate: string;
  unitsConsumed?: number;
  billAmount?: number;
  unitRate: number;
  notes?: string;
  createdAt: string;
}

export interface MaintenanceRequest {
  id: number;
  roomId?: number;
  roomNumber?: string;
  tenantId?: number;
  tenantName?: string;
  title: string;
  description: string;
  type: MaintenanceType;
  status: MaintenanceStatus;
  priority: MaintenancePriority;
  estimatedCost?: number;
  actualCost?: number;
  requestDate: string;
  completedDate?: string;
  notes?: string;
  chargeToTenant: boolean;
  createdAt: string;
}

export interface Dashboard {
  totalRooms: number;
  occupiedRooms: number;
  availableRooms: number;
  maintenanceRooms: number;
  totalRentCollectedThisMonth: number;
  totalElectricityCollectedThisMonth: number;
  pendingPayments: number;
  upcomingMoveOutsCount: number;
  pendingMaintenanceRequests: number;
  recentPayments: RecentPayment[];
  upcomingMoveOuts: UpcomingMoveOut[];
}

export interface RecentPayment {
  tenantName: string;
  roomNumber: string;
  amount: number;
  paymentDate: string;
  paymentType: string;
}

export interface UpcomingMoveOut {
  tenantName: string;
  roomNumber: string;
  moveOutDate: string;
  daysRemaining: number;
}

export interface PaymentSummary {
  totalRentCollected: number;
  totalElectricityPayments: number;
  totalSecurityDeposits: number;
  totalMaintenancePayments: number;
  totalMiscellaneousPayments: number;
  grandTotal: number;
  fromDate: string;
  toDate: string;
}