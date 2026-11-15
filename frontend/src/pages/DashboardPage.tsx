import React from 'react';
import { 
  Scan, 
  AlertTriangle, 
  Cpu, 
  Clock, 
  ArrowRight
} from 'lucide-react';
import { NavLink } from 'react-router-dom';

export const DashboardPage: React.FC = () => {
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
            <p className="text-2xl font-black text-slate-800 tracking-tight">12,580</p>
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

      {/* Middle row: Activity Chart + Model Donut */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Activity Chart */}
        <div className="lg:col-span-8 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h3 className="text-base font-bold text-slate-800">Thống kê hoạt động</h3>
              <p className="text-xs text-slate-400 mt-0.5">Số lượng phân tích theo tháng</p>
            </div>
            <div className="flex items-center gap-4 text-xs font-semibold">
              <span className="flex items-center gap-1.5 text-slate-600">
                <span className="w-2.5 h-2.5 rounded-full bg-blue-600"></span>
                Phân tích
              </span>
              <span className="flex items-center gap-1.5 text-slate-600">
                <span className="w-2.5 h-2.5 rounded-full bg-indigo-400"></span>
                Nghi ngờ gãy xương
              </span>
            </div>
          </div>

          {/* SVG Smooth Curve Simulation */}
          <div className="relative h-56 w-full flex flex-col justify-end">
            <svg viewBox="0 0 700 200" className="w-full h-44 overflow-visible">
              <defs>
                <linearGradient id="gradBlue" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.25" />
                  <stop offset="100%" stopColor="#3b82f6" stopOpacity="0.0" />
                </linearGradient>
              </defs>
              <path
                d="M 20 130 C 120 140, 180 90, 280 100 C 380 110, 480 40, 560 60 C 620 75, 660 110, 680 120 L 680 200 L 20 200 Z"
                fill="url(#gradBlue)"
              />
              <path
                d="M 20 130 C 120 140, 180 90, 280 100 C 380 110, 480 40, 560 60 C 620 75, 660 110, 680 120"
                fill="none"
                stroke="#2563eb"
                strokeWidth="3.5"
                strokeLinecap="round"
              />
              {/* Highlight Pin */}
              <circle cx="560" cy="60" r="5" fill="#2563eb" stroke="#ffffff" strokeWidth="2.5" />
              <g transform="translate(520, 20)">
                <rect width="80" height="24" rx="12" fill="#0f172a" />
                <text x="40" y="16" fill="#ffffff" fontSize="11" fontWeight="bold" textAnchor="middle">
                  1,248 ảnh
                </text>
              </g>
            </svg>

            {/* X-Axis labels */}
            <div className="flex justify-between text-xs font-semibold text-slate-400 px-2 pt-3 border-t border-slate-100">
              <span>T7</span>
              <span>T8</span>
              <span>T9</span>
              <span>T10</span>
              <span>T11</span>
              <span>T12</span>
            </div>
          </div>
        </div>

        {/* Model AI Donut */}
        <div className="lg:col-span-4 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs flex flex-col justify-between">
          <h3 className="text-base font-bold text-slate-800 mb-2">Model AI đang sử dụng</h3>

          <div className="flex flex-col items-center justify-center py-4">
            <div className="relative w-40 h-40 flex items-center justify-center">
              <svg viewBox="0 0 100 100" className="w-full h-full -rotate-90">
                <circle cx="50" cy="50" r="38" stroke="#e2e8f0" strokeWidth="16" fill="none" />
                {/* YOLOv8m (65%) */}
                <circle cx="50" cy="50" r="38" stroke="#2563eb" strokeWidth="16" fill="none" strokeDasharray="155 240" strokeLinecap="round" />
                {/* YOLOv11 (15%) */}
                <circle cx="50" cy="50" r="38" stroke="#38bdf8" strokeWidth="16" fill="none" strokeDasharray="36 240" strokeDashoffset="-158" strokeLinecap="round" />
                {/* YOLOv26 (12%) */}
                <circle cx="50" cy="50" r="38" stroke="#a855f7" strokeWidth="16" fill="none" strokeDasharray="28 240" strokeDashoffset="-196" strokeLinecap="round" />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center text-center">
                <span className="text-xs font-extrabold text-blue-600">YOLOv8m</span>
                <span className="text-[10px] text-slate-400 font-medium">v1.0</span>
              </div>
            </div>
          </div>

          <div className="space-y-2 text-xs font-medium text-slate-600 pt-2 border-t border-slate-100">
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-blue-600"></span>
                YOLOv8m
              </span>
              <span className="font-bold text-slate-800">65%</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-sky-400"></span>
                YOLOv11
              </span>
              <span className="font-bold text-slate-800">15%</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-purple-500"></span>
                YOLOv26
              </span>
              <span className="font-bold text-slate-800">12%</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="flex items-center gap-2">
                <span className="w-2.5 h-2.5 rounded-full bg-slate-300"></span>
                Faster R-CNN
              </span>
              <span className="font-bold text-slate-800">8%</span>
            </div>
          </div>
        </div>
      </div>

      {/* Bottom row: Quick Model Info + Recent History */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        {/* Quick Model Info */}
        <div className="lg:col-span-5 bg-white rounded-3xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-base font-bold text-slate-800">Thông tin nhanh</h3>
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
              Đang sẵn sàng
            </span>
          </div>

          <div className="flex items-center gap-4">
            <div className="w-20 h-20 rounded-2xl bg-slate-900 border border-slate-200 flex items-center justify-center overflow-hidden shrink-0">
              <div className="text-blue-400 text-xs font-mono text-center p-1">
                [X-Ray Wrist]
              </div>
            </div>

            <div className="space-y-1 text-xs">
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
            {[
              { name: 'wrist_001.jpg', model: 'YOLOv8m', time: '2 phút trước' },
              { name: 'wrist_002.jpg', model: 'YOLOv8m', time: '5 phút trước' },
              { name: 'wrist_003.jpg', model: 'YOLOv8m', time: '12 phút trước' },
            ].map((item, idx) => (
              <div key={idx} className="flex items-center justify-between p-3 rounded-2xl bg-slate-50/70 border border-slate-100">
                <div className="flex items-center gap-3">
                  <div className="w-9 h-9 rounded-xl bg-slate-900 flex items-center justify-center text-[10px] text-white font-mono">
                    RX
                  </div>
                  <div>
                    <p className="text-xs font-bold text-slate-800">{item.name}</p>
                    <p className="text-[11px] text-slate-400">Đã phát hiện 1 vùng</p>
                  </div>
                </div>

                <div className="flex items-center gap-3">
                  <span className="text-[11px] font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                    {item.model}
                  </span>
                  <span className="text-[11px] text-slate-400">{item.time}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
