import React from 'react';
import { AlertCircle } from 'lucide-react';
import type { DetectionItemDto } from '../../types/analysis.types';

interface Props {
  detections: DetectionItemDto[];
  highlightedDetectionId?: string | null;
  onHoverDetection?: (id: string | null) => void;
}

export const DetectionsTable: React.FC<Props> = ({
  detections,
  highlightedDetectionId,
  onHoverDetection,
}) => {
  return (
    <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-bold text-slate-800 flex items-center gap-2">
          <span>Danh sách vùng nghi ngờ</span>
          <span className="w-5 h-5 rounded-full bg-blue-100 text-blue-600 text-[11px] font-bold flex items-center justify-center">
            {detections.length}
          </span>
        </h3>
      </div>

      {detections.length === 0 ? (
        <div className="py-6 text-center text-slate-400 text-xs flex flex-col items-center gap-1.5">
          <AlertCircle className="w-6 h-6 text-slate-300" />
          <span>Không phát hiện tổn thương hoặc vùng nghi ngờ gãy xương.</span>
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead>
              <tr className="border-b border-slate-100 text-slate-400 font-semibold uppercase text-[10px]">
                <th className="py-2 px-2.5 w-8">#</th>
                <th className="py-2 px-2.5">Nhãn</th>
                <th className="py-2 px-2.5">Confidence</th>
                <th className="py-2 px-2.5 text-right">Vị trí</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {detections.map((det, index) => {
                const isHovered = highlightedDetectionId === det.detectionId;
                const conf = (det.confidenceScore > 1 ? det.confidenceScore : det.confidenceScore * 100).toFixed(1);
                const { x, y, width, height } = det.boundingBox;
                const posStr = `[${Math.round(x)}, ${Math.round(y)}, ${Math.round(width)}, ${Math.round(height)}]`;

                return (
                  <tr
                    key={det.detectionId || index}
                    onMouseEnter={() => onHoverDetection?.(det.detectionId)}
                    onMouseLeave={() => onHoverDetection?.(null)}
                    className={`transition-colors cursor-pointer ${
                      isHovered ? 'bg-blue-50/60 font-semibold' : 'hover:bg-slate-50'
                    }`}
                  >
                    <td className="py-2.5 px-2.5 font-bold text-slate-500">
                      {index + 1}
                    </td>
                    <td className="py-2.5 px-2.5">
                      <span className="capitalize font-semibold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                        {det.classLabel || 'fracture'}
                      </span>
                    </td>
                    <td className="py-2.5 px-2.5 font-bold text-slate-800">
                      {conf}%
                    </td>
                    <td className="py-2.5 px-2.5 font-mono text-[11px] text-slate-500 text-right">
                      {posStr}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
