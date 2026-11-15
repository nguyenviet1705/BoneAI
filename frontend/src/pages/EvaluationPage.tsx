import React from 'react';
import { BarChart3, Clock } from 'lucide-react';

export const EvaluationPage: React.FC = () => {
  return (
    <div className="bg-white rounded-2xl p-8 border border-slate-200/80 shadow-sm text-center">
      <div className="w-16 h-16 bg-blue-50 text-blue-600 rounded-2xl flex items-center justify-center mx-auto mb-4">
        <BarChart3 className="w-8 h-8" />
      </div>
      <h2 className="text-xl font-bold text-slate-800">Đánh giá mô hình</h2>
      <p className="text-slate-500 text-sm mt-1 max-w-md mx-auto">
        Giao diện Đánh giá mô hình (FE5/Evaluation) sẽ hiển thị Precision, Recall, F1-Score, mAP@50, mAP@50-95 và Confusion Matrix.
      </p>
      <div className="mt-6 inline-flex items-center gap-2 px-3 py-1.5 bg-blue-50 text-blue-700 text-xs font-medium rounded-full">
        <Clock className="w-3.5 h-3.5" />
        Sắp ra mắt trong giai đoạn FE5
      </div>
    </div>
  );
};
