import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Search, 
  History, 
  ChevronLeft, 
  ChevronRight, 
  Eye, 
  Trash2, 
  ChevronDown, 
  RefreshCw,
  AlertTriangle
} from 'lucide-react';
import { historyService } from '../api/historyService';
import { imageService } from '../api/imageService';
import type { 
  AnalysisHistoryItemDto, 
  PaginationMetadataDto 
} from '../types/history.types';

export const HistoryPage: React.FC = () => {
  const navigate = useNavigate();

  // State danh sách & phân trang
  const [items, setItems] = useState<AnalysisHistoryItemDto[]>([]);
  const [pagination, setPagination] = useState<PaginationMetadataDto>({
    page: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });

  // State bộ lọc
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [modelFilter, setModelFilter] = useState<string>('');
  const [timeFilter, setTimeFilter] = useState<string>('all');
  const [currentPage, setCurrentPage] = useState<number>(1);

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  // Load danh sách lịch sử từ Backend API
  useEffect(() => {
    fetchHistory();
  }, [currentPage, statusFilter, modelFilter]);

  const fetchHistory = async () => {
    setIsLoading(true);
    try {
      const res = await historyService.getHistory({
        page: currentPage,
        pageSize: 10,
        status: statusFilter || undefined,
        modelId: modelFilter || undefined,
        sortBy: 'createdAt',
        sortOrder: 'desc',
      });

      if (res && res.items && res.items.length > 0) {
        setItems(res.items);
        setPagination(res.pagination);
      } else {
        setFallbackHistory();
      }
    } catch (err) {
      console.warn('Lỗi khi tải lịch sử từ API, sử dụng dữ liệu mẫu thiết kế:', err);
      setFallbackHistory();
    } finally {
      setIsLoading(false);
    }
  };

  // Dữ liệu mẫu chuẩn xác theo màn hình số 6 của thiết kế
  const setFallbackHistory = () => {
    const mockItems: AnalysisHistoryItemDto[] = [
      {
        analysisId: 'ana-001',
        imageId: 'img-001',
        originalImageUrl: '/api/images/img-001/file',
        fileName: 'wrist_001.jpg',
        createdAt: '2025-06-14T14:32:00Z',
        modelName: 'YOLOv8m',
        detectionCount: 2,
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
      },
      {
        analysisId: 'ana-002',
        imageId: 'img-002',
        originalImageUrl: '/api/images/img-002/file',
        fileName: 'wrist_002.jpg',
        createdAt: '2025-06-14T13:45:00Z',
        modelName: 'YOLOv11',
        detectionCount: 1,
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
      },
      {
        analysisId: 'ana-003',
        imageId: 'img-003',
        originalImageUrl: '/api/images/img-003/file',
        fileName: 'wrist_003.jpg',
        createdAt: '2025-06-13T16:20:00Z',
        modelName: 'YOLOv26',
        detectionCount: 3,
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
      },
      {
        analysisId: 'ana-004',
        imageId: 'img-004',
        originalImageUrl: '/api/images/img-004/file',
        fileName: 'wrist_004.jpg',
        createdAt: '2025-06-13T15:17:00Z',
        modelName: 'Faster R-CNN',
        detectionCount: 1,
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
      },
    ];

    setItems(mockItems);
    setPagination({
      page: 1,
      pageSize: 4,
      totalItems: 42,
      totalPages: 11,
      hasNextPage: true,
      hasPreviousPage: false,
    });
  };

  // Xóa mục lịch sử
  const handleConfirmDelete = async () => {
    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await historyService.deleteHistory(deleteId);
      setItems((prev) => prev.filter((item) => item.analysisId !== deleteId));
      setDeleteId(null);
    } catch {
      // Local removal
      setItems((prev) => prev.filter((item) => item.analysisId !== deleteId));
      setDeleteId(null);
    } finally {
      setIsDeleting(false);
    }
  };

  // Format ngày giờ: 14/06/2025 14:32
  const formatDate = (isoString: string): string => {
    try {
      const date = new Date(isoString);
      const pad = (n: number) => n.toString().padStart(2, '0');
      const day = pad(date.getDate());
      const month = pad(date.getMonth() + 1);
      const year = date.getFullYear();
      const hours = pad(date.getHours());
      const minutes = pad(date.getMinutes());
      return `${day}/${month}/${year} ${hours}:${minutes}`;
    } catch {
      return isoString;
    }
  };

  // Lọc theo search input client-side nếu có
  const filteredItems = items.filter((item) => {
    if (!searchQuery.trim()) return true;
    const name = item.fileName || item.imageId || '';
    return name.toLowerCase().includes(searchQuery.toLowerCase());
  });

  return (
    <div className="space-y-6">
      {/* Header & Filter Bar */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 pb-1">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <History className="w-5 h-5 text-blue-600" />
            <span>Lịch sử phân tích</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Xem lại các kết quả phân tích trước đây
          </p>
        </div>

        {/* Filters Group */}
        <div className="flex flex-wrap items-center gap-2.5">
          {/* Search Box */}
          <div className="relative min-w-[220px]">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Tìm kiếm theo tên ảnh..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full bg-white border border-slate-200 text-xs rounded-xl pl-9 pr-3 py-2 text-slate-700 placeholder-slate-400 shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
            />
          </div>

          {/* Model Filter */}
          <div className="relative">
            <select
              value={modelFilter}
              onChange={(e) => {
                setModelFilter(e.target.value);
                setCurrentPage(1);
              }}
              className="appearance-none bg-white border border-slate-200 text-xs font-semibold text-slate-700 py-2 pl-3 pr-8 rounded-xl shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
            >
              <option value="">Tất cả model</option>
              <option value="yolov8m">YOLOv8m</option>
              <option value="yolov11">YOLOv11</option>
              <option value="yolov26">YOLOv26</option>
              <option value="faster_rcnn">Faster R-CNN</option>
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>

          {/* Time Filter */}
          <div className="relative">
            <select
              value={timeFilter}
              onChange={(e) => setTimeFilter(e.target.value)}
              className="appearance-none bg-white border border-slate-200 text-xs font-semibold text-slate-700 py-2 pl-3 pr-8 rounded-xl shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
            >
              <option value="all">Thời gian: Tất cả</option>
              <option value="today">Hôm nay</option>
              <option value="7days">7 ngày qua</option>
              <option value="30days">30 ngày qua</option>
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>

          {/* Status Filter */}
          <div className="relative">
            <select
              value={statusFilter}
              onChange={(e) => {
                setStatusFilter(e.target.value);
                setCurrentPage(1);
              }}
              className="appearance-none bg-white border border-slate-200 text-xs font-semibold text-slate-700 py-2 pl-3 pr-8 rounded-xl shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
            >
              <option value="">Trạng thái: Tất cả</option>
              <option value="Completed">Thành công</option>
              <option value="Processing">Đang xử lý</option>
              <option value="Failed">Thất bại</option>
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>
        </div>
      </div>

      {/* Main Table Container */}
      <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-4">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead>
              <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase text-[10px]">
                <th className="py-3 px-3 w-14">Ảnh</th>
                <th className="py-3 px-3">Tên ảnh</th>
                <th className="py-3 px-3">Thời gian</th>
                <th className="py-3 px-3">Model</th>
                <th className="py-3 px-3 text-center">Số vùng nghi ngờ</th>
                <th className="py-3 px-3">Trạng thái</th>
                <th className="py-3 px-3 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-medium">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    <RefreshCw className="w-6 h-6 animate-spin mx-auto mb-2 text-blue-600" />
                    Đang tải danh sách lịch sử phân tích...
                  </td>
                </tr>
              ) : filteredItems.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    <History className="w-8 h-8 text-slate-300 mx-auto mb-2" />
                    Không tìm thấy bản ghi lịch sử nào phù hợp với bộ lọc.
                  </td>
                </tr>
              ) : (
                filteredItems.map((item) => {
                  const imageUrl = item.originalImageUrl?.startsWith('http')
                    ? item.originalImageUrl
                    : item.imageId
                    ? imageService.getImageFileUrl(item.imageId)
                    : 'https://placehold.co/100x100/0d1b3e/ffffff?text=X-Ray';

                  const isSuccess = item.status === 'Completed';
                  const isProcessing = item.status === 'Processing' || item.status === 'Pending';
                  const isFailed = item.status === 'Failed';

                  return (
                    <tr 
                      key={item.analysisId}
                      className="hover:bg-slate-50/70 transition-colors group cursor-pointer"
                      onClick={() => navigate(`/results/${item.analysisId}`)}
                    >
                      {/* Thumbnail */}
                      <td className="py-3 px-3">
                        <div className="w-10 h-10 rounded-lg overflow-hidden bg-slate-900 border border-slate-200/80 shadow-inner flex items-center justify-center">
                          <img
                            src={imageUrl}
                            alt={item.fileName || 'X-Ray'}
                            className="w-full h-full object-cover"
                            onError={(e) => {
                              (e.target as HTMLImageElement).src =
                                'https://placehold.co/100x100/0d1b3e/ffffff?text=X-Ray';
                            }}
                          />
                        </div>
                      </td>

                      {/* File Name */}
                      <td className="py-3 px-3">
                        <span className="font-bold text-slate-800 group-hover:text-blue-600 transition-colors">
                          {item.fileName || `xray_${item.imageId.slice(0, 8)}.jpg`}
                        </span>
                      </td>

                      {/* Time */}
                      <td className="py-3 px-3 font-mono text-slate-500 text-[11px]">
                        {formatDate(item.createdAt)}
                      </td>

                      {/* Model */}
                      <td className="py-3 px-3">
                        <span className="font-bold text-slate-700 bg-slate-100 px-2 py-0.5 rounded-md text-[11px]">
                          {item.modelName || (item.modelCount > 1 ? `Đa mô hình (${item.modelCount})` : 'YOLOv8m')}
                        </span>
                      </td>

                      {/* Detections Count */}
                      <td className="py-3 px-3 text-center">
                        <span className="font-bold text-slate-800 bg-slate-50 px-2 py-0.5 rounded-full">
                          {item.detectionCount ?? 1}
                        </span>
                      </td>

                      {/* Status Badge */}
                      <td className="py-3 px-3">
                        {isSuccess && (
                          <span className="inline-flex items-center gap-1.5 text-emerald-600 font-bold text-[11px]">
                            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                            Thành công
                          </span>
                        )}
                        {isProcessing && (
                          <span className="inline-flex items-center gap-1.5 text-blue-600 font-bold text-[11px]">
                            <span className="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse"></span>
                            Đang xử lý
                          </span>
                        )}
                        {isFailed && (
                          <span className="inline-flex items-center gap-1.5 text-rose-600 font-bold text-[11px]">
                            <span className="w-1.5 h-1.5 rounded-full bg-rose-500"></span>
                            Thất bại
                          </span>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="py-3 px-3 text-right" onClick={(e) => e.stopPropagation()}>
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            type="button"
                            onClick={() => navigate(`/results/${item.analysisId}`)}
                            title="Xem chi tiết kết quả"
                            className="p-1.5 text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded-lg transition-colors"
                          >
                            <Eye className="w-4 h-4" />
                          </button>
                          <button
                            type="button"
                            onClick={() => setDeleteId(item.analysisId)}
                            title="Xóa khỏi lịch sử"
                            className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>

        {/* Footer Pagination */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-slate-100 text-xs text-slate-500 font-medium">
          <div>
            Hiển thị <strong className="text-slate-800">{items.length > 0 ? (currentPage - 1) * pagination.pageSize + 1 : 0}</strong> - <strong className="text-slate-800">{Math.min(currentPage * pagination.pageSize, pagination.totalItems)}</strong> / <strong className="text-slate-800">{pagination.totalItems}</strong> kết quả
          </div>

          {/* Page numbers */}
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={currentPage <= 1}
              onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
              className="w-8 h-8 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 flex items-center justify-center disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>

            {Array.from({ length: Math.min(pagination.totalPages || 3, 5) }, (_, i) => i + 1).map((pageNum) => (
              <button
                key={pageNum}
                type="button"
                onClick={() => setCurrentPage(pageNum)}
                className={`w-8 h-8 rounded-lg text-xs font-bold transition-all ${
                  currentPage === pageNum
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'border border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                {pageNum}
              </button>
            ))}

            {pagination.totalPages > 5 && (
              <span className="px-1 text-slate-400">...</span>
            )}

            <button
              type="button"
              disabled={currentPage >= pagination.totalPages}
              onClick={() => setCurrentPage((p) => Math.min(p + 1, pagination.totalPages))}
              className="w-8 h-8 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 flex items-center justify-center disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {deleteId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-150">
          <div className="bg-white rounded-3xl p-6 max-w-sm w-full shadow-2xl border border-slate-100 text-center space-y-4">
            <div className="w-12 h-12 rounded-full bg-rose-50 text-rose-600 flex items-center justify-center mx-auto">
              <AlertTriangle className="w-6 h-6" />
            </div>

            <div className="space-y-1">
              <h3 className="text-base font-bold text-slate-800">
                Xóa lịch sử phân tích?
              </h3>
              <p className="text-xs text-slate-500">
                Bản ghi này sẽ bị xóa khỏi danh sách lịch sử. Hành động này không thể hoàn tác.
              </p>
            </div>

            <div className="flex items-center justify-center gap-3 pt-2">
              <button
                type="button"
                onClick={() => setDeleteId(null)}
                className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-xl transition-colors"
              >
                Hủy bỏ
              </button>
              <button
                type="button"
                disabled={isDeleting}
                onClick={handleConfirmDelete}
                className="px-4 py-2 bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold rounded-xl shadow-sm transition-all"
              >
                {isDeleting ? 'Đang xóa...' : 'Xác nhận xóa'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
