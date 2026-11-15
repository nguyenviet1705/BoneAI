import React, { useState } from 'react';

interface Props {
  data: Array<{ confidence: number; f1: number }>;
}

export const F1CurveChart: React.FC<Props> = ({ data }) => {
  const [hoverIndex, setHoverIndex] = useState<number | null>(null);

  const width = 420;
  const height = 180;
  const padding = { top: 20, right: 20, bottom: 30, left: 35 };

  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;

  const getX = (conf: number) => padding.left + conf * chartWidth;
  const getY = (f1: number) => padding.top + chartHeight - f1 * chartHeight;

  const f1Path = data
    .map((d, i) => `${i === 0 ? 'M' : 'L'} ${getX(d.confidence)} ${getY(d.f1)}`)
    .join(' ');

  const activePoint = hoverIndex !== null ? data[hoverIndex] : null;

  return (
    <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col justify-between">
      {/* Header */}
      <div className="flex items-center justify-between mb-2">
        <h4 className="text-xs font-bold text-slate-800">F1 Curve</h4>
        <span className="text-[10px] text-blue-600 font-mono font-bold">
          Max F1: 0.900 at conf: 0.50
        </span>
      </div>

      {/* SVG Canvas */}
      <div className="relative w-full aspect-[16/7] flex items-center justify-center">
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
          {[0, 0.25, 0.5, 0.75, 1.0].map((conf) => (
            <g key={conf}>
              <line
                x1={getX(conf)}
                y1={height - padding.bottom}
                x2={getX(conf)}
                y2={height - padding.bottom + 4}
                stroke="#cbd5e1"
                strokeWidth="1"
              />
              <text
                x={getX(conf)}
                y={height - padding.bottom + 14}
                textAnchor="middle"
                className="text-[9px] fill-slate-400 font-mono"
              >
                {(conf * 100).toFixed(0)}
              </text>
            </g>
          ))}

          {/* Curve */}
          <path
            d={f1Path}
            fill="none"
            stroke="#2563eb"
            strokeWidth="2.25"
          />

          {/* Interactive rects */}
          {data.map((d, i) => (
            <rect
              key={i}
              x={getX(d.confidence) - chartWidth / (data.length * 2)}
              y={padding.top}
              width={chartWidth / data.length}
              height={chartHeight}
              fill="transparent"
              onMouseEnter={() => setHoverIndex(i)}
              className="cursor-crosshair"
            />
          ))}

          {/* Hover highlight */}
          {activePoint && (
            <g>
              <line
                x1={getX(activePoint.confidence)}
                y1={padding.top}
                x2={getX(activePoint.confidence)}
                y2={height - padding.bottom}
                stroke="#94a3b8"
                strokeWidth="1"
                strokeDasharray="2 2"
              />
              <circle
                cx={getX(activePoint.confidence)}
                cy={getY(activePoint.f1)}
                r="4"
                fill="#2563eb"
                stroke="#ffffff"
                strokeWidth="1.5"
              />
            </g>
          )}
        </svg>

        {/* Floating tooltip */}
        {activePoint && (
          <div className="absolute top-1 right-2 bg-slate-900/90 text-white rounded-lg px-2 py-1 text-[10px] shadow-md pointer-events-none">
            <div className="text-slate-300">Confidence: {(activePoint.confidence * 100).toFixed(0)}%</div>
            <div className="text-blue-400 font-bold">F1: {activePoint.f1.toFixed(3)}</div>
          </div>
        )}
      </div>
    </div>
  );
};
