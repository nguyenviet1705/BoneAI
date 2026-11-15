import React, { useState } from 'react';

interface Props {
  data: Array<{ epoch: number; map50: number; map50_95: number }>;
}

export const MAPEpochChart: React.FC<Props> = ({ data }) => {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  const width = 340;
  const height = 180;
  const padding = { top: 20, right: 20, bottom: 30, left: 35 };

  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;

  const getX = (epoch: number) => padding.left + (epoch / 100) * chartWidth;
  const getY = (score: number) => padding.top + chartHeight - score * chartHeight;

  const map50Path = data
    .map((d, i) => `${i === 0 ? 'M' : 'L'} ${getX(d.epoch)} ${getY(d.map50)}`)
    .join(' ');

  const map50_95Path = data
    .map((d, i) => `${i === 0 ? 'M' : 'L'} ${getX(d.epoch)} ${getY(d.map50_95)}`)
    .join(' ');

  const activePoint = hoverIndex !== null ? data[hoverIndex] : null;

  return (
    <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col justify-between">
      {/* Header & Legend */}
      <div className="flex items-center justify-between mb-2">
        <h4 className="text-xs font-bold text-slate-800">mAP theo Epoch</h4>
        <div className="flex items-center gap-3 text-[10px] font-semibold">
          <span className="flex items-center gap-1 text-blue-600">
            <span className="w-2.5 h-0.5 bg-blue-600"></span>
            mAP@50
          </span>
          <span className="flex items-center gap-1 text-teal-600">
            <span className="w-2.5 h-0.5 bg-teal-500"></span>
            mAP@50-95
          </span>
        </div>
      </div>

      {/* SVG Canvas */}
      <div className="relative w-full aspect-[16/9] flex items-center justify-center">
        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="w-full h-full overflow-visible select-none"
          onMouseLeave={() => setHoverIndex(null)}
        >
          {/* Grid lines */}
          {[0, 0.5, 1.0].map((val) => (
            <g key={val}>
              <line
                x1={padding.left}
                y1={getY(val)}
                x2={width - padding.right}
                y2={getY(val)}
                stroke="#f1f5f9"
                strokeWidth="1"
              />
              <text
                x={padding.left - 8}
                y={getY(val) + 3}
                textAnchor="end"
                className="text-[9px] fill-slate-400 font-mono"
              >
                {val}
              </text>
            </g>
          ))}

          {/* X axis ticks */}
          {[0, 25, 50, 75, 100].map((epoch) => (
            <g key={epoch}>
              <line
                x1={getX(epoch)}
                y1={height - padding.bottom}
                x2={getX(epoch)}
                y2={height - padding.bottom + 4}
                stroke="#cbd5e1"
                strokeWidth="1"
              />
              <text
                x={getX(epoch)}
                y={height - padding.bottom + 14}
                textAnchor="middle"
                className="text-[9px] fill-slate-400 font-mono"
              >
                {epoch}
              </text>
            </g>
          ))}

          {/* Paths */}
          <path
            d={map50Path}
            fill="none"
            stroke="#2563eb"
            strokeWidth="2"
          />
          <path
            d={map50_95Path}
            fill="none"
            stroke="#0d9488"
            strokeWidth="1.75"
          />

          {/* Interactive hover points */}
          {data.map((d, i) => (
            <rect
              key={d.epoch}
              x={getX(d.epoch) - chartWidth / (data.length * 2)}
              y={padding.top}
              width={chartWidth / data.length}
              height={chartHeight}
              fill="transparent"
              onMouseEnter={() => setHoverIndex(i)}
              className="cursor-crosshair"
            />
          ))}

          {/* Highlight point */}
          {activePoint && (
            <g>
              <line
                x1={getX(activePoint.epoch)}
                y1={padding.top}
                x2={getX(activePoint.epoch)}
                y2={height - padding.bottom}
                stroke="#94a3b8"
                strokeWidth="1"
                strokeDasharray="2 2"
              />
              <circle
                cx={getX(activePoint.epoch)}
                cy={getY(activePoint.map50)}
                r="3.5"
                fill="#2563eb"
                stroke="#ffffff"
                strokeWidth="1.5"
              />
              <circle
                cx={getX(activePoint.epoch)}
                cy={getY(activePoint.map50_95)}
                r="3.5"
                fill="#0d9488"
                stroke="#ffffff"
                strokeWidth="1.5"
              />
            </g>
          )}
        </svg>

        {/* Floating tooltip */}
        {activePoint && (
          <div className="absolute top-1 right-2 bg-slate-900/90 text-white rounded-lg px-2 py-1 text-[10px] shadow-md pointer-events-none">
            <div>Epoch: {activePoint.epoch}</div>
            <div className="text-blue-400">mAP@50: {(activePoint.map50 * 100).toFixed(1)}%</div>
            <div className="text-teal-400">mAP@50-95: {(activePoint.map50_95 * 100).toFixed(1)}%</div>
          </div>
        )}
      </div>
    </div>
  );
};
