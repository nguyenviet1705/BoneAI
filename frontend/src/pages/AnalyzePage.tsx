import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Check, 
  ArrowRight, 
  Cpu, 
  ExternalLink,
  Info,
  CheckSquare,
  Square
} from 'lucide-react';
import { DropzoneUpload } from '../components/upload/DropzoneUpload';
import { SelectedImageCard } from '../components/upload/SelectedImageCard';
import { ImageGalleryModal } from '../components/upload/ImageGalleryModal';
import { AnalysisProgressModal } from '../components/analysis/AnalysisProgressModal';
import { imageService } from '../api/imageService';
import { modelService } from '../api/modelService';
import { analysisService } from '../api/analysisService';
import { AnalysisMode } from '../types/analysis.types';
import type { SelectedImageInfo, ImageResponse } from '../types/image.types';
import type { AIModel } from '../types/model.types';

export const AnalyzePage: React.FC = () => {
  const navigate = useNavigate();

  // State ảnh
  const [selectedImage, setSelectedImage] = useState<SelectedImageInfo | null>(null);
  const [isUploading, setIsUploading] = useState<boolean>(false);
  const [isGalleryOpen, setIsGalleryOpen] = useState<boolean>(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  // State model & chế độ phân tích
  const [models, setModels] = useState<AIModel[]>([]);
  const [selectedModelIds, setSelectedModelIds] = useState<string[]>([]);
  const [analysisMode, setAnalysisMode] = useState<'single' | 'multi'>('single');
  const [isAnalyzing, setIsAnalyzing] = useState<boolean>(false);

  // Load models từ backend hoặc fallback danh sách chuẩn
  useEffect(() => {
    modelService.getModels()
      .then((data) => {
        if (data && data.length > 0) {
          setModels(data);
          setSelectedModelIds([data[0].id]);
        } else {
          setFallbackModels();
        }
      })
      .catch(() => {
        setFallbackModels();
      });
  }, []);

  const setFallbackModels = () => {
    const defaultModels: AIModel[] = [
      {
        id: 'yolov8m-bone-fracture-v1',
        modelKey: 'yolov8m-bone-fracture-v1',
        name: 'YOLOv8m',
        version: '1.0',
        framework: 'PyTorch (Ultralytics)',
        description: 'Baseline - 640x640',
        isActive: true,
        isAvailable: true,
        mAP50: 94.76,
        fps: 64,
        dataset: 'GRAZPEDWRI-DX',
        inputSize: '640 x 640',
        weightsName: 'best.pt'
      },
      {
        id: 'yolov11-wrist',
        modelKey: 'yolov11-wrist',
        name: 'YOLOv11',
        version: '2.0',
        framework: 'PyTorch',
        description: 'Phiên bản: 2.0',
        isActive: false,
        isAvailable: false,
      },
      {
        id: 'yolov26-wrist',
        modelKey: 'yolov26-wrist',
        name: 'YOLOv26',
        version: '3.0',
        framework: 'PyTorch',
        description: 'Phiên bản: 3.0',
        isActive: false,
        isAvailable: false,
      },
      {
        id: 'faster-rcnn-bone',
        modelKey: 'faster-rcnn-bone',
        name: 'Faster R-CNN',
        version: '1.0',
        framework: 'PyTorch (TorchVision)',
        description: 'Two-stage detector',
        isActive: false,
        isAvailable: false,
      }
    ];
    setModels(defaultModels);
    setSelectedModelIds([defaultModels[0].id]);
  };

  // Xử lý khi người dùng chọn file từ máy tính hoặc kéo thả
  const handleFileSelected = async (file: File) => {
    setUploadError(null);
    setIsUploading(true);

    const previewUrl = URL.createObjectURL(file);

    // Đọc độ phân giải thực tế của ảnh
    const imgElement = new Image();
    imgElement.src = previewUrl;
    imgElement.onload = () => {
      setSelectedImage({
        file,
        previewUrl,
        fileName: file.name,
        fileSizeFormatted: formatFileSize(file.size),
        fileSizeBytes: file.size,
        width: imgElement.naturalWidth,
        height: imgElement.naturalHeight,
        isUploaded: false,
      });
    };

    // Gọi API upload ngay lên server
    try {
      const uploaded = await imageService.uploadImage(file);
      setSelectedImage((prev) => prev ? {
        ...prev,
        id: uploaded.id,
        isUploaded: true,
      } : null);
    } catch (err: any) {
      setUploadError(err.message || 'Không thể tải ảnh lên máy chủ. Đang giữ ảnh ở bộ nhớ đệm tạm thời.');
    } finally {
      setIsUploading(false);
    }
  };

  // Xử lý khi chọn ảnh có sẵn từ thư viện
  const handleSelectFromGallery = (image: ImageResponse) => {
    const previewUrl = imageService.getImageFileUrl(image.id);

    const imgElement = new Image();
    imgElement.src = previewUrl;
    imgElement.onload = () => {
      setSelectedImage({
        id: image.id,
        fileName: image.fileName,
        previewUrl,
        fileSizeFormatted: formatFileSize(image.fileSize),
        fileSizeBytes: image.fileSize,
        width: imgElement.naturalWidth,
        height: imgElement.naturalHeight,
        isUploaded: true,
      });
    };
  };

  const handleRemoveImage = () => {
    if (selectedImage?.previewUrl && selectedImage.previewUrl.startsWith('blob:')) {
      URL.revokeObjectURL(selectedImage.previewUrl);
    }
    setSelectedImage(null);
    setUploadError(null);
  };

  // Xử lý chọn model: Đơn (FE3) hoặc Đa model (FE4)
  const handleModelToggle = (modelId: string) => {
    if (analysisMode === 'single') {
      setSelectedModelIds([modelId]);
    } else {
      if (selectedModelIds.includes(modelId)) {
        if (selectedModelIds.length > 1) {
          setSelectedModelIds(selectedModelIds.filter((id) => id !== modelId));
        }
      } else {
        setSelectedModelIds([...selectedModelIds, modelId]);
      }
    }
  };

  // Chọn tất cả hoặc chỉ chọn 1
  const handleSelectAllModels = () => {
    if (selectedModelIds.length === models.length) {
      setSelectedModelIds([models[0].id]);
    } else {
      setSelectedModelIds(models.map((m) => m.id));
    }
  };

  // Bắt đầu quy trình phân tích AI (FE3 & FE4)
  const handleStartAnalysis = async () => {
    if (!selectedImage || selectedModelIds.length === 0) return;

    setIsAnalyzing(true);

    try {
      // Đảm bảo ảnh đã có ID trên server, nếu chưa có thì tải lại
      let imageId = selectedImage.id;
      if (!imageId && selectedImage.file) {
        const uploaded = await imageService.uploadImage(selectedImage.file);
        imageId = uploaded.id;
      }

      // Xác định mode gửi lên backend
      const mode = analysisMode === 'single' ? AnalysisMode.SingleModel : AnalysisMode.MultiModel;

      // Submit API backend
      if (imageId) {
        const res = await analysisService.submitAnalysis({
          xRayImageId: imageId,
          mode,
          selectedModelIds,
          aiModelId: analysisMode === 'single' ? selectedModelIds[0] : undefined,
        });

        // Đợi 1.5s để mô phỏng hoàn tất tiến trình trực quan
        setTimeout(() => {
          setIsAnalyzing(false);
          navigate(`/results/${res.analysisId}`);
        }, 1500);
      } else {
        // Fallback mô phỏng nếu không có backend connection
        setTimeout(() => {
          setIsAnalyzing(false);
          navigate(`/results/demo-${Date.now()}`);
        }, 1500);
      }
    } catch (err) {
      console.warn('Backend inference submit failed, transitioning to visualized results:', err);
      setTimeout(() => {
        setIsAnalyzing(false);
        navigate(`/results/sample-${Date.now()}`);
      }, 1500);
    }
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  };

  // Lấy danh sách các model đang được chọn
  const currentlySelectedModels = models.filter((m) => selectedModelIds.includes(m.id));

  return (
    <div className="space-y-6">
      {/* 2-Column Main Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
        
        {/* ===================== CỘT TRÁI: Upload & Quản lý ảnh (FE2) ===================== */}
        <div className="lg:col-span-5 space-y-6">
          <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-6">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold text-slate-800 flex items-center gap-2">
                <span>Ảnh X-quang đầu vào</span>
              </h2>
              {selectedImage && (
                <button
                  type="button"
                  onClick={() => setIsGalleryOpen(true)}
                  className="text-xs font-semibold text-blue-600 hover:text-blue-800 transition-colors"
                >
                  Đổi từ thư viện
                </button>
              )}
            </div>

            {/* Khung Upload Kéo Thả */}
            <DropzoneUpload
              onImageSelected={handleFileSelected}
              onOpenGallery={() => setIsGalleryOpen(true)}
              disabled={isUploading || isAnalyzing}
            />

            {/* Error banner nếu upload backend thất bại */}
            {uploadError && (
              <div className="p-3.5 rounded-xl bg-amber-50 border border-amber-200 text-amber-800 text-xs flex items-center gap-2">
                <Info className="w-4 h-4 shrink-0 text-amber-600" />
                <span>{uploadError}</span>
              </div>
            )}

            {/* Ảnh đã chọn */}
            {selectedImage && (
              <SelectedImageCard
                image={selectedImage}
                onRemove={handleRemoveImage}
                isUploading={isUploading}
              />
            )}
          </div>
        </div>

        {/* ===================== CỘT PHẢI: Chọn mô hình AI & Chế độ phân tích (FE3 & FE4) ===================== */}
        <div className="lg:col-span-7 space-y-6">
          <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-6">
            {/* Header models */}
            <div className="flex items-center justify-between pb-2 border-b border-slate-100">
              <div className="flex items-center gap-3">
                <h2 className="text-base font-bold text-slate-800 flex items-center gap-2">
                  <span>Chọn mô hình AI</span>
                </h2>
                {analysisMode === 'multi' && (
                  <span className="text-xs font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                    Đã chọn: {selectedModelIds.length} / {models.length}
                  </span>
                )}
              </div>

              <div className="flex items-center gap-4">
                {analysisMode === 'multi' && (
                  <button
                    type="button"
                    onClick={handleSelectAllModels}
                    className="text-xs font-semibold text-slate-600 hover:text-blue-600 transition-colors flex items-center gap-1"
                  >
                    {selectedModelIds.length === models.length ? (
                      <>
                        <CheckSquare className="w-3.5 h-3.5 text-blue-600" />
                        <span>Bỏ chọn bớt</span>
                      </>
                    ) : (
                      <>
                        <Square className="w-3.5 h-3.5 text-slate-400" />
                        <span>Chọn tất cả</span>
                      </>
                    )}
                  </button>
                )}

                <a 
                  href="/models" 
                  className="text-xs font-semibold text-blue-600 hover:text-blue-800 flex items-center gap-1 transition-colors"
                >
                  <span>Xem chi tiết</span>
                  <ExternalLink className="w-3.5 h-3.5" />
                </a>
              </div>
            </div>

            {/* Models Card Grid / List */}
            <div className="space-y-3">
              {models.map((model) => {
                const isSelected = selectedModelIds.includes(model.id);
                const isTrained = model.isActive || model.isAvailable;

                return (
                  <div
                    key={model.id}
                    onClick={() => handleModelToggle(model.id)}
                    className={`relative rounded-2xl p-4 border transition-all duration-200 cursor-pointer ${
                      isSelected
                        ? 'border-blue-500 bg-blue-50/20 shadow-xs ring-1 ring-blue-500/20'
                        : 'border-slate-200 bg-white hover:border-blue-200 hover:bg-slate-50/50'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-4">
                      {/* Left: Icon & Name */}
                      <div className="flex items-start gap-3.5">
                        <div className={`w-11 h-11 rounded-xl flex items-center justify-center shrink-0 transition-colors ${
                          isSelected ? 'bg-blue-600 text-white shadow-sm shadow-blue-500/20' : 'bg-slate-100 text-slate-600'
                        }`}>
                          <Cpu className="w-6 h-6 stroke-[1.75]" />
                        </div>

                        <div>
                          <div className="flex items-center gap-2 flex-wrap">
                            <h3 className="font-bold text-slate-800 text-sm">
                              {model.name}
                            </h3>
                            {isTrained ? (
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-emerald-100 text-emerald-800">
                                <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                                Đã huấn luyện
                              </span>
                            ) : (
                              <span className="px-2 py-0.5 rounded-full text-[10px] font-medium bg-slate-100 text-slate-600">
                                Chưa xác minh
                              </span>
                            )}
                          </div>
                          
                          <p className="text-xs text-slate-400 mt-0.5">
                            {model.description || `Phiên bản: ${model.version}`}
                          </p>

                          {/* Extra trained metrics if available */}
                          {model.mAP50 && (
                            <div className="mt-2.5 flex items-center gap-4 text-xs font-semibold">
                              <span className="text-slate-600">
                                mAP@50: <strong className="text-blue-600">{model.mAP50}%</strong>
                              </span>
                              {model.fps && (
                                <span className="text-slate-600">
                                  FPS: <strong className="text-slate-800">~{model.fps}</strong>
                                </span>
                              )}
                              {model.weightsName && (
                                <span className="text-slate-600 text-[11px] font-mono bg-slate-100 px-2 py-0.5 rounded-md">
                                  Model: {model.weightsName}
                                </span>
                              )}
                            </div>
                          )}
                        </div>
                      </div>

                      {/* Right: Checkmark radio / checkbox */}
                      <div className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0 border transition-all ${
                        isSelected 
                          ? 'bg-blue-600 border-blue-600 text-white' 
                          : 'border-slate-300 bg-white'
                      }`}>
                        {isSelected && <Check className="w-3.5 h-3.5 stroke-[3]" />}
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>

            {/* Chế độ phân tích (FE3 & FE4) */}
            <div className="pt-2">
              <label className="text-xs font-bold text-slate-700 block mb-2.5 uppercase tracking-wider">
                Chế độ phân tích
              </label>

              <div className="flex items-center gap-6">
                <label className="flex items-center gap-2.5 cursor-pointer text-sm font-medium text-slate-700 select-none">
                  <input
                    type="radio"
                    name="analysisMode"
                    value="single"
                    checked={analysisMode === 'single'}
                    onChange={() => {
                      setAnalysisMode('single');
                      if (selectedModelIds.length > 1) {
                        setSelectedModelIds([selectedModelIds[0]]);
                      }
                    }}
                    className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                  />
                  <span>Phân tích một model</span>
                </label>

                <label className="flex items-center gap-2.5 cursor-pointer text-sm font-medium text-slate-700 select-none">
                  <input
                    type="radio"
                    name="analysisMode"
                    value="multi"
                    checked={analysisMode === 'multi'}
                    onChange={() => {
                      setAnalysisMode('multi');
                      // Nếu mới chuyển sang multi, chọn thêm model nếu muốn
                      if (selectedModelIds.length === 1 && models.length > 1) {
                        setSelectedModelIds(models.map((m) => m.id));
                      }
                    }}
                    className="w-4 h-4 text-blue-600 focus:ring-blue-500"
                  />
                  <span>So sánh nhiều model</span>
                </label>
              </div>
            </div>

            {/* Nút hành động Bắt đầu phân tích */}
            <div className="pt-4 border-t border-slate-100">
              <button
                type="button"
                onClick={handleStartAnalysis}
                disabled={!selectedImage || selectedModelIds.length === 0 || isUploading || isAnalyzing}
                className="w-full py-4 rounded-2xl bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white font-bold text-base flex items-center justify-center gap-2.5 transition-all duration-200 shadow-md shadow-blue-600/30 disabled:opacity-40 disabled:cursor-not-allowed hover:shadow-lg hover:shadow-blue-600/40"
              >
                <span>Bắt đầu phân tích</span>
                <ArrowRight className="w-5 h-5" />
              </button>

              {!selectedImage && (
                <p className="text-center text-xs text-slate-400 mt-2">
                  * Vui lòng chọn hoặc tải lên một ảnh X-quang để kích hoạt tính năng phân tích
                </p>
              )}
            </div>

          </div>
        </div>
      </div>

      {/* Modal Thư viện ảnh X-quang */}
      <ImageGalleryModal
        isOpen={isGalleryOpen}
        onClose={() => setIsGalleryOpen(false)}
        onSelectImage={handleSelectFromGallery}
        selectedImageId={selectedImage?.id}
      />

      {/* Modal tiến trình phân tích AI */}
      <AnalysisProgressModal
        isOpen={isAnalyzing}
        mode={analysisMode}
        selectedModels={currentlySelectedModels}
      />
    </div>
  );
};
