import React from 'react';
import { History, Clock } from 'lucide-react';

export const HistoryPage: React.FC = () => {
  return (
    <div className="bg-white rounded-2xl p-8 border border-slate-200/80 shadow-sm text-center">
      <div className="w-16 h-16 bg-blue-50 text-blue-600 rounded-2xl flex items-center justify-center mx-auto mb-4">
        <History className="w-8 h-8" />
      </div>
      <h2 className="text-xl font-bold text-slate-800">Lịch sử phân tích</h2>
      <p className="text-slate-500 text-sm mt-1 max-w-md mx-auto">
        Giao diện Lịch sử phân tích và thống kê (FE6) sẽ hiển thị bảng các lần chẩn đoán, bộ lọc model, thời gian và kết quả chi tiết.
      </p>
      <div className="mt-6 inline-flex items-center gap-2 px-3 py-1.5 bg-blue-50 text-blue-700 text-xs font-medium rounded-full">
        <Clock className="w-3.5 h-3.5" />
        Sắp ra mắt trong giai đoạn FE6
      </div>
    </div>
  );
};
