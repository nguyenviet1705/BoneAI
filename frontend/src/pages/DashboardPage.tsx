import React, { useEffect, useState } from 'react';
import { 
  Scan, 
  AlertTriangle, 
  Cpu, 
  Clock, 
  ArrowRight
} from 'lucide-react';
import { NavLink, useNavigate } from 'react-router-dom';
import { historyService } from '../api/historyService';
import type { AnalysisHistoryItemDto } from '../types/history.types';

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
  const [recentItems, setRecentItems] = useState<AnalysisHistoryItemDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(12580);

  useEffect(() => {
    historyService.getHistory({ page: 1, pageSize: 3 })
      .then((res) => {
        if (res && res.items && res.items.length > 0) {
          setRecentItems(res.items);
          if (res.pagination?.totalItems) {
            setTotalCount(res.pagination.totalItems);
          }
        } else {
          setFallbackRecent();
        }
      })
      .catch(() => {
        setFallbackRecent();
      });
  }, []);

  const setFallbackRecent = () => {
    setRecentItems([
      {
        analysisId: 'ana-001',
        imageId: 'img-001',
        originalImageUrl: '',
        fileName: 'wrist_001.jpg',
        modelName: 'YOLOv8m',
        createdAt: new Date(Date.now() - 2 * 60 * 1000).toISOString(),
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
        detectionCount: 2,
      },
      {
        analysisId: 'ana-002',
        imageId: 'img-002',
        originalImageUrl: '',
        fileName: 'wrist_002.jpg',
        modelName: 'YOLOv8m',
        createdAt: new Date(Date.now() - 5 * 60 * 1000).toISOString(),
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
        detectionCount: 1,
      },
      {
        analysisId: 'ana-003',
        imageId: 'img-003',
        originalImageUrl: '',
        fileName: 'wrist_003.jpg',
        modelName: 'YOLOv8m',
        createdAt: new Date(Date.now() - 12 * 60 * 1000).toISOString(),
        status: 'Completed',
        modelCount: 1,
        completedModelCount: 1,
        failedModelCount: 0,
        detectionCount: 3,
      },
    ]);
  };

  const getTimeAgo = (isoString: string): string => {
    try {
      const diffMs = Date.now() - new Date(isoString).getTime();
      const diffMin = Math.floor(diffMs / 60000);
      if (diffMin < 1) return 'Vừa xong';
      if (diffMin < 60) return `${diffMin} phút trước`;
      const diffHour = Math.floor(diffMin / 60);
      if (diffHour < 24) return `${diffHour} giờ trước`;
      return `${Math.floor(diffHour / 24)} ngày trước`;
    } catch {
      return 'Gần đây';
    }
  };

  return (
    <div className="space-y-8 relative">
      {/* Top 4 Stat Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {/* Card 1: Tổng ảnh đã phân tích */}
        <div className="bg-white rounded-3xl p-5 border border-slate-200/80 shadow-xs flex items-center justify-between">
          <div>
            <div className="w-10 h-10 rounded-2xl bg-blue-50 text-blue-600 flex items-center justify-center mb-3">
              <Scan className="w-5 h-5" />
            </div>
            <p className="text-2xl font-black text-slate-800 tracking-tight">{totalCount.toLocaleString()}</p>
            <div className="flex items-center gap-1.5 mt-1">
              <span className="text-xs text-slate-500 font-medium">Tổng ảnh đã phân tích</span>
              <span className="text-[11px] font-bold text-emerald-600 bg-emerald-50 px-1.5 py-0.5 rounded-md flex items-center">
                ↑ 12%
              </span>
            </div>
          </div>
        </div>

        {/* Card 2: Trường hợp nghi ngờ */}
        <div className="bg-white rounded-3xl p-5 border border-slate-200/80 shadow-xs flex items-center justify-between">
          <div>
            <div className="w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center mb-3">
              <AlertTriangle className="w-5 h-5" />
            </div>
            <p className="text-2xl font-black text-slate-800 tracking-tight">842</p>
            <div className="flex items-center gap-1.5 mt-1">
              <span className="text-xs text-slate-500 font-medium">Trường hợp nghi ngờ</span>
              <span className="text-[11px] font-bold text-emerald-600 bg-emerald-50 px-1.5 py-0.5 rounded-md flex items-center">
                ↑ 8%
              </span>
            </div>
          </div>
        </div>

        {/* Card 3: Model AI hiện có */}
        <div className="bg-white rounded-3xl p-5 border border-slate-200/80 shadow-xs flex items-center justify-between">
          <div>
            <div className="w-10 h-10 rounded-2xl bg-emerald-50 text-emerald-600 flex items-center justify-center mb-3">
              <Cpu className="w-5 h-5" />
            </div>
            <p className="text-2xl font-black text-slate-800 tracking-tight">4</p>
            <p className="text-xs text-slate-500 font-medium mt-1">Model AI hiện có</p>
          </div>
        </div>

        {/* Card 4: Thời gian TB */}
        <div className="bg-white rounded-3xl p-5 border border-slate-200/80 shadow-xs flex items-center justify-between">
          <div>
            <div className="w-10 h-10 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center mb-3">
              <Clock className="w-5 h-5" />
            </div>
            <p className="text-2xl font-black text-slate-800 tracking-tight">1.8s</p>
            <p className="text-xs text-slate-500 font-medium mt-1">Thời gian TB</p>
          </div>
        </div>
      </div>

      {/* Row 2: Charts Area */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch">
        
        {/* Left: Thống kê hoạt động (SVG Activity Chart) */}
        <div className="lg:col-span-8 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-base font-bold text-slate-800">Thống kê hoạt động</h3>
            <div className="flex items-center gap-4 text-xs font-semibold">
              <span className="flex items-center gap-1.5 text-blue-600">
                <span className="w-2.5 h-2.5 rounded-full bg-blue-600"></span>
                Phân tích
              </span>
              <span className="flex items-center gap-1.5 text-indigo-500">
                <span className="w-2.5 h-2.5 rounded-full bg-indigo-500"></span>
                Nghi ngờ gãy xương
              </span>
            </div>
          </div>

          {/* SVG Smooth Curve Activity Chart */}
          <div className="relative w-full aspect-[21/9]">
            <svg viewBox="0 0 700 220" className="w-full h-full overflow-visible">
              <defs>
                <linearGradient id="blueGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="#2563eb" stopOpacity="0.25" />
                  <stop offset="100%" stopColor="#2563eb" stopOpacity="0.0" />
                </linearGradient>
              </defs>

              {/* Grid Lines */}
              <line x1="40" y1="20" x2="680" y2="20" stroke="#f1f5f9" strokeWidth="1" />
              <line x1="40" y1="100" x2="680" y2="100" stroke="#f1f5f9" strokeWidth="1" />
              <line x1="40" y1="180" x2="680" y2="180" stroke="#f1f5f9" strokeWidth="1" />

              {/* Y Axis labels */}
              <text x="30" y="24" textAnchor="end" className="text-[10px] fill-slate-400 font-mono">1.5k</text>
              <text x="30" y="104" textAnchor="end" className="text-[10px] fill-slate-400 font-mono">500</text>
              <text x="30" y="184" textAnchor="end" className="text-[10px] fill-slate-400 font-mono">0</text>

              {/* Area Fill */}
              <path
                d="M 60,140 Q 150,110 240,85 T 420,105 T 560,45 T 660,95 L 660,180 L 60,180 Z"
                fill="url(#blueGradient)"
              />

              {/* Blue Line (Total Analysis) */}
              <path
                d="M 60,140 Q 150,110 240,85 T 420,105 T 560,45 T 660,95"
                fill="none"
                stroke="#2563eb"
                strokeWidth="3"
                strokeLinecap="round"
              />

              {/* Highlight Pin at T11 (1,248 ảnh) */}
              <circle cx="560" cy="45" r="5" fill="#2563eb" stroke="#ffffff" strokeWidth="2.5" />
              
              {/* Tooltip bubble on chart */}
              <g transform="translate(525, 10)">
                <rect width="70" height="24" rx="6" fill="#0d1b3e" />
                <text x="35" y="16" textAnchor="middle" fill="#ffffff" className="text-[10px] font-bold font-mono">
                  1,248 ảnh
                </text>
              </g>

              {/* X Axis Labels */}
              {['T7', 'T8', 'T9', 'T10', 'T11', 'T12'].map((month, idx) => (
                <text
                  key={month}
                  x={60 + idx * 120}
                  y="205"
                  textAnchor="middle"
                  className="text-[11px] fill-slate-400 font-bold"
                >
                  {month}
                </text>
              ))}
            </svg>
          </div>
        </div>

        {/* Right: Model AI đang sử dụng (Donut Chart) */}
        <div className="lg:col-span-4 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <h3 className="text-base font-bold text-slate-800 mb-2">Model AI đang sử dụng</h3>

          {/* Donut Chart representation */}
          <div className="relative w-44 h-44 mx-auto flex items-center justify-center my-3">
            <svg viewBox="0 0 100 100" className="w-full h-full -rotate-90">
              {/* Circle segments */}
              <circle cx="50" cy="50" r="38" fill="transparent" stroke="#2563eb" strokeWidth="14" strokeDasharray="140 240" strokeDashoffset="0" />
              <circle cx="50" cy="50" r="38" fill="transparent" stroke="#38bdf8" strokeWidth="14" strokeDasharray="40 240" strokeDashoffset="-140" />
              <circle cx="50" cy="50" r="38" fill="transparent" stroke="#a855f7" strokeWidth="14" strokeDasharray="30 240" strokeDashoffset="-180" />
              <circle cx="50" cy="50" r="38" fill="transparent" stroke="#f43f5e" strokeWidth="14" strokeDasharray="30 240" strokeDashoffset="-210" />
            </svg>
            <div className="absolute inset-0 flex flex-col items-center justify-center text-center">
              <span className="text-xs font-black text-slate-800">YOLOv8m</span>
              <span className="text-[10px] text-slate-400 font-medium">v1.0</span>
            </div>
          </div>

          {/* Legend */}
          <div className="space-y-2 text-xs font-semibold pt-2">
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2 text-slate-700">
                <span className="w-2.5 h-2.5 rounded-full bg-blue-600"></span>
                YOLOv8m
              </span>
              <span className="text-slate-400 font-mono">60%</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2 text-slate-700">
                <span className="w-2.5 h-2.5 rounded-full bg-sky-400"></span>
                YOLOv11
              </span>
              <span className="text-slate-400 font-mono">18%</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2 text-slate-700">
                <span className="w-2.5 h-2.5 rounded-full bg-purple-500"></span>
                YOLOv26
              </span>
              <span className="text-slate-400 font-mono">12%</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2 text-slate-700">
                <span className="w-2.5 h-2.5 rounded-full bg-rose-500"></span>
                Faster R-CNN
              </span>
              <span className="text-slate-400 font-mono">10%</span>
            </div>
          </div>
        </div>

      </div>

      {/* Row 3: Quick Info & Recent History */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        
        {/* Quick Info Card */}
        <div className="lg:col-span-5 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-base font-bold text-slate-800">Thông tin nhanh</h3>
            <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
              Đang sẵn sàng
            </span>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-20 h-20 rounded-2xl bg-slate-900 border border-slate-200 flex items-center justify-center overflow-hidden shrink-0 shadow-inner">
              <img
                src="https://placehold.co/100x100/0d1b3e/ffffff?text=X-Ray"
                alt="X-Ray Hand"
                className="w-full h-full object-cover"
              />
            </div>

            <div className="space-y-1.5 text-xs">
              <p className="font-semibold text-slate-700">
                Model: <strong className="text-slate-900">YOLOv8m</strong>
              </p>
              <p className="font-semibold text-slate-700">
                Dataset: <strong className="text-slate-900">GRAZPEDWRI-DX</strong>
              </p>
              <p className="font-semibold text-slate-700">
                Input size: <strong className="text-slate-900">640 × 640</strong>
              </p>
            </div>
          </div>

          <div className="mt-5 pt-4 border-t border-slate-100 flex items-center justify-between">
            <NavLink
              to="/analyze"
              className="w-full py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-semibold text-xs flex items-center justify-center gap-2 transition-all shadow-sm shadow-blue-500/25"
            >
              <span>Phân tích ảnh ngay</span>
              <ArrowRight className="w-4 h-4" />
            </NavLink>
          </div>
        </div>

        {/* Recent History */}
        <div className="lg:col-span-7 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-base font-bold text-slate-800">Lịch sử gần đây</h3>
            <NavLink to="/history" className="text-xs font-semibold text-blue-600 hover:text-blue-800 transition-colors">
              Xem tất cả →
            </NavLink>
          </div>

          <div className="space-y-3">
            {recentItems.map((item) => (
              <div 
                key={item.analysisId}
                onClick={() => navigate(`/results/${item.analysisId}`)}
                className="flex items-center justify-between p-3 rounded-2xl bg-slate-50/70 border border-slate-100 hover:bg-blue-50/40 hover:border-blue-100 transition-all cursor-pointer group"
              >
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-slate-900 flex items-center justify-center text-[10px] text-white font-mono shadow-xs">
                    RX
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-800 group-hover:text-blue-600 transition-colors">
                      {item.fileName || `wrist_${item.analysisId.slice(0, 4)}.jpg`}
                    </p>
                    <p className="text-[11px] text-slate-400">
                      Đã phát hiện {item.detectionCount ?? 1} vùng
                    </p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <span className="text-[11px] font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                    {item.modelName || 'YOLOv8m'}
                  </span>
                  <span className="text-[11px] text-slate-400 font-mono">
                    {getTimeAgo(item.createdAt)}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
