export interface GetHistoryQuery {
  page?: number;
  pageSize?: number;
  status?: string;
  modelId?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface AnalysisHistoryItemDto {
  analysisId: string;
  imageId: string;
  originalImageUrl: string;
  status: string;
  modelCount: number;
  completedModelCount: number;
  failedModelCount: number;
  createdAt: string;
  completedAt?: string;
  // Augmented UI fields
  fileName?: string;
  modelName?: string;
  detectionCount?: number;
}

export interface PaginationMetadataDto {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface AnalysisHistoryResponse {
  items: AnalysisHistoryItemDto[];
  pagination: PaginationMetadataDto;
}
