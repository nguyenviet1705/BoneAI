import React from 'react';

interface Props {
  className?: string;
  size?: number;
}

export const HandXRayIcon: React.FC<Props> = ({ className = "w-9 h-9", size }) => {
  return (
    <div 
      className={`relative flex items-center justify-center rounded-xl bg-gradient-to-br from-blue-600 to-indigo-700 shadow-md shadow-blue-500/20 text-white ${className}`}
      style={size ? { width: size, height: size } : {}}
    >
      <svg 
        viewBox="0 0 24 24" 
        fill="none" 
        stroke="currentColor" 
        strokeWidth="2" 
        strokeLinecap="round" 
        strokeLinejoin="round" 
        className="w-5 h-5 text-blue-100 drop-shadow-[0_0_4px_rgba(147,197,253,0.8)]"
      >
        {/* Palm / Metacarpals */}
        <path d="M18 11V6a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v4" />
        <path d="M14 10V4a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v7" />
        <path d="M10 10.5V6a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2v8" />
        {/* Thumb */}
        <path d="M6 13a2 2 0 0 0-2 2v2a5 5 0 0 0 5 5h4a6 6 0 0 0 6-6V9a2 2 0 0 0-2-2v0a2 2 0 0 0-2 2" />
        {/* Wrist bone highlights */}
        <circle cx="12" cy="18" r="1" fill="currentColor" />
        <circle cx="9.5" cy="17.5" r="0.8" fill="currentColor" />
        <circle cx="14.5" cy="17.5" r="0.8" fill="currentColor" />
      </svg>
    </div>
  );
};
