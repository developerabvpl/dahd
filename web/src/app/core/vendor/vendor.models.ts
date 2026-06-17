export type VendorStatus =
  | 'Draft' | 'Submitted' | 'UnderReview' | 'Approved' | 'Rejected' | 'Blacklisted';

export type VendorDocumentType =
  | 'DrugLicence' | 'Gmp' | 'Iso9001' | 'Iso13485'
  | 'Bis' | 'Cdsco' | 'ManufacturerAuthorization'
  | 'PastPerformance' | 'Gst' | 'Pan' | 'Udyam' | 'Other';

export const VENDOR_DOCUMENT_TYPES: VendorDocumentType[] = [
  'DrugLicence', 'Gmp', 'Iso9001', 'Iso13485',
  'Bis', 'Cdsco', 'ManufacturerAuthorization',
  'PastPerformance', 'Gst', 'Pan', 'Udyam', 'Other'
];

export const VENDOR_CATEGORIES = [
  { value: 1,   label: 'Drugs' },
  { value: 2,   label: 'Vaccines' },
  { value: 4,   label: 'Equipment' },
  { value: 8,   label: 'Cold Chain' },
  { value: 16,  label: 'Lab Consumables' },
  { value: 32,  label: 'AI Consumables' },
  { value: 64,  label: 'MVU Services' },
  { value: 128, label: 'Gaushala Capex' },
  { value: 256, label: 'Biogas EPC' },
  { value: 512, label: 'IT Services' }
] as const;

export interface VendorDocument {
  id: string;
  documentType: VendorDocumentType;
  fileName: string;
  storageRef?: string;
  issuingAuthority?: string;
  certificateNumber?: string;
  issuedDate?: string;
  expiryDate?: string;
  notes?: string;
  uploadedAt: string;
  uploadedBy?: string;
}

export interface Vendor {
  id: string;
  userId: string;
  username: string;
  legalName: string;
  tradeName?: string;
  contactPerson: string;
  contactEmail: string;
  contactPhone: string;
  city?: string;
  state?: string;
  pincode?: string;
  gstin?: string;
  pan?: string;
  udyamRegNumber?: string;
  isManufacturer: boolean;
  isMsme: boolean;
  categories: number;
  status: VendorStatus;
  submittedAt?: string;
  underReviewAt?: string;
  approvedAt?: string;
  rejectedAt?: string;
  blacklistedAt?: string;
  reviewedBy?: string;
  reviewRemarks?: string;
  scheduledInspectionAt?: string;
  inspectionRemarks?: string;
  blacklistReason?: string;
  empanelmentValidUntil?: string;
  documents: VendorDocument[];
}

export interface VendorRegistrationRequest {
  username: string;
  password: string;
  legalName: string;
  tradeName?: string;
  contactPerson: string;
  contactEmail: string;
  contactPhone: string;
  address?: string;
  city?: string;
  state?: string;
  pincode?: string;
  gstin?: string;
  pan?: string;
  udyamRegNumber?: string;
  isManufacturer: boolean;
  isMsme: boolean;
  categories: number;
}

export interface UploadVendorDocumentRequest {
  documentType: VendorDocumentType;
  fileName: string;
  storageRef?: string;
  issuingAuthority?: string;
  certificateNumber?: string;
  issuedDate?: string;
  expiryDate?: string;
  notes?: string;
}

export interface VendorReviewActionRequest {
  remarks?: string;
  scheduledInspectionAt?: string;
  empanelmentValidUntil?: string;
  blacklistReason?: string;
}
