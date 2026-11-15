import React, { useEffect, useState } from 'react';
import { Loader2, Clock, Cpu } from 'lucide-react';
import type { AIModel } from '../../types/model.types';

interface Props {
  isOpen: boolean;
  mode: 'single' | 'multi';
  selectedModels: AIModel[];
  onCancel?: () => void;
}

export const AnalysisProgressModal: React.FC<Props> = ({
  isOpen,
  mode,
  selectedModels,
}) => {
  const [elapsedSeconds, setElapsedSeconds] = useState<number>(0);

  useEffect(() => {
    if (!isOpen) {
      setElapsedSeconds(0);
      return;
    }
    const interval = setInterval(() => {
      setElapsedSeconds((prev) => +(prev + 0.1).toFixed(1));
    }, 100);
    return () => clearInterval(interval);
  }, [isOpen]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="bg-white rounded-3xl p-7 max-w-md w-full shadow-2xl border border-slate-100 text-center space-y-6">
        
        {/* Animated Scanning Circle */}
        <div className="relative w-20 h-20 mx-auto flex items-center justify-center">
          <div className="absolute inset-0 rounded-full border-4 border-blue-100 animate-ping opacity-30" />
          <div className="w-20 h-20 rounded-full bg-blue-50 border-2 border-blue-500/20 flex items-center justify-center shadow-inner">
            <Loader2 className="w-10 h-10 text-blue-600 animate-spin stroke-[2.2]" />
          </div>
        </div>

        {/* Title & Description */}
        <div className="space-y-1.5">
          <h3 className="text-lg font-bold text-slate-800">
            {mode === 'single' ? 'Đang phân tích ảnh X-quang...' : 'Đang so sánh đa mô hình AI...'}
          </h3>
          <p className="text-xs text-slate-500 max-w-xs mx-auto">
            Hệ thống đang truyền ảnh vào mạng nơ-ron sâu để nhận diện các vùng nghi ngờ gãy xương cổ tay.
          </p>
        </div>

        {/* Models list & Status */}
        <div className="space-y-2 text-left bg-slate-50 p-3.5 rounded-2xl border border-slate-200/60">
          <div className="text-[11px] font-bold text-slate-500 uppercase tracking-wider mb-1 flex items-center justify-between">
            <span>Tiến trình mô hình</span>
            <span className="flex items-center gap-1 font-mono text-blue-600 font-semibold normal-case">
              <Clock className="w-3 h-3" />
              {elapsedSeconds.toFixed(1)}s
            </span>
          </div>

          {selectedModels.map((m, idx) => (
            <div
              key={m.id || idx}
              className="flex items-center justify-between text-xs py-1 px-2 rounded-lg bg-white border border-slate-100"
            >
              <div className="flex items-center gap-2">
                <Cpu className="w-3.5 h-3.5 text-blue-600" />
                <span className="font-semibold text-slate-700">{m.name}</span>
              </div>
              <span className="inline-flex items-center gap-1 text-[11px] text-blue-600 font-medium">
                <Loader2 className="w-3 h-3 animate-spin" />
                Đang xử lý
              </span>
            </div>
          ))}
        </div>

        {/* Progress Bar */}
        <div className="w-full bg-slate-100 rounded-full h-2 overflow-hidden">
          <div className="bg-blue-600 h-full rounded-full animate-pulse w-3/4 transition-all duration-300" />
        </div>

        <p className="text-[11px] text-slate-400">
          * Quá trình thường mất từ 1 đến 3 giây tùy theo số lượng mô hình.
        </p>
      </div>
    </div>
  );
};
