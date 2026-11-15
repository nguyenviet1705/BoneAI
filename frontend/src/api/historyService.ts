import { apiClient } from './apiClient';
import type { 
  GetHistoryQuery, 
  AnalysisHistoryResponse 
} from '../types/history.types';
import type { AnalysisResultDto } from '../types/analysis.types';

export const historyService = {
  /**
   * Lấy danh sách lịch sử phân tích có phân trang và lọc
   */
  async getHistory(query: GetHistoryQuery = {}): Promise<AnalysisHistoryResponse> {
    const params = new URLSearchParams();
    if (query.page) params.append('page', query.page.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.status) params.append('status', query.status);
    if (query.modelId) params.append('modelId', query.modelId);
    if (query.sortBy) params.append('sortBy', query.sortBy);
    if (query.sortOrder) params.append('sortOrder', query.sortOrder);

    const response = await apiClient.get<AnalysisHistoryResponse>(`/history?${params.toString()}`);
    return response.data;
  },

  /**
   * Lấy chi tiết một phiên phân tích trong lịch sử
   */
  async getHistoryDetail(id: string): Promise<AnalysisResultDto> {
    const response = await apiClient.get<AnalysisResultDto>(`/history/${id}`);
    return response.data;
  },

  /**
   * Xóa mềm một lượt phân tích khỏi lịch sử
   */
  async deleteHistory(id: string): Promise<void> {
    await apiClient.delete(`/history/${id}`);
  }
};
