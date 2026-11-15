import { apiClient } from './apiClient';
import type { ImagePaginationResponse, ImageResponse } from '../types/image.types';

export const imageService = {
  /**
   * Tải ảnh X-quang lên hệ thống (multipart/form-data)
   */
  async uploadImage(file: File): Promise<ImageResponse> {
    const formData = new FormData();
    formData.append('File', file);

    const response = await apiClient.post<ImageResponse>('/images/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return response.data;
  },

  /**
   * Lấy danh sách ảnh đã upload (hỗ trợ phân trang)
   */
  async getImages(page = 1, pageSize = 10): Promise<ImagePaginationResponse> {
    const response = await apiClient.get<ImagePaginationResponse>('/images', {
      params: { page, pageSize },
    });
    return response.data;
  },

  /**
   * Lấy chi tiết thông tin ảnh
   */
  async getImageById(id: string): Promise<ImageResponse> {
    const response = await apiClient.get<ImageResponse>(`/images/${id}`);
    return response.data;
  },

  /**
   * Xóa ảnh X-quang nếu chưa bị ràng buộc bởi Analysis
   */
  async deleteImage(id: string): Promise<void> {
    await apiClient.delete(`/images/${id}`);
  },

  /**
   * Lấy URL trực tiếp để hiển thị file ảnh
   */
  getImageFileUrl(id: string): string {
    const baseUrl = import.meta.env.VITE_API_BASE_URL || '/api';
    return `${baseUrl}/images/${id}/file`;
  },
};
