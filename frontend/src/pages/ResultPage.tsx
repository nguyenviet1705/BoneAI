import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  ArrowLeft, 
  Download, 
  RefreshCw, 
  AlertTriangle, 
  CheckCircle2, 
  Layers
} from 'lucide-react';
import { analysisService } from '../api/analysisService';
import { imageService } from '../api/imageService';
import { XRayImageViewer } from '../components/results/XRayImageViewer';
import { DetectionsTable } from '../components/results/DetectionsTable';
import { ModelComparisonTable } from '../components/results/ModelComparisonTable';
import { ModelInfoWidget } from '../components/results/ModelInfoWidget';
import type { 
  AnalysisResultDto, 
  AnalysisRunResultDto, 
  DetectionItemDto 
} from '../types/analysis.types';

export const ResultPage: React.FC = () => {
  const { analysisId } = useParams<{ analysisId: string }>();
  const navigate = useNavigate();

  const [analysis, setAnalysis] = useState<AnalysisResultDto | null>(null);
  const [activeRunId, setActiveRunId] = useState<string>('');
  const [highlightedDetectionId, setHighlightedDetectionId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Tải kết quả phân tích
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
          setActiveRunId(data.runs[0].runId);
        }

        // Nếu trạng thái còn Pending hoặc Processing, tiếp tục poll
        if (data.overallStatus === 'Pending' || data.overallStatus === 'Processing') {
          pollTimer = setTimeout(fetchResult, 1500);
        } else {
          setIsLoading(false);
        }
      } catch (err: any) {
        if (!isMounted) return;
        setError(err.message || 'Lỗi khi tải kết quả');
        console.warn('Lỗi khi tải kết quả thật từ API, sử dụng dữ liệu trực quan thiết kế:', err);
        createFallbackResult(analysisId);
        setIsLoading(false);
      }
    };

    fetchResult();

    return () => {
      isMounted = false;
      if (pollTimer) clearTimeout(pollTimer);
    };
  }, [analysisId]);

  // Fallback kết quả chuẩn xác theo thiết kế đính kèm
  const createFallbackResult = (id: string) => {
    const mockDetections: DetectionItemDto[] = [
      {
        detectionId: 'det-1',
        classLabel: 'fracture',
        confidenceScore: 0.927,
        boundingBox: {
          x: 0.38,
          y: 0.52,
          width: 0.22,
          height: 0.18,
        },
      },
    ];

    const mockRuns: AnalysisRunResultDto[] = [
      {
        runId: 'run-v8',
        analysisId: id,
        model: {
          modelId: 'm1',
          modelName: 'YOLOv8m',
          modelKey: 'yolov8m-bone-fracture-v1',
          version: 'v1.0',
          framework: 'PyTorch',
        },
        status: 'Completed',
        processingTime: 0.0139,
        processingTimeMs: 13.9,
        detectionCount: 1,
        detections: mockDetections,
      },
      {
        runId: 'run-v11',
        analysisId: id,
        model: {
          modelId: 'm2',
          modelName: 'YOLOv11',
          modelKey: 'yolov11-wrist',
          version: 'v2.0',
          framework: 'PyTorch',
        },
        status: 'Completed',
        processingTime: 0.0142,
        processingTimeMs: 14.2,
        detectionCount: 1,
        detections: [
          {
            detectionId: 'det-2',
            classLabel: 'fracture',
            confidenceScore: 0.5512,
            boundingBox: { x: 0.39, y: 0.53, width: 0.21, height: 0.17 },
          },
        ],
      },
      {
        runId: 'run-v26',
        analysisId: id,
        model: {
          modelId: 'm3',
          modelName: 'YOLOv26',
          modelKey: 'yolov26-wrist',
          version: 'v3.0',
          framework: 'PyTorch',
        },
        status: 'Completed',
        processingTime: 0.0151,
        processingTimeMs: 15.1,
        detectionCount: 3,
        detections: [
          ...mockDetections,
          {
            detectionId: 'det-3',
            classLabel: 'fracture',
            confidenceScore: 0.9348,
            boundingBox: { x: 0.36, y: 0.50, width: 0.24, height: 0.20 },
          },
        ],
      },
      {
        runId: 'run-rcnn',
        analysisId: id,
        model: {
          modelId: 'm4',
          modelName: 'Faster R-CNN',
          modelKey: 'faster-rcnn-bone',
          version: 'v1.0',
          framework: 'TorchVision',
        },
        status: 'Completed',
        processingTime: 0.0287,
        processingTimeMs: 28.7,
        detectionCount: 1,
        detections: mockDetections,
      },
    ];

    const fallback: AnalysisResultDto = {
      analysisId: id,
      imageId: 'img-wrist-child-001',
      originalImage: {
        imageId: 'img-wrist-child-001',
        fileName: 'wrist_child.jpg',
        contentType: 'image/jpeg',
        fileSize: 1258291,
        originalImageUrl: '/api/images/img-wrist-child-001/file',
      },
      overallStatus: 'Completed',
      mode: 'MultiModel',
      createdAt: new Date().toISOString(),
      totalRunsCount: 4,
      completedRunsCount: 4,
      failedRunsCount: 0,
      runs: mockRuns,
    };

    setAnalysis(fallback);
    setActiveRunId(mockRuns[0].runId);
  };

  const handleDownloadReport = () => {
    window.print();
  };

  if (isLoading && !analysis) {
    return (
      <div className="min-h-[400px] flex flex-col items-center justify-center space-y-4">
        <RefreshCw className="w-8 h-8 text-blue-600 animate-spin" />
        <p className="text-sm font-semibold text-slate-600">
          Đang tải kết quả phân tích...
        </p>
      </div>
    );
  }

  if (error || !analysis) {
    return (
      <div className="bg-white rounded-3xl p-8 border border-slate-200/80 shadow-xs text-center space-y-4 max-w-md mx-auto">
        <AlertTriangle className="w-12 h-12 text-rose-500 mx-auto" />
        <h3 className="text-base font-bold text-slate-800">
          Không tìm thấy kết quả phân tích
        </h3>
        <p className="text-xs text-slate-500">
          {error || 'Lượt phân tích không tồn tại hoặc đã bị xóa.'}
        </p>
        <button
          type="button"
          onClick={() => navigate('/analyze')}
          className="px-4 py-2 bg-blue-600 text-white text-xs font-semibold rounded-xl"
        >
          Quay lại màn hình phân tích
        </button>
      </div>
    );
  }

  // Active run hiện tại
  const activeRun =
    analysis.runs.find((r) => r.runId === activeRunId) || analysis.runs[0];

  // Original image URL
  const originalImageUrl = analysis.originalImage.originalImageUrl?.startsWith('http')
    ? analysis.originalImage.originalImageUrl
    : analysis.originalImage.imageId
    ? imageService.getImageFileUrl(analysis.originalImage.imageId)
    : 'https://placehold.co/600x600/0d1b3e/ffffff?text=X-Ray+Original';

  // Result detection image URL
  const resultImageUrl = activeRun?.runId
    ? analysisService.getRunResultImageUrl(activeRun.runId)
    : originalImageUrl;

  const isMultiModel = analysis.runs.length > 1;

  return (
    <div className="space-y-6">
      {/* Top Header Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-1">
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <span>Kết quả phân tích</span>
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-100 text-emerald-800 border border-emerald-200">
              <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
              Hoàn thành
            </span>
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Kết quả phát hiện gãy xương trên ảnh X-quang cổ tay
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
                  {r.detections.length}
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
            
            {/* Ảnh gốc */}
            <div className="h-full">
              <XRayImageViewer
                title="Ảnh gốc"
                imageUrl={originalImageUrl}
                showDetections={false}
              />
            </div>

            {/* Ảnh sau khi phát hiện */}
            <div className="h-full">
              <XRayImageViewer
                title="Ảnh sau khi phát hiện"
                imageUrl={resultImageUrl || originalImageUrl}
                detections={activeRun?.detections || []}
                highlightedDetectionId={highlightedDetectionId}
                onHoverDetection={setHighlightedDetectionId}
                showDetections={true}
              />
            </div>

          </div>

          <div className="flex items-center justify-between text-[11px] text-slate-400 font-medium px-2 pt-1 border-t border-slate-100">
            <span>File: {analysis.originalImage.fileName || 'wrist_child.jpg'}</span>
            <span>Kích thước chuẩn hóa: 640 × 640 px</span>
          </div>
        </div>

        {/* ===================== CỘT PHẢI (Lg: 4 cols): Bảng vùng nghi ngờ & So sánh model ===================== */}
        <div className="lg:col-span-4 space-y-5">
          
          {/* Bảng danh sách vùng nghi ngờ */}
          <DetectionsTable
            detections={activeRun?.detections || []}
            highlightedDetectionId={highlightedDetectionId}
            onHoverDetection={setHighlightedDetectionId}
          />

          {/* Bảng so sánh đa model (FE4) */}
          {isMultiModel && (
            <ModelComparisonTable
              runs={analysis.runs}
              activeRunId={activeRun?.runId || ''}
              onSelectRun={(runId) => setActiveRunId(runId)}
            />
          )}

          {/* Widget thông tin Model */}
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
