import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Bell, Activity, CheckCircle2, AlertCircle } from 'lucide-react';
import { modelService } from '../../api/modelService';

export const Header: React.FC = () => {
  const location = useLocation();
  const [aiReady, setAiReady] = useState<boolean | null>(null);

  useEffect(() => {
    // Check AI health on mount
    modelService.checkAIServiceHealth()
      .then((res) => setAiReady(res.readiness === 'Ready'))
      .catch(() => setAiReady(false));
  }, []);

  const getPageInfo = () => {
    switch (location.pathname) {
      case '/dashboard':
      case '/':
        return {
          title: 'Dashboard',
          subtitle: 'Tổng quan hệ thống phân tích gãy xương cổ tay trẻ em bằng AI',
          icon: Activity,
        };
      case '/analyze':
        return {
          title: 'Phân tích ảnh',
          subtitle: 'Tải ảnh và chọn mô hình để bắt đầu phân tích',
          icon: Activity,
        };
      case '/history':
        return {
          title: 'Lịch sử phân tích',
          subtitle: 'Xem lại các kết quả phân tích trước đây',
          icon: Activity,
        };
      case '/evaluation':
        return {
          title: 'Đánh giá mô hình',
          subtitle: 'Phân tích hiệu năng mô hình trên tập kiểm thử',
          icon: Activity,
        };
      case '/models':
        return {
          title: 'Quản lý mô hình',
          subtitle: 'Quản lý các mô hình AI và checkpoint',
          icon: Activity,
        };
      default:
        if (location.pathname.startsWith('/results')) {
          return {
            title: 'Kết quả phân tích',
            subtitle: 'Kết quả phát hiện gãy xương trên ảnh X-quang',
            icon: Activity,
          };
        }
        return {
          title: 'BoneAI System',
          subtitle: 'Hệ thống hỗ trợ chẩn đoán gãy xương',
          icon: Activity,
        };
    }
  };

  const pageInfo = getPageInfo();

  return (
    <header className="h-20 bg-white/80 backdrop-blur-md border-b border-slate-200/80 px-8 flex items-center justify-between sticky top-0 z-30 shadow-xs">
      {/* Title & Subtitle */}
      <div>
        <h2 className="text-xl font-bold text-slate-800 tracking-tight flex items-center gap-2">
          {pageInfo.title}
        </h2>
        <p className="text-xs text-slate-500 font-normal mt-0.5">
          {pageInfo.subtitle}
        </p>
      </div>

      {/* Right controls: Status indicator + Notifications + User Avatar */}
      <div className="flex items-center gap-4">
        {/* AI Health Badge */}
        {aiReady !== null && (
          <div className={`hidden md:flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium border ${
            aiReady 
              ? 'bg-emerald-50 text-emerald-700 border-emerald-200' 
              : 'bg-amber-50 text-amber-700 border-amber-200'
          }`}>
            {aiReady ? (
              <>
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                <span>AI Service Sẵn sàng</span>
              </>
            ) : (
              <>
                <AlertCircle className="w-3.5 h-3.5 text-amber-600" />
                <span>AI Service Ngoại tuyến</span>
              </>
            )}
          </div>
        )}

        {/* Notification Bell */}
        <button 
          title="Thông báo"
          className="relative w-10 h-10 rounded-xl bg-slate-50 hover:bg-slate-100 text-slate-600 hover:text-slate-900 flex items-center justify-center transition-colors border border-slate-200/70"
        >
          <Bell className="w-4 h-4" />
          <span className="absolute top-2.5 right-2.5 w-2 h-2 rounded-full bg-blue-600 ring-2 ring-white"></span>
        </button>

        {/* User Profile NV */}
        <div className="flex items-center gap-3 pl-2 border-l border-slate-200">
          <div 
            title="Bác sĩ / Chuyên viên Nguyễn Văn"
            className="w-10 h-10 rounded-full bg-gradient-to-tr from-blue-700 to-indigo-600 text-white font-semibold text-sm flex items-center justify-center shadow-sm shadow-blue-600/20 cursor-pointer select-none"
          >
            NV
          </div>
        </div>
      </div>
    </header>
  );
};
