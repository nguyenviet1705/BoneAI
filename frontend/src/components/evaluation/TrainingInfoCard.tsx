import React from 'react';
import { Settings } from 'lucide-react';
import type { TrainingConfig } from '../../types/evaluation.types';

interface Props {
  config: TrainingConfig;
}

export const TrainingInfoCard: React.FC<Props> = ({ config }) => {
  return (
    <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col justify-between">
      {/* Header */}
      <div className="flex items-center justify-between mb-3">
        <h4 className="text-xs font-bold text-slate-800 flex items-center gap-1.5">
          <Settings className="w-3.5 h-3.5 text-blue-600" />
          <span>Thông tin huấn luyện</span>
        </h4>
        <span className="text-[10px] text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full font-bold">
          Sẵn sàng
        </span>
      </div>

      {/* Attributes table */}
      <div className="space-y-1.5 text-xs">
        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Model</span>
          <span className="font-bold text-slate-800">{config.modelName}</span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Dataset</span>
          <span className="font-semibold text-blue-700 bg-blue-50 px-1.5 py-0.2 rounded font-mono text-[11px]">
            {config.dataset}
          </span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Image size</span>
          <span className="font-medium text-slate-700">{config.imageSize}</span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Batch size</span>
          <span className="font-mono text-slate-700">{config.batchSize}</span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Max epochs</span>
          <span className="font-mono text-slate-700">{config.maxEpochs}</span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Patience</span>
          <span className="font-mono text-slate-700">{config.patience}</span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">Optimizer</span>
          <span className="font-medium text-slate-700">{config.optimizer}</span>
        </div>

        <div className="flex items-center justify-between py-1 border-b border-slate-50">
          <span className="text-slate-400">AMP</span>
          <span className="text-emerald-600 font-bold">{config.amp ? 'Enabled' : 'Disabled'}</span>
        </div>

        <div className="flex items-center justify-between py-1">
          <span className="text-slate-400">GPU</span>
          <span className="font-mono text-slate-700">{config.gpu}</span>
        </div>
      </div>
    </div>
  );
};
