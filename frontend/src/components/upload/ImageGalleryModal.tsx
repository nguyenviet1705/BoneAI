import React, { useEffect, useState } from 'react';
import { 
  X, 
  Trash2, 
  Check, 
  Loader2, 
  Calendar, 
  HardDrive, 
  AlertCircle,
  ChevronLeft,
  ChevronRight,
  RefreshCw,
  FolderOpen
} from 'lucide-react';
import { imageService } from '../../api/imageService';
import type { ImageResponse } from '../../types/image.types';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSelectImage: (image: ImageResponse) => void;
  selectedImageId?: string;
}

export const ImageGalleryModal: React.FC<Props> = ({
  isOpen,
  onClose,
  onSelectImage,
  selectedImageId,
}) => {
  const [images, setImages] = useState<ImageResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const fetchImages = async (targetPage = 1) => {
    setLoading(true);
    setErrorMessage(null);
    try {
      const data = await imageService.getImages(targetPage, 8);
      setImages(data.items);
      setPage(data.page);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể tải danh sách ảnh X-quang từ máy chủ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchImages(1);
    }
  }, [isOpen]);

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.stopPropagation();
    if (!window.confirm('Bạn có chắc chắn muốn xóa ảnh X-quang này khỏi hệ thống không?')) {
      return;
    }

    setDeletingId(id);
    setErrorMessage(null);
    try {
      await imageService.deleteImage(id);
      await fetchImages(page);
    } catch (err: any) {
      setErrorMessage(err.message || 'Không thể xóa ảnh. Có thể ảnh đang được sử dụng trong các lượt phân tích.');
    } finally {
      setDeletingId(null);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  const formatDate = (dateStr: string): string => {
    try {
      const d = new Date(dateStr);
      return d.toLocaleDateString('vi-VN', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateStr;
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-200">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-4xl max-h-[90vh] flex flex-col overflow-hidden">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-blue-100 text-blue-600 flex items-center justify-center">
              <FolderOpen className="w-4 h-4" />
            </div>
            <div>
              <h3 className="font-bold text-base text-slate-800">
                Thư viện ảnh X-quang
              </h3>
              <p className="text-xs text-slate-500">
                Tổng cộng {totalCount} ảnh đã được lưu trữ trên hệ thống
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              onClick={() => fetchImages(page)}
              title="Làm mới"
              className="p-2 text-slate-500 hover:text-slate-800 rounded-lg hover:bg-slate-200/60 transition-colors"
            >
              <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
            </button>
            <button
              onClick={onClose}
              className="p-2 text-slate-400 hover:text-slate-700 rounded-lg hover:bg-slate-200/60 transition-colors"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        {/* Error message */}
        {errorMessage && (
          <div className="mx-6 mt-4 p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2">
            <AlertCircle className="w-4 h-4 shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        {/* Content body */}
        <div className="flex-1 overflow-y-auto p-6 min-h-[360px]">
          {loading && images.length === 0 ? (
            <div className="h-64 flex flex-col items-center justify-center text-slate-400 gap-3">
              <Loader2 className="w-8 h-8 animate-spin text-blue-600" />
              <p className="text-xs">Đang tải danh sách ảnh...</p>
            </div>
          ) : images.length === 0 ? (
            <div className="h-64 flex flex-col items-center justify-center text-slate-400 gap-3">
              <div className="w-16 h-16 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-400">
                <FolderOpen className="w-8 h-8 stroke-[1.5]" />
              </div>
              <p className="text-sm font-medium text-slate-600">Chưa có ảnh X-quang nào trong thư viện</p>
              <p className="text-xs text-slate-400">Hãy tải ảnh mới từ máy tính của bạn</p>
            </div>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
              {images.map((img) => {
                const isSelected = img.id === selectedImageId;
                const isThisDeleting = deletingId === img.id;

                return (
                  <div
                    key={img.id}
                    onClick={() => {
                      onSelectImage(img);
                      onClose();
                    }}
                    className={`group relative rounded-xl border p-2.5 bg-white cursor-pointer transition-all duration-200 hover:shadow-md ${
                      isSelected
                        ? 'border-blue-500 ring-2 ring-blue-500/20 bg-blue-50/20'
                        : 'border-slate-200 hover:border-blue-300'
                    }`}
                  >
                    {/* Thumbnail */}
                    <div className="w-full aspect-square rounded-lg overflow-hidden bg-slate-900 border border-slate-100 relative mb-2">
                      <img
                        src={imageService.getImageFileUrl(img.id)}
                        alt={img.fileName}
                        className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
                        loading="lazy"
                      />

                      {/* Selection Badge */}
                      {isSelected && (
                        <div className="absolute top-1.5 left-1.5 w-6 h-6 rounded-full bg-blue-600 text-white flex items-center justify-center shadow-md">
                          <Check className="w-3.5 h-3.5 stroke-[3]" />
                        </div>
                      )}

                      {/* Delete Action button */}
                      <button
                        type="button"
                        onClick={(e) => handleDelete(e, img.id)}
                        disabled={isThisDeleting}
                        title="Xóa ảnh này khỏi thư viện"
                        className="absolute top-1.5 right-1.5 w-7 h-7 rounded-lg bg-black/60 hover:bg-rose-600 text-white flex items-center justify-center opacity-0 group-hover:opacity-100 transition-all duration-200 backdrop-blur-xs disabled:opacity-50"
                      >
                        {isThisDeleting ? (
                          <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        ) : (
                          <Trash2 className="w-3.5 h-3.5" />
                        )}
                      </button>
                    </div>

                    {/* Meta info */}
                    <p className="text-xs font-semibold text-slate-800 truncate" title={img.fileName}>
                      {img.fileName}
                    </p>
                    <div className="mt-1 flex items-center justify-between text-[11px] text-slate-500">
                      <span className="flex items-center gap-1">
                        <HardDrive className="w-3 h-3 text-slate-400" />
                        {formatFileSize(img.fileSize)}
                      </span>
                      <span className="flex items-center gap-1 text-[10px] text-slate-400">
                        <Calendar className="w-2.5 h-2.5" />
                        {formatDate(img.createdAt).split(' ')[0]}
                      </span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Footer with Pagination */}
        <div className="px-6 py-3.5 border-t border-slate-100 flex items-center justify-between bg-slate-50/60 text-xs text-slate-500">
          <span>
            Trang {page} / {totalPages || 1}
          </span>

          <div className="flex items-center gap-2">
            <button
              onClick={() => fetchImages(page - 1)}
              disabled={page <= 1 || loading}
              className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => fetchImages(page + 1)}
              disabled={page >= totalPages || loading}
              className="p-1.5 rounded-lg border border-slate-200 bg-white hover:bg-slate-50 disabled:opacity-40 transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
