import React from 'react';
import { CheckCircle2, Trash2, Loader2, Check } from 'lucide-react';
import type { SelectedImageInfo } from '../../types/image.types';

interface Props {
  image: SelectedImageInfo;
  onRemove: () => void;
  isUploading?: boolean;
}

export const SelectedImageCard: React.FC<Props> = ({
  image,
  onRemove,
  isUploading = false,
}) => {
  return (
    <div className="w-full">
      <h3 className="text-sm font-semibold text-slate-700 mb-2.5">
        Ảnh đã chọn
      </h3>

      <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex items-center justify-between gap-4 transition-all">
        {/* Left: Thumbnail with green tick badge */}
        <div className="relative shrink-0">
          <div className="w-20 h-20 rounded-xl overflow-hidden bg-slate-900 border border-slate-200/80 flex items-center justify-center shadow-inner">
            <img
              src={image.previewUrl}
              alt={image.fileName}
              className="w-full h-full object-cover"
            />
          </div>

          {/* Green checkmark pill */}
          {!isUploading && (
            <div className="absolute -top-1.5 -right-1.5 w-6 h-6 rounded-full bg-emerald-500 text-white flex items-center justify-center shadow-sm ring-2 ring-white">
              <CheckCircle2 className="w-4 h-4 stroke-[2.5]" />
            </div>
          )}

          {isUploading && (
            <div className="absolute inset-0 bg-black/40 rounded-xl flex items-center justify-center text-white">
              <Loader2 className="w-6 h-6 animate-spin text-white" />
            </div>
          )}
        </div>

        {/* Center: File metadata */}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-slate-800 truncate" title={image.fileName}>
            {image.fileName}
          </p>
          
          <div className="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-500 font-medium">
            <span>{image.fileSizeFormatted}</span>
            {image.width && image.height && (
              <>
                <span className="text-slate-300">•</span>
                <span>{image.width} × {image.height}</span>
              </>
            )}
          </div>

          {/* Upload Status */}
          <div className="mt-2">
            {isUploading ? (
              <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                <Loader2 className="w-3 h-3 animate-spin" />
                Đang lưu trữ ảnh...
              </span>
            ) : image.isUploaded ? (
              <span className="inline-flex items-center gap-1 text-[11px] font-medium text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md">
                <Check className="w-3.5 h-3.5 text-emerald-600" />
                Sẵn sàng phân tích
              </span>
            ) : null}
          </div>
        </div>

        {/* Right: Delete button */}
        <button
          type="button"
          onClick={onRemove}
          disabled={isUploading}
          className="shrink-0 flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold text-rose-600 bg-rose-50 hover:bg-rose-100 hover:text-rose-700 transition-colors border border-rose-100 active:scale-95 disabled:opacity-50"
        >
          <Trash2 className="w-3.5 h-3.5" />
          <span>Xóa ảnh</span>
        </button>
      </div>
    </div>
  );
};
