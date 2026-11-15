export interface ImageResponse {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  createdAt: string;
  fileUrl: string;
}

export interface ImagePaginationResponse {
  items: ImageResponse[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface SelectedImageInfo {
  id?: string;
  file?: File;
  previewUrl: string;
  fileName: string;
  fileSizeFormatted: string;
  fileSizeBytes: number;
  width?: number;
  height?: number;
  isUploaded: boolean;
}
