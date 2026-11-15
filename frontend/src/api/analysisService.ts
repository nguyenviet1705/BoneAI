import { apiClient } from './apiClient';
import type {
  SubmitAnalysisRequest,
  AnalysisSubmissionResponse,
  AnalysisResultDto,
  AnalysisRunResultDto,
  AnalysisComparisonDto
} from '../types/analysis.types';

export const analysisService = {
  /**
   * Khởi tạo phân tích ảnh X-quang (Single Model hoặc Multi Model)
   */
  async submitAnalysis(request: SubmitAnalysisRequest): Promise<AnalysisSubmissionResponse> {
    const response = await apiClient.post<AnalysisSubmissionResponse>('/analysis', request);
    return response.data;
  },

  /**
   * Lấy trạng thái tổng thể của lượt phân tích
   */
  async getAnalysisStatus(analysisId: string): Promise<AnalysisResultDto> {
    const response = await apiClient.get<AnalysisResultDto>(`/results/${analysisId}`);
    return response.data;
  },

  /**
   * Lấy kết quả chi tiết của lượt phân tích (bao gồm tất cả runs)
   */
  async getAnalysisResult(analysisId: string): Promise<AnalysisResultDto> {
    const response = await apiClient.get<AnalysisResultDto>(`/results/${analysisId}`);
    return response.data;
  },

  /**
   * Lấy kết quả của một AnalysisRun cụ thể
   */
  async getAnalysisRunResult(runId: string): Promise<AnalysisRunResultDto> {
    const response = await apiClient.get<AnalysisRunResultDto>(`/results/runs/${runId}`);
    return response.data;
  },

  /**
   * Lấy dữ liệu bảng so sánh giữa các model
   */
  async getAnalysisComparison(analysisId: string): Promise<AnalysisComparisonDto> {
    const response = await apiClient.get<AnalysisComparisonDto>(`/results/${analysisId}/comparison`);
    return response.data;
  },

  /**
   * Đường dẫn ảnh kết quả vẽ bounding box của một run
   */
  getRunResultImageUrl(runId: string): string {
    return `/api/results/runs/${runId}/image`;
  }
};
