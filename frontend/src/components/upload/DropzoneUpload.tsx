import React, { useRef, useState } from 'react';
import { UploadCloud, ImagePlus, AlertCircle } from 'lucide-react';

interface Props {
  onImageSelected: (file: File) => void;
  onOpenGallery?: () => void;
  disabled?: boolean;
}

export const DropzoneUpload: React.FC<Props> = ({
  onImageSelected,
  onOpenGallery,
  disabled = false,
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isDragOver, setIsDragOver] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const validateAndProcessFile = (file: File) => {
    setErrorMessage(null);

    // Validate type
    const validTypes = ['image/jpeg', 'image/png', 'image/jpg'];
    if (!validTypes.includes(file.type.toLowerCase())) {
      setErrorMessage('Định dạng file không hợp lệ. Vui lòng chỉ tải lên file ảnh JPG hoặc PNG.');
      return;
    }

    // Validate size (max 10MB)
    const maxSize = 10 * 1024 * 1024;
    if (file.size > maxSize) {
      setErrorMessage('Dung lượng ảnh vượt quá giới hạn 10MB cho phép.');
      return;
    }

    onImageSelected(file);
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (!disabled) setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragOver(false);

    if (disabled) return;

    if (e.dataTransfer.files && e.dataTransfer.files.length > 0) {
      validateAndProcessFile(e.dataTransfer.files[0]);
    }
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      validateAndProcessFile(e.target.files[0]);
    }
  };

  return (
    <div className="w-full">
      <div
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
        className={`relative border-2 border-dashed rounded-2xl p-8 flex flex-col items-center justify-center text-center transition-all duration-200 cursor-pointer ${
          isDragOver
            ? 'border-blue-500 bg-blue-50/70 scale-[1.01]'
            : 'border-slate-200/90 bg-white hover:border-blue-300 hover:bg-slate-50/50'
        } ${disabled ? 'opacity-50 pointer-events-none' : ''}`}
        onClick={() => fileInputRef.current?.click()}
      >
        <input
          ref={fileInputRef}
          type="file"
          accept=".jpg,.jpeg,.png,image/jpeg,image/png"
          className="hidden"
          onChange={handleFileChange}
          disabled={disabled}
        />

        {/* Cloud Upload Icon */}
        <div className="w-16 h-16 rounded-2xl bg-blue-50 flex items-center justify-center text-blue-600 mb-4 shadow-xs">
          <UploadCloud className="w-9 h-9 stroke-[1.75]" />
        </div>

        {/* Text Guidelines */}
        <p className="text-base font-semibold text-slate-800">
          Kéo thả ảnh vào đây
        </p>
        <p className="text-xs text-slate-400 my-1 font-medium">hoặc</p>

        {/* Upload Button */}
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            fileInputRef.current?.click();
          }}
          className="mt-1 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-700 text-white font-medium text-sm transition-all duration-200 shadow-sm shadow-blue-500/25 active:scale-95"
        >
          Chọn ảnh từ máy tính
        </button>

        {/* Subtext info */}
        <p className="text-[12px] text-slate-400 mt-4 font-normal">
          Hỗ trợ: JPG, PNG • Tối đa: 10MB
        </p>
      </div>

      {/* Button to open existing library */}
      {onOpenGallery && (
        <div className="mt-3 text-center">
          <button
            type="button"
            onClick={onOpenGallery}
            className="inline-flex items-center gap-1.5 text-xs font-semibold text-blue-600 hover:text-blue-800 transition-colors py-1 px-2.5 rounded-lg hover:bg-blue-50/80"
          >
            <ImagePlus className="w-3.5 h-3.5" />
            <span>Chọn từ thư viện ảnh đã tải lên</span>
          </button>
        </div>
      )}

      {/* Error alert */}
      {errorMessage && (
        <div className="mt-3 p-3 rounded-xl bg-rose-50 border border-rose-200 text-rose-700 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 shrink-0" />
          <span>{errorMessage}</span>
        </div>
      )}
    </div>
  );
};
