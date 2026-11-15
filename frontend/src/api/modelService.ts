import { apiClient } from './apiClient';
import type { AIModel } from '../types/model.types';

export const modelService = {
  /**
   * Lấy danh sách các AI model đang hoạt động từ backend
   */
  async getModels(): Promise<AIModel[]> {
    const response = await apiClient.get<AIModel[]>('/analysis/models');
    return response.data;
  },

  /**
   * Kiểm tra trạng thái AI Service
   */
  async checkAIServiceHealth(): Promise<{ liveness: string; readiness: string; checkedAt: string }> {
    const response = await apiClient.get<{ liveness: string; readiness: string; checkedAt: string }>('/analysis/ai-service-health');
    return response.data;
  },
};
