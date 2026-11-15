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
  AlertTriangle,
  Layers,
  ArrowRight
} from 'lucide-react';
import { historyService } from '../api/historyService';
import { modelService } from '../api/modelService';
import { imageService } from '../api/imageService';
import type { 
  AnalysisHistoryItemDto, 
  PaginationMetadataDto 
} from '../types/history.types';
import type { AIModel } from '../types/model.types';

export const HistoryPage: React.FC = () => {
  const navigate = useNavigate();

  // State danh sách & phân trang thực tế từ Backend
  const [items, setItems] = useState<AnalysisHistoryItemDto[]>([]);
  const [pagination, setPagination] = useState<PaginationMetadataDto>({
    page: 1,
    pageSize: 10,
    totalItems: 0,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
  });

  // State danh sách model thực tế từ Backend để lọc
  const [availableModels, setAvailableModels] = useState<AIModel[]>([]);

  // State bộ lọc
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [modelFilter, setModelFilter] = useState<string>('');
  const [currentPage, setCurrentPage] = useState<number>(1);

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState<boolean>(false);

  // Load danh sách AI models thực tế từ Backend để đưa vào bộ lọc
  useEffect(() => {
    modelService.getModels()
      .then((models) => setAvailableModels(models))
      .catch((err) => console.error('Không thể tải danh sách model:', err));
  }, []);

  // Load danh sách lịch sử thực tế từ Backend API
  useEffect(() => {
    fetchHistory();
  }, [currentPage, statusFilter, modelFilter]);

  const fetchHistory = async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const res = await historyService.getHistory({
        page: currentPage,
        pageSize: 10,
        status: statusFilter || undefined,
        modelId: modelFilter || undefined,
        sortBy: 'createdAt',
        sortOrder: 'desc',
      });

      if (res && res.items) {
        setItems(res.items);
        setPagination(res.pagination || {
          page: currentPage,
          pageSize: 10,
          totalItems: res.items.length,
          totalPages: Math.ceil(res.items.length / 10) || 1,
          hasNextPage: false,
          hasPreviousPage: false,
        });
      } else {
        setItems([]);
      }
    } catch (err: any) {
      console.error('Lỗi khi tải lịch sử từ API:', err);
      setErrorMessage(err.message || 'Không thể kết nối đến máy chủ để lấy dữ liệu lịch sử phân tích.');
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  };

  // Xóa mục lịch sử thực tế trên Database
  const handleConfirmDelete = async () => {
    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await historyService.deleteHistory(deleteId);
      // Cập nhật lại danh sách thực tế sau khi xóa
      await fetchHistory();
      setDeleteId(null);
    } catch (err: any) {
      alert(`Xóa thất bại: ${err.message || 'Lỗi không xác định'}`);
    } finally {
      setIsDeleting(false);
    }
  };

  // Format ngày giờ: 14/06/2025 14:32
  const formatDate = (isoString?: string): string => {
    if (!isoString) return '-';
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

  // Lọc theo search input client-side theo ID hoặc tên
  const filteredItems = items.filter((item) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase();
    const id = item.analysisId.toLowerCase();
    const imgId = item.imageId.toLowerCase();
    const fileName = (item.fileName || '').toLowerCase();
    return id.includes(q) || imgId.includes(q) || fileName.includes(q);
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
            Xem lại các kết quả phân tích thực tế từ cơ sở dữ liệu
          </p>
        </div>

        {/* Filters Group */}
        <div className="flex flex-wrap items-center gap-2.5">
          {/* Search Box */}
          <div className="relative min-w-[220px]">
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
            <input
              type="text"
              placeholder="Tìm theo ID ảnh hoặc phiên..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full bg-white border border-slate-200 text-xs rounded-xl pl-9 pr-3 py-2 text-slate-700 placeholder-slate-400 shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
            />
          </div>

          {/* Model Filter (Binding danh sách model thật) */}
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
              {availableModels.map((m) => (
                <option key={m.id} value={m.id}>
                  {m.name}
                </option>
              ))}
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>

          {/* Status Filter (Binding enum trạng thái thật) */}
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
              <option value="Completed">Hoàn thành (Completed)</option>
              <option value="Processing">Đang xử lý (Processing)</option>
              <option value="PartiallyCompleted">Một phần (PartiallyCompleted)</option>
              <option value="Failed">Thất bại (Failed)</option>
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>

          {/* Refresh button */}
          <button
            type="button"
            onClick={fetchHistory}
            title="Làm mới dữ liệu từ Database"
            className="p-2 bg-white border border-slate-200 text-slate-600 hover:text-blue-600 rounded-xl shadow-xs transition-colors"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin text-blue-600' : ''}`} />
          </button>
        </div>
      </div>

      {/* Error alert nếu kết nối API lỗi */}
      {errorMessage && (
        <div className="p-4 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
            <span>{errorMessage}</span>
          </div>
          <button
            type="button"
            onClick={fetchHistory}
            className="px-3 py-1 bg-white border border-rose-200 text-rose-700 font-bold rounded-lg hover:bg-rose-100"
          >
            Thử lại
          </button>
        </div>
      )}

      {/* Main Table Container */}
      <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-4">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead>
              <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase text-[10px]">
                <th className="py-3 px-3 w-14">Ảnh</th>
                <th className="py-3 px-3">Mã phân tích / Ảnh</th>
                <th className="py-3 px-3">Thời gian tạo</th>
                <th className="py-3 px-3">Mô hình AI</th>
                <th className="py-3 px-3 text-center">Tiến độ chạy</th>
                <th className="py-3 px-3">Trạng thái</th>
                <th className="py-3 px-3 text-right">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100 font-medium">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    <RefreshCw className="w-6 h-6 animate-spin mx-auto mb-2 text-blue-600" />
                    Đang tải dữ liệu thực tế từ cơ sở dữ liệu...
                  </td>
                </tr>
              ) : filteredItems.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-12 text-center text-slate-400">
                    <History className="w-8 h-8 text-slate-300 mx-auto mb-2" />
                    <p className="font-semibold text-slate-600">Chưa có lượt phân tích nào trong cơ sở dữ liệu.</p>
                    <p className="text-[11px] text-slate-400 mt-1 max-w-sm mx-auto">
                      Hãy tải ảnh X-quang lên và bắt đầu phân tích để lưu lịch sử chẩn đoán.
                    </p>
                    <button
                      type="button"
                      onClick={() => navigate('/analyze')}
                      className="mt-4 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs rounded-xl inline-flex items-center gap-1.5 shadow-sm"
                    >
                      <span>Bắt đầu phân tích ảnh ngay</span>
                      <ArrowRight className="w-3.5 h-3.5" />
                    </button>
                  </td>
                </tr>
              ) : (
                filteredItems.map((item) => {
                  const imageUrl = item.originalImageUrl?.startsWith('http')
                    ? item.originalImageUrl
                    : item.imageId
                    ? imageService.getImageFileUrl(item.imageId)
                    : '';

                  const isSuccess = item.status === 'Completed';
                  const isProcessing = item.status === 'Processing' || item.status === 'Pending';
                  const isPartial = item.status === 'PartiallyCompleted';
                  const isFailed = item.status === 'Failed';

                  return (
                    <tr 
                      key={item.analysisId}
                      className="hover:bg-slate-50/70 transition-colors group cursor-pointer"
                      onClick={() => navigate(`/results/${item.analysisId}`)}
                    >
                      {/* Thumbnail thật từ Backend */}
                      <td className="py-3 px-3">
                        <div className="w-10 h-10 rounded-lg overflow-hidden bg-slate-900 border border-slate-200/80 shadow-inner flex items-center justify-center shrink-0">
                          {imageUrl ? (
                            <img
                              src={imageUrl}
                              alt="X-Ray"
                              className="w-full h-full object-cover"
                              onError={(e) => {
                                (e.target as HTMLElement).style.display = 'none';
                              }}
                            />
                          ) : (
                            <span className="text-[9px] font-mono text-slate-400">RX</span>
                          )}
                        </div>
                      </td>

                      {/* File Name / Analysis ID */}
                      <td className="py-3 px-3">
                        <div className="space-y-0.5">
                          <p className="font-bold text-slate-800 group-hover:text-blue-600 transition-colors">
                            {item.fileName || `Ca chẩn đoán #${item.analysisId.slice(0, 8)}`}
                          </p>
                          <p className="font-mono text-[10px] text-slate-400">
                            Ảnh: {item.imageId.slice(0, 8)}...
                          </p>
                        </div>
                      </td>

                      {/* Time */}
                      <td className="py-3 px-3 font-mono text-slate-500 text-[11px]">
                        {formatDate(item.createdAt)}
                      </td>

                      {/* Model Count / Name */}
                      <td className="py-3 px-3">
                        <span className="inline-flex items-center gap-1 font-bold text-slate-700 bg-slate-100 px-2 py-0.5 rounded-md text-[11px]">
                          <Layers className="w-3 h-3 text-slate-400" />
                          <span>{item.modelCount > 1 ? `Đa mô hình (${item.modelCount})` : '1 mô hình'}</span>
                        </span>
                      </td>

                      {/* Model Runs Progress */}
                      <td className="py-3 px-3 text-center">
                        <span className="font-bold text-slate-700 bg-slate-50 px-2.5 py-0.5 rounded-full text-[11px] border border-slate-200/60">
                          {item.completedModelCount} / {item.modelCount} hoàn tất
                        </span>
                      </td>

                      {/* Status Badge */}
                      <td className="py-3 px-3">
                        {isSuccess && (
                          <span className="inline-flex items-center gap-1.5 text-emerald-600 font-bold text-[11px] bg-emerald-50 px-2.5 py-1 rounded-full">
                            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                            Thành công
                          </span>
                        )}
                        {isProcessing && (
                          <span className="inline-flex items-center gap-1.5 text-blue-600 font-bold text-[11px] bg-blue-50 px-2.5 py-1 rounded-full">
                            <span className="w-1.5 h-1.5 rounded-full bg-blue-500 animate-pulse"></span>
                            Đang xử lý
                          </span>
                        )}
                        {isPartial && (
                          <span className="inline-flex items-center gap-1.5 text-amber-600 font-bold text-[11px] bg-amber-50 px-2.5 py-1 rounded-full">
                            <span className="w-1.5 h-1.5 rounded-full bg-amber-500"></span>
                            Một phần
                          </span>
                        )}
                        {isFailed && (
                          <span className="inline-flex items-center gap-1.5 text-rose-600 font-bold text-[11px] bg-rose-50 px-2.5 py-1 rounded-full">
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

        {/* Footer Pagination với dữ liệu phân trang thật từ Backend */}
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-slate-100 text-xs text-slate-500 font-medium">
          <div>
            Hiển thị <strong className="text-slate-800">{items.length > 0 ? (currentPage - 1) * pagination.pageSize + 1 : 0}</strong> - <strong className="text-slate-800">{Math.min(currentPage * pagination.pageSize, pagination.totalItems)}</strong> / <strong className="text-slate-800">{pagination.totalItems}</strong> kết quả từ cơ sở dữ liệu
          </div>

          {/* Page numbers */}
          <div className="flex items-center gap-1">
            <button
              type="button"
              disabled={currentPage <= 1 || !pagination.hasPreviousPage}
              onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
              className="w-8 h-8 rounded-lg border border-slate-200 text-slate-600 hover:bg-slate-50 flex items-center justify-center disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>

            {Array.from({ length: Math.max(pagination.totalPages, 1) }, (_, i) => i + 1).map((pageNum) => (
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

            <button
              type="button"
              disabled={currentPage >= pagination.totalPages || !pagination.hasNextPage}
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
                Bản ghi này sẽ bị xóa mềm khỏi cơ sở dữ liệu và dọn dẹp các ảnh kết quả liên quan.
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
