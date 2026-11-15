import React, { useState, useRef, useEffect } from 'react';
import { ZoomIn, ZoomOut, RotateCcw, ImageOff, RefreshCw } from 'lucide-react';
import type { DetectionItemDto } from '../../types/analysis.types';

interface Props {
  title: string;
  imageUrl: string;
  fallbackUrl?: string;
  isProcessing?: boolean;
  detections?: DetectionItemDto[];
  highlightedDetectionId?: string | null;
  onHoverDetection?: (id: string | null) => void;
  showDetections?: boolean;
}

export const XRayImageViewer: React.FC<Props> = ({
  title,
  imageUrl,
  fallbackUrl,
  isProcessing = false,
  detections = [],
  highlightedDetectionId,
  onHoverDetection,
  showDetections = false,
}) => {
  const [scale, setScale] = useState<number>(1);
  const [position, setPosition] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const [isDragging, setIsDragging] = useState<boolean>(false);
  const [currentSrc, setCurrentSrc] = useState<string>(imageUrl);
  const [hasError, setHasError] = useState<boolean>(false);
  const dragStartRef = useRef<{ x: number; y: number }>({ x: 0, y: 0 });

  useEffect(() => {
    setCurrentSrc(imageUrl);
    setHasError(false);
  }, [imageUrl]);

  const handleZoomIn = () => setScale((prev) => Math.min(prev + 0.25, 3));
  const handleZoomOut = () => setScale((prev) => Math.max(prev - 0.25, 0.75));
  const handleReset = () => {
    setScale(1);
    setPosition({ x: 0, y: 0 });
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    if (scale > 1) {
      setIsDragging(true);
      dragStartRef.current = { x: e.clientX - position.x, y: e.clientY - position.y };
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    if (isDragging && scale > 1) {
      setPosition({
        x: e.clientX - dragStartRef.current.x,
        y: e.clientY - dragStartRef.current.y,
      });
    }
  };

  const handleMouseUp = () => setIsDragging(false);

  const handleImageError = () => {
    if (fallbackUrl && currentSrc !== fallbackUrl) {
      // Fallback về ảnh gốc thay vì placeholder bên ngoài
      setCurrentSrc(fallbackUrl);
    } else {
      setHasError(true);
    }
  };

  return (
    <div className="flex flex-col h-full">
      {/* Title banner */}
      <div className="flex items-center justify-between px-3 py-2 bg-slate-900 text-slate-200 text-xs font-semibold rounded-t-2xl border-b border-slate-800">
        <span>{title}</span>
        {detections.length > 0 && (
          <span className="px-2 py-0.5 rounded-full text-[10px] font-bold bg-red-500/20 text-red-400 border border-red-500/30">
            {detections.length} vùng tổn thương
          </span>
        )}
      </div>

      {/* Main Medical Image View Container */}
      <div
        className="relative flex-1 min-h-[380px] max-h-[480px] bg-slate-950 rounded-b-2xl overflow-hidden flex items-center justify-center select-none cursor-grab active:cursor-grabbing border border-slate-900 shadow-inner"
        onMouseDown={handleMouseDown}
        onMouseMove={handleMouseMove}
        onMouseUp={handleMouseUp}
        onMouseLeave={handleMouseUp}
      >
        {isProcessing && (
          <div className="absolute inset-0 bg-slate-950/70 backdrop-blur-xs z-30 flex flex-col items-center justify-center gap-2 text-white">
            <RefreshCw className="w-8 h-8 text-blue-500 animate-spin" />
            <p className="text-xs font-semibold">Đang phân tích hình ảnh và vẽ kết quả...</p>
          </div>
        )}

        {hasError ? (
          <div className="flex flex-col items-center justify-center gap-2 text-slate-500 p-6 text-center">
            <ImageOff className="w-10 h-10 text-slate-600" />
            <p className="text-xs font-medium">Không thể tải file ảnh từ máy chủ</p>
          </div>
        ) : (
          <div
            className="relative transition-transform duration-75 flex items-center justify-center"
            style={{
              transform: `translate(${position.x}px, ${position.y}px) scale(${scale})`,
            }}
          >
            {/* Base X-Ray Image */}
            <img
              src={currentSrc}
              alt={title}
              className="max-h-[440px] w-auto object-contain rounded-md block pointer-events-none"
              onError={handleImageError}
            />

            {/* Render Bounding Box Overlays nếu bật chế độ client-side overlay */}
            {showDetections &&
              detections.map((det) => {
                const isHighlighted = highlightedDetectionId === det.detectionId;
                const { x, y, width, height } = det.boundingBox;

                // Normalized or pixel coordinates handling
                const left = x <= 1 ? `${x * 100}%` : `${x}px`;
                const top = y <= 1 ? `${y * 100}%` : `${y}px`;
                const boxWidth = width <= 1 ? `${width * 100}%` : `${width}px`;
                const boxHeight = height <= 1 ? `${height * 100}%` : `${height}px`;

                const confidencePercent = (det.confidenceScore > 1 ? det.confidenceScore : det.confidenceScore * 100).toFixed(1);

                return (
                  <div
                    key={det.detectionId}
                    onMouseEnter={() => onHoverDetection?.(det.detectionId)}
                    onMouseLeave={() => onHoverDetection?.(null)}
                    style={{
                      position: 'absolute',
                      left,
                      top,
                      width: boxWidth,
                      height: boxHeight,
                    }}
                    className={`pointer-events-auto transition-all duration-150 border-2 rounded-sm ${
                      isHighlighted
                        ? 'border-red-400 bg-red-500/30 ring-4 ring-red-400/50 shadow-lg shadow-red-500/50 z-20'
                        : 'border-red-500 bg-red-500/20 hover:bg-red-500/30 z-10'
                    }`}
                  >
                    {/* Floating Label with pill tag matching UI mockup: 'Fracture 92.7%' */}
                    <div className="absolute -top-7 left-0 whitespace-nowrap px-2.5 py-0.5 rounded-md bg-red-600 text-white text-[11px] font-bold shadow-md flex items-center gap-1">
                      <span className="capitalize">{det.classLabel || 'Fracture'}</span>
                      <span>{confidencePercent}%</span>
                    </div>
                  </div>
                );
              })}
          </div>
        )}

        {/* Floating View Controls (Bottom Right) */}
        <div className="absolute bottom-3 right-3 flex items-center gap-1.5 p-1 rounded-xl bg-slate-900/80 backdrop-blur-md border border-slate-700/60 shadow-lg text-slate-300">
          <button
            type="button"
            onClick={handleZoomIn}
            title="Phóng to"
            className="p-1.5 hover:text-white hover:bg-slate-800 rounded-lg transition-colors"
          >
            <ZoomIn className="w-4 h-4" />
          </button>
          <button
            type="button"
            onClick={handleZoomOut}
            title="Thu nhỏ"
            className="p-1.5 hover:text-white hover:bg-slate-800 rounded-lg transition-colors"
          >
            <ZoomOut className="w-4 h-4" />
          </button>
          <button
            type="button"
            onClick={handleReset}
            title="Đặt lại kích thước"
            className="p-1.5 hover:text-white hover:bg-slate-800 rounded-lg transition-colors"
          >
            <RotateCcw className="w-4 h-4" />
          </button>
          <span className="px-2 text-[10px] font-mono text-slate-400 border-l border-slate-700 select-none">
            {Math.round(scale * 100)}%
          </span>
        </div>
      </div>
    </div>
  );
};
