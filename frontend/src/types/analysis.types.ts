export const AnalysisMode = {
  SingleModel: 0,
  MultiModel: 1,
  CompareAll: 2,
} as const;

export type AnalysisMode = (typeof AnalysisMode)[keyof typeof AnalysisMode];

export type AnalysisStatus =
  | 'Pending'
  | 'Processing'
  | 'Completed'
  | 'PartiallyCompleted'
  | 'Failed';

export interface SubmitAnalysisRequest {
  xRayImageId: string;
  mode?: AnalysisMode;
  aiModelId?: string;
  selectedModelIds?: string[];
  idempotencyKey?: string;
}

export interface AnalysisSubmissionResponse {
  analysisId: string;
  xRayImageId: string;
  mode: string;
  status: AnalysisStatus;
  runs: Array<{
    runId: string;
    modelId: string;
    modelKey: string;
    status: string;
  }>;
  createdAt: string;
}

export interface BoundingBoxDto {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface DetectionItemDto {
  detectionId: string;
  classLabel: string;
  confidenceScore: number;
  boundingBox: BoundingBoxDto;
}

export interface ModelInfoDto {
  modelId: string;
  modelName: string;
  modelKey: string;
  version: string;
  framework: string;
}

export interface OriginalImageInfoDto {
  imageId: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  originalImageUrl: string;
}

export interface AnalysisRunResultDto {
  runId: string;
  analysisId: string;
  model: ModelInfoDto;
  status: AnalysisStatus;
  processingTime?: number;
  processingTimeMs?: number;
  startedAt?: string;
  completedAt?: string;
  errorMessage?: string;
  resultImageUrl?: string;
  detectionCount: number;
  detections: DetectionItemDto[];
}

export interface AnalysisResultDto {
  analysisId: string;
  imageId: string;
  originalImage: OriginalImageInfoDto;
  overallStatus: AnalysisStatus;
  mode: string;
  createdAt: string;
  totalRunsCount: number;
  completedRunsCount: number;
  failedRunsCount: number;
  runs: AnalysisRunResultDto[];
}

export interface ModelComparisonItemDto {
  runId: string;
  modelId: string;
  modelName: string;
  modelKey: string;
  version: string;
  status: string;
  processingTime?: number;
  detectionCount: number;
  averageConfidence?: number;
  detections: DetectionItemDto[];
  resultImageUrl?: string;
  errorMessage?: string;
}

export interface AnalysisComparisonDto {
  analysisId: string;
  imageId: string;
  overallStatus: string;
  originalImageUrl: string;
  models: ModelComparisonItemDto[];
}
