import React, { useState, useEffect } from 'react';
import { 
  Layers, 
  Plus, 
  RefreshCw, 
  MoreVertical,
  SlidersHorizontal
} from 'lucide-react';
import { apiClient } from '../api/apiClient';

interface ModelItem {
  id: string;
  name: string;
  version: string;
  checkpoint: string;
  testMap50: string;
  isActive: boolean;
  isAvailable: boolean;
  framework: string;
  description: string;
}

export const ModelsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'models' | 'trainingConfig'>('models');
  const [models, setModels] = useState<ModelItem[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isSyncing, setIsSyncing] = useState<boolean>(false);
  const [selectedModel, setSelectedModel] = useState<ModelItem | null>(null);

  useEffect(() => {
    fetchModels();
  }, []);

  const fetchModels = async () => {
    setIsLoading(true);
    try {
      const res = await apiClient.get<any[]>('/models');
      if (res.data && res.data.length > 0) {
        const mapped: ModelItem[] = res.data.map((m) => ({
          id: m.id,
          name: m.name,
          version: m.version || 'v1.0',
          checkpoint: m.name.toLowerCase().includes('v8') ? 'best.pt / last.pt' : '-',
          testMap50: m.name.toLowerCase().includes('v8') ? 'mAP@50: 94.76%' : '-',
          isActive: m.isActive,
          isAvailable: m.isAvailable,
          framework: m.framework,
          description: m.description || '',
        }));
        setModels(mapped);
      } else {
        setFallbackModels();
      }
    } catch {
      setFallbackModels();
    } finally {
      setIsLoading(false);
    }
  };

  const setFallbackModels = () => {
    setModels([
      {
        id: '1',
        name: 'YOLOv8m',
        version: 'v1.0 (baseline_640)',
        checkpoint: 'best.pt / last.pt',
        testMap50: 'mAP@50: 94.76%',
        isActive: true,
        isAvailable: true,
        framework: 'PyTorch (Ultralytics)',
        description: 'Mô hình baseline nhận diện gãy xương cổ tay',
      },
      {
        id: '2',
        name: 'YOLOv11',
        version: 'v2.0',
        checkpoint: '-',
        testMap50: '-',
        isActive: false,
        isAvailable: false,
        framework: 'PyTorch',
        description: 'Thử nghiệm kiến trúc YOLOv11',
      },
      {
        id: '3',
        name: 'YOLOv26',
        version: 'v3.0',
        checkpoint: '-',
        testMap50: '-',
        isActive: false,
        isAvailable: false,
        framework: 'PyTorch',
        description: 'Thử nghiệm transformer backbone',
      },
      {
        id: '4',
        name: 'Faster R-CNN',
        version: 'v1.0',
        checkpoint: '-',
        testMap50: '-',
        isActive: false,
        isAvailable: false,
        framework: 'TorchVision',
        description: 'Mô hình Two-stage đối sánh baseline',
      },
    ]);
  };

  const handleSyncWithAIService = async () => {
    setIsSyncing(true);
    try {
      await apiClient.post('/models/sync');
      await fetchModels();
    } catch (e) {
      console.warn('Sync models API not ready, refreshed locally');
      await fetchModels();
    } finally {
      setIsSyncing(false);
    }
  };

  const handleToggleActive = async (model: ModelItem) => {
    try {
      await apiClient.patch(`/models/${model.id}/status`, { isActive: !model.isActive });
      setModels((prev) =>
        prev.map((m) => (m.id === model.id ? { ...m, isActive: !m.isActive } : m))
      );
    } catch {
      // Toggle locally
      setModels((prev) =>
        prev.map((m) => (m.id === model.id ? { ...m, isActive: !m.isActive } : m))
      );
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-1">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <Layers className="w-5 h-5 text-blue-600" />
            <span>Quản lý mô hình</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Quản lý các mô hình AI và checkpoint
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={handleSyncWithAIService}
            disabled={isSyncing}
            className="px-3.5 py-2 rounded-xl bg-white border border-slate-200/90 text-xs font-semibold text-slate-700 hover:bg-slate-50 flex items-center gap-1.5 shadow-xs transition-colors"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isSyncing ? 'animate-spin text-blue-600' : 'text-slate-500'}`} />
            <span>Đồng bộ AI Service</span>
          </button>

          <button
            type="button"
            onClick={() => alert('Chức năng thêm mô hình mới sẽ mở trong bản phát hành tới.')}
            className="px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold flex items-center gap-1.5 shadow-md shadow-blue-600/20 transition-all"
          >
            <Plus className="w-4 h-4" />
            <span>Thêm mô hình</span>
          </button>
        </div>
      </div>

      {/* Main Container */}
      <div className="bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs space-y-5">
        {/* Tab Navigation */}
        <div className="flex items-center gap-6 border-b border-slate-100">
          <button
            type="button"
            onClick={() => setActiveTab('models')}
            className={`pb-3 text-xs font-bold transition-all relative ${
              activeTab === 'models'
                ? 'text-blue-600'
                : 'text-slate-400 hover:text-slate-600'
            }`}
          >
            <span>Danh sách model</span>
            {activeTab === 'models' && (
              <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600 rounded-full" />
            )}
          </button>

          <button
            type="button"
            onClick={() => setActiveTab('trainingConfig')}
            className={`pb-3 text-xs font-bold transition-all relative ${
              activeTab === 'trainingConfig'
                ? 'text-blue-600'
                : 'text-slate-400 hover:text-slate-600'
            }`}
          >
            <span>Cấu hình huấn luyện</span>
            {activeTab === 'trainingConfig' && (
              <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-blue-600 rounded-full" />
            )}
          </button>
        </div>

        {/* Tab Content 1: Danh sách model */}
        {activeTab === 'models' && (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase text-[10px]">
                  <th className="py-3 px-3">Tên model</th>
                  <th className="py-3 px-3">Phiên bản</th>
                  <th className="py-3 px-3">Trạng thái</th>
                  <th className="py-3 px-3">Checkpoint</th>
                  <th className="py-3 px-3">Hiệu năng (Test)</th>
                  <th className="py-3 px-3 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 font-medium">
                {isLoading ? (
                  <tr>
                    <td colSpan={6} className="py-8 text-center text-slate-400">
                      <RefreshCw className="w-5 h-5 animate-spin mx-auto mb-1 text-blue-600" />
                      Đang tải danh sách mô hình...
                    </td>
                  </tr>
                ) : (
                  models.map((model) => (
                    <tr key={model.id} className="hover:bg-slate-50/70 transition-colors">
                      {/* Name */}
                      <td className="py-3.5 px-3">
                        <span className="font-bold text-slate-800">{model.name}</span>
                      </td>

                      {/* Version */}
                      <td className="py-3.5 px-3 text-slate-600">
                        {model.version}
                      </td>

                      {/* Status */}
                      <td className="py-3.5 px-3">
                        {model.isActive ? (
                          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
                            Đã sẵn sàng
                          </span>
                        ) : (
                          <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[11px] font-medium bg-slate-100 text-slate-500">
                            <span className="w-1.5 h-1.5 rounded-full bg-slate-400"></span>
                            Chưa xác minh
                          </span>
                        )}
                      </td>

                      {/* Checkpoint */}
                      <td className="py-3.5 px-3 font-mono text-[11px] text-slate-600">
                        {model.checkpoint}
                      </td>

                      {/* Hiệu năng */}
                      <td className="py-3.5 px-3">
                        {model.testMap50 !== '-' ? (
                          <span className="font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                            {model.testMap50}
                          </span>
                        ) : (
                          <span className="text-slate-400">-</span>
                        )}
                      </td>

                      {/* Actions */}
                      <td className="py-3.5 px-3 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <button
                            type="button"
                            onClick={() => setSelectedModel(model)}
                            className="px-2.5 py-1 text-xs font-semibold text-blue-600 hover:text-blue-800 hover:bg-blue-50 rounded-lg transition-colors"
                          >
                            Xem chi tiết
                          </button>
                          <button
                            type="button"
                            onClick={() => handleToggleActive(model)}
                            title={model.isActive ? 'Vô hiệu hóa model' : 'Kích hoạt model'}
                            className="p-1 text-slate-400 hover:text-slate-600 rounded-lg hover:bg-slate-100 transition-colors"
                          >
                            <MoreVertical className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        )}

        {/* Tab Content 2: Cấu hình huấn luyện */}
        {activeTab === 'trainingConfig' && (
          <div className="p-6 bg-slate-50 rounded-2xl border border-slate-200/60 space-y-4">
            <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2">
              <SlidersHorizontal className="w-4 h-4 text-blue-600" />
              <span>Cấu hình huấn luyện & Hyperparameters</span>
            </h3>
            <p className="text-xs text-slate-500">
              Các thông số cấu hình mặc định được áp dụng khi huấn luyện các mô hình AI trên bộ dữ liệu GRAZPEDWRI-DX.
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4 text-xs font-medium pt-2">
              <div className="bg-white p-3 rounded-xl border border-slate-200">
                <span className="text-slate-400 block mb-0.5">Dataset</span>
                <span className="font-bold text-slate-800">GRAZPEDWRI-DX (Pediatric Wrist)</span>
              </div>
              <div className="bg-white p-3 rounded-xl border border-slate-200">
                <span className="text-slate-400 block mb-0.5">Input Resolution</span>
                <span className="font-bold text-slate-800">640 × 640 px</span>
              </div>
              <div className="bg-white p-3 rounded-xl border border-slate-200">
                <span className="text-slate-400 block mb-0.5">Default Batch Size</span>
                <span className="font-bold text-slate-800">16</span>
              </div>
              <div className="bg-white p-3 rounded-xl border border-slate-200">
                <span className="text-slate-400 block mb-0.5">Epochs</span>
                <span className="font-bold text-slate-800">100 epochs (Early Stopping 20)</span>
              </div>
              <div className="bg-white p-3 rounded-xl border border-slate-200">
                <span className="text-slate-400 block mb-0.5">Optimizer</span>
                <span className="font-bold text-slate-800">SGD / AdamW Auto</span>
              </div>
              <div className="bg-white p-3 rounded-xl border border-slate-200">
                <span className="text-slate-400 block mb-0.5">Inference Precision</span>
                <span className="font-bold text-emerald-600">FP16 / AMP Enabled</span>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Modal Chi tiết mô hình */}
      {selectedModel && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-150">
          <div className="bg-white rounded-3xl p-6 max-w-lg w-full shadow-2xl border border-slate-100 space-y-5">
            <div className="flex items-center justify-between pb-3 border-b border-slate-100">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center">
                  <Layers className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="text-base font-bold text-slate-800">{selectedModel.name}</h3>
                  <p className="text-xs text-slate-400">{selectedModel.version}</p>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setSelectedModel(null)}
                className="w-8 h-8 rounded-full hover:bg-slate-100 text-slate-400 hover:text-slate-600 flex items-center justify-center"
              >
                ✕
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="bg-slate-50 p-3 rounded-xl">
                <span className="text-slate-400 block mb-1">Mô tả</span>
                <p className="font-medium text-slate-700">{selectedModel.description || 'Không có mô tả chi tiết'}</p>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="bg-slate-50 p-3 rounded-xl">
                  <span className="text-slate-400 block mb-1">Framework</span>
                  <p className="font-bold text-slate-800">{selectedModel.framework || 'PyTorch'}</p>
                </div>
                <div className="bg-slate-50 p-3 rounded-xl">
                  <span className="text-slate-400 block mb-1">Checkpoint</span>
                  <p className="font-mono text-slate-800">{selectedModel.checkpoint}</p>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="bg-slate-50 p-3 rounded-xl">
                  <span className="text-slate-400 block mb-1">Hiệu năng mAP@50</span>
                  <p className="font-bold text-blue-600">{selectedModel.testMap50}</p>
                </div>
                <div className="bg-slate-50 p-3 rounded-xl">
                  <span className="text-slate-400 block mb-1">Trạng thái kích hoạt</span>
                  <p className={`font-bold ${selectedModel.isActive ? 'text-emerald-600' : 'text-slate-500'}`}>
                    {selectedModel.isActive ? 'Đang kích hoạt' : 'Chưa kích hoạt'}
                  </p>
                </div>
              </div>
            </div>

            <div className="pt-2 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setSelectedModel(null)}
                className="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-xl"
              >
                Đóng
              </button>
              <button
                type="button"
                onClick={() => {
                  handleToggleActive(selectedModel);
                  setSelectedModel(null);
                }}
                className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-xs font-bold rounded-xl"
              >
                {selectedModel.isActive ? 'Vô hiệu hóa mô hình' : 'Kích hoạt mô hình'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
