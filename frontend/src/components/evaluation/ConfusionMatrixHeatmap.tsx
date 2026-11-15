import React from 'react';
import type { ConfusionMatrixData } from '../../types/evaluation.types';

interface Props {
  data: ConfusionMatrixData;
}

export const ConfusionMatrixHeatmap: React.FC<Props> = ({ data }) => {
  const { tp, fn, fp, tn } = data;
  const total = tp + fn + fp + tn;

  const tpPct = ((tp / total) * 100).toFixed(1);
  const fnPct = ((fn / total) * 100).toFixed(1);
  const fpPct = ((fp / total) * 100).toFixed(1);
  const tnPct = ((tn / total) * 100).toFixed(1);

  return (
    <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col justify-between">
      {/* Header */}
      <div className="flex items-center justify-between mb-3">
        <h4 className="text-xs font-bold text-slate-800">Confusion Matrix</h4>
        <span className="text-[10px] text-slate-400 font-medium">
          Tổng mẫu: {total.toLocaleString()}
        </span>
      </div>

      <div className="flex items-center gap-4">
        {/* Y-axis Label */}
        <div className="w-5 flex items-center justify-center">
          <span className="-rotate-90 text-[10px] font-bold text-slate-400 uppercase tracking-widest whitespace-nowrap">
            Thực tế
          </span>
        </div>

        {/* Matrix Grid */}
        <div className="flex-1 space-y-1.5">
          {/* Top Row: TP and FN */}
          <div className="grid grid-cols-2 gap-1.5">
            {/* TP */}
            <div className="bg-blue-600 text-white rounded-xl p-3 flex flex-col items-center justify-center shadow-xs">
              <span className="text-base font-black tracking-tight">{tp.toLocaleString()}</span>
              <span className="text-[10px] text-blue-100 font-medium">{tpPct}% (TP)</span>
            </div>

            {/* FN */}
            <div className="bg-blue-100 text-blue-900 rounded-xl p-3 flex flex-col items-center justify-center">
              <span className="text-sm font-bold">{fn.toLocaleString()}</span>
              <span className="text-[10px] text-blue-700 font-medium">{fnPct}% (FN)</span>
            </div>
          </div>

          {/* Bottom Row: FP and TN */}
          <div className="grid grid-cols-2 gap-1.5">
            {/* FP */}
            <div className="bg-blue-50 text-blue-900 rounded-xl p-3 flex flex-col items-center justify-center">
              <span className="text-sm font-bold">{fp.toLocaleString()}</span>
              <span className="text-[10px] text-blue-700 font-medium">{fpPct}% (FP)</span>
            </div>

            {/* TN */}
            <div className="bg-blue-500 text-white rounded-xl p-3 flex flex-col items-center justify-center shadow-xs">
              <span className="text-base font-black tracking-tight">{tn.toLocaleString()}</span>
              <span className="text-[10px] text-blue-100 font-medium">{tnPct}% (TN)</span>
            </div>
          </div>

          {/* X-axis Label */}
          <div className="text-center pt-1">
            <span className="text-[10px] font-bold text-slate-400 uppercase tracking-widest">
              Dự đoán
            </span>
          </div>
        </div>

        {/* Gradient Legend Scale Bar */}
        <div className="flex flex-col items-center gap-1 shrink-0">
          <span className="text-[9px] font-mono text-slate-400">2k</span>
          <div className="w-3 h-20 rounded-full bg-gradient-to-t from-blue-50 via-blue-400 to-blue-600 border border-slate-200" />
          <span className="text-[9px] font-mono text-slate-400">1k</span>
          <span className="text-[9px] font-mono text-slate-400">0</span>
        </div>
      </div>
    </div>
  );
};
