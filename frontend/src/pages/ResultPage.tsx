import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  ArrowLeft, 
  Download, 
  RefreshCw, 
  AlertTriangle, 
  CheckCircle2, 
  Layers,
  XCircle,
  Clock
} from 'lucide-react';
import { analysisService } from '../api/analysisService';
import { imageService } from '../api/imageService';
import { XRayImageViewer } from '../components/results/XRayImageViewer';
import { DetectionsTable } from '../components/results/DetectionsTable';
import { ModelComparisonTable } from '../components/results/ModelComparisonTable';
import { ModelInfoWidget } from '../components/results/ModelInfoWidget';
import type { 
  AnalysisResultDto 
} from '../types/analysis.types';

export const ResultPage: React.FC = () => {
  const { analysisId } = useParams<{ analysisId: string }>();
  const navigate = useNavigate();

  const [analysis, setAnalysis] = useState<AnalysisResultDto | null>(null);
  const [activeRunId, setActiveRunId] = useState<string>('');
  const [highlightedDetectionId, setHighlightedDetectionId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Tải kết quả phân tích thực tế từ Backend API
  useEffect(() => {
    if (!analysisId) return;

    let isMounted = true;
    let pollTimer: any = null;

    const fetchResult = async () => {
      try {
        const data = await analysisService.getAnalysisResult(analysisId);
        if (!isMounted) return;

        setAnalysis(data);
        if (data.runs && data.runs.length > 0 && !activeRunId) {
          // Ưu tiên chọn run đã hoàn thành đầu tiên
          const completedRun = data.runs.find((r) => r.status === 'Completed');
          setActiveRunId(completedRun ? completedRun.runId : data.runs[0].runId);
        }

        // Nếu trạng thái tổng thể còn Pending hoặc Processing, tiếp tục poll kết quả thực tế
        if (data.overallStatus === 'Pending' || data.overallStatus === 'Processing') {
          pollTimer = setTimeout(fetchResult, 1500);
        } else {
          setIsLoading(false);
        }
      } catch (err: any) {
        if (!isMounted) return;
        console.error('Lỗi khi tải kết quả phân tích:', err);
        setError(err.message || `Không thể tìm thấy kết quả phân tích cho mã ${analysisId}`);
        setIsLoading(false);
      }
    };

    fetchResult();

    return () => {
      isMounted = false;
      if (pollTimer) clearTimeout(pollTimer);
    };
  }, [analysisId, activeRunId]);

  const handleDownloadReport = () => {
    window.print();
  };

  if (isLoading && !analysis) {
    return (
      <div className="min-h-[400px] flex flex-col items-center justify-center space-y-4">
        <RefreshCw className="w-8 h-8 text-blue-600 animate-spin" />
        <p className="text-sm font-semibold text-slate-600">
          Đang truy xuất kết quả phân tích từ máy chủ...
        </p>
      </div>
    );
  }

  if (error || !analysis) {
    return (
      <div className="bg-white rounded-3xl p-8 border border-slate-200/80 shadow-xs text-center space-y-4 max-w-md mx-auto my-12">
        <AlertTriangle className="w-12 h-12 text-rose-500 mx-auto" />
        <h3 className="text-base font-bold text-slate-800">
          Không tìm thấy kết quả phân tích
        </h3>
        <p className="text-xs text-slate-500 leading-relaxed">
          {error || `Phiên phân tích (${analysisId}) không tồn tại trong cơ sở dữ liệu.`}
        </p>
        <button
          type="button"
          onClick={() => navigate('/analyze')}
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-semibold rounded-xl transition-colors"
        >
          Quay lại màn hình phân tích
        </button>
      </div>
    );
  }

  // Active run hiện tại
  const activeRun =
    analysis.runs.find((r) => r.runId === activeRunId) || analysis.runs[0];

  // URL ảnh gốc thực tế
  const originalImageUrl = analysis.originalImage?.originalImageUrl?.startsWith('http')
    ? analysis.originalImage.originalImageUrl
    : analysis.originalImage?.imageId || analysis.imageId
    ? imageService.getImageFileUrl(analysis.originalImage?.imageId || analysis.imageId)
    : '';

  // URL ảnh kết quả thực tế từ rendering service của backend (thêm cache-buster timestamp để luôn nạp ảnh mới nhất)
  const isRunCompleted = activeRun?.status === 'Completed';
  const resultImageUrl = isRunCompleted && activeRun?.runId
    ? `${analysisService.getRunResultImageUrl(activeRun.runId)}?v=${new Date(activeRun.completedAt || Date.now()).getTime()}`
    : originalImageUrl;

  const isMultiModel = analysis.runs.length > 1;
  const isOverallCompleted = analysis.overallStatus === 'Completed';
  const isOverallProcessing = analysis.overallStatus === 'Processing' || analysis.overallStatus === 'Pending';
  const isOverallFailed = analysis.overallStatus === 'Failed';

  return (
    <div className="space-y-6">
      {/* Top Header Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-1">
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <span>Kết quả phân tích</span>
            {isOverallCompleted && (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800 border border-emerald-200">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                Hoàn thành
              </span>
            )}
            {isOverallProcessing && (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-blue-100 text-blue-800 border border-blue-200">
                <Clock className="w-3.5 h-3.5 text-blue-600 animate-spin" />
                Đang xử lý
              </span>
            )}
            {isOverallFailed && (
              <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-rose-100 text-rose-800 border border-rose-200">
                <XCircle className="w-3.5 h-3.5 text-rose-600" />
                Thất bại
              </span>
            )}
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Mã phiên: <span className="font-mono text-slate-700">{analysis.analysisId}</span> • Chế độ: <strong className="text-slate-700">{analysis.mode}</strong>
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={handleDownloadReport}
            className="px-4 py-2.5 rounded-xl bg-white border border-slate-200/90 hover:bg-slate-50 text-slate-700 text-xs font-bold flex items-center gap-2 shadow-xs transition-colors"
          >
            <Download className="w-4 h-4 text-blue-600" />
            <span>Tải xuống báo cáo</span>
          </button>

          <button
            type="button"
            onClick={() => navigate('/analyze')}
            className="px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold flex items-center gap-2 shadow-md shadow-blue-600/20 transition-all"
          >
            <ArrowLeft className="w-4 h-4" />
            <span>Quay lại</span>
          </button>
        </div>
      </div>

      {/* Multi-Model Switcher Tabs (FE4) */}
      {isMultiModel && (
        <div className="bg-white rounded-2xl p-2 border border-slate-200/80 shadow-xs flex items-center gap-2 overflow-x-auto">
          <span className="text-xs font-bold text-slate-500 px-3 flex items-center gap-1.5 shrink-0">
            <Layers className="w-4 h-4 text-blue-600" />
            <span>Chọn mô hình xem:</span>
          </span>
          {analysis.runs.map((r) => {
            const isTabActive = r.runId === activeRun?.runId;
            return (
              <button
                key={r.runId}
                type="button"
                onClick={() => setActiveRunId(r.runId)}
                className={`px-3.5 py-1.5 rounded-xl text-xs font-bold transition-all shrink-0 flex items-center gap-2 ${
                  isTabActive
                    ? 'bg-blue-600 text-white shadow-xs'
                    : 'bg-slate-100 text-slate-600 hover:bg-slate-200/70'
                }`}
              >
                <span>{r.model.modelName}</span>
                <span
                  className={`text-[10px] px-1.5 py-0.2 rounded-full ${
                    isTabActive
                      ? 'bg-white/20 text-white'
                      : 'bg-slate-200 text-slate-700'
                  }`}
                >
                  {r.detections ? r.detections.length : 0}
                </span>
              </button>
            );
          })}
        </div>
      )}

      {/* Main 2-Column Content Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        
        {/* ===================== CỘT TRÁI (Lg: 8 cols): 2 Ảnh Song Song (Ảnh gốc & Sau phát hiện) ===================== */}
        <div className="lg:col-span-8 bg-white rounded-3xl p-5 border border-slate-200/80 shadow-xs space-y-5">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            
            {/* Ảnh gốc thực tế */}
            <div className="h-full">
              <XRayImageViewer
                title="Ảnh gốc"
                imageUrl={originalImageUrl}
                showDetections={false}
              />
            </div>

            {/* Ảnh sau khi phát hiện thực tế */}
            <div className="h-full">
              <XRayImageViewer
                title={`Ảnh sau phát hiện (${activeRun?.model?.modelName || 'AI'})`}
                imageUrl={resultImageUrl}
                fallbackUrl={originalImageUrl}
                isProcessing={!isRunCompleted && (isOverallProcessing || activeRun?.status === 'Processing')}
                detections={activeRun?.detections || []}
                highlightedDetectionId={highlightedDetectionId}
                onHoverDetection={setHighlightedDetectionId}
                showDetections={false}
              />
            </div>

          </div>

          <div className="flex items-center justify-between text-[11px] text-slate-400 font-medium px-2 pt-1 border-t border-slate-100">
            <span>File: {analysis.originalImage?.fileName || `image_${analysis.imageId.slice(0, 8)}`}</span>
            <span>Trạng thái lượt chạy: <strong className="text-slate-600">{activeRun?.status || '-'}</strong></span>
          </div>

          {/* Error message nếu run thất bại */}
          {activeRun?.status === 'Failed' && activeRun.errorMessage && (
            <div className="p-3.5 rounded-2xl bg-rose-50 border border-rose-200 text-rose-800 text-xs flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-rose-600 shrink-0" />
              <span>Mô hình gặp lỗi khi suy luận: {activeRun.errorMessage}</span>
            </div>
          )}
        </div>

        {/* ===================== CỘT PHẢI (Lg: 4 cols): Bảng vùng nghi ngờ & So sánh model ===================== */}
        <div className="lg:col-span-4 space-y-5">
          
          {/* Bảng danh sách vùng nghi ngờ thực tế */}
          <DetectionsTable
            detections={activeRun?.detections || []}
            highlightedDetectionId={highlightedDetectionId}
            onHoverDetection={setHighlightedDetectionId}
          />

          {/* Bảng so sánh đa model thực tế (FE4) */}
          {isMultiModel && (
            <ModelComparisonTable
              runs={analysis.runs}
              activeRunId={activeRun?.runId || ''}
              onSelectRun={(runId) => setActiveRunId(runId)}
            />
          )}

          {/* Widget thông tin Model thực tế */}
          {activeRun && (
            <ModelInfoWidget
              model={activeRun.model}
              processingTimeMs={activeRun.processingTimeMs}
            />
          )}

        </div>
      </div>
    </div>
  );
};
