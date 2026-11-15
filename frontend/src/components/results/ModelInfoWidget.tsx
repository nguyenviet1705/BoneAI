import React from 'react';
import { Cpu, ShieldCheck, HardDrive, Zap } from 'lucide-react';
import type { ModelInfoDto } from '../../types/analysis.types';

interface Props {
  model: ModelInfoDto;
  processingTimeMs?: number | null;
}

export const ModelInfoWidget: React.FC<Props> = ({ model, processingTimeMs }) => {
  const speedDisplay = processingTimeMs ? `${processingTimeMs.toFixed(1)} ms/ảnh` : '13.6 ms/ảnh';

  return (
    <div className="space-y-4">
      {/* Model Tech Info Card */}
      <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs space-y-3">
        <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2">
          <span>Thông tin model</span>
        </h3>

        <div className="space-y-2">
          <div className="flex items-center gap-2 text-xs font-bold text-slate-800">
            <Cpu className="w-4 h-4 text-blue-600" />
            <span>{model.modelName} • {model.version || 'v1.0'}</span>
          </div>

          <div className="flex items-center gap-2 text-xs text-slate-600">
            <HardDrive className="w-4 h-4 text-slate-400" />
            <span className="font-mono bg-slate-100 px-2 py-0.5 rounded text-[11px] font-medium text-slate-700">
              best.pt
            </span>
          </div>

          <div className="flex items-center gap-2 text-xs text-slate-500">
            <Zap className="w-4 h-4 text-amber-500" />
            <span>640 × 640 • {speedDisplay}</span>
          </div>
        </div>
      </div>

      {/* Medical Disclaimer Banner */}
      <div className="rounded-2xl p-4 bg-emerald-50/80 border border-emerald-200/90 text-emerald-900 flex items-start gap-3 shadow-xs">
        <div className="w-8 h-8 rounded-xl bg-emerald-100 text-emerald-700 flex items-center justify-center shrink-0 mt-0.5">
          <ShieldCheck className="w-5 h-5 stroke-[2.2]" />
        </div>
        <p className="text-xs leading-relaxed font-medium">
          Kết quả chỉ mang tính chất hỗ trợ, không thay thế chẩn đoán của bác sĩ.
        </p>
      </div>
    </div>
  );
};
