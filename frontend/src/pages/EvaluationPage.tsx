import React, { useState } from 'react';
import { 
  BarChart3, 
  Target, 
  RefreshCw, 
  Scale, 
  Award, 
  TrendingUp, 
  ChevronDown
} from 'lucide-react';
import { LossChart } from '../components/evaluation/LossChart';
import { MAPEpochChart } from '../components/evaluation/MAPEpochChart';
import { PrecisionRecallChart } from '../components/evaluation/PrecisionRecallChart';
import { F1CurveChart } from '../components/evaluation/F1CurveChart';
import { ConfusionMatrixHeatmap } from '../components/evaluation/ConfusionMatrixHeatmap';
import { TrainingInfoCard } from '../components/evaluation/TrainingInfoCard';
import type { ModelEvaluationData } from '../types/evaluation.types';

export const EvaluationPage: React.FC = () => {
  const [selectedSplit, setSelectedSplit] = useState<'test' | 'val' | 'train'>('test');
  const [selectedModelKey, setSelectedModelKey] = useState<string>('yolov8m');

  // Dữ liệu đánh giá hiệu năng mô hình y tế
  const evaluationData: Record<string, ModelEvaluationData> = {
    yolov8m: {
      modelId: 'm-yolov8m',
      modelName: 'YOLOv8m',
      datasetSplit: selectedSplit,
      metrics: {
        precision: 93.00,
        recall: 87.19,
        f1Score: 90.00,
        mAP50: 94.76,
        mAP50_95: 56.59,
      },
      trainingConfig: {
        modelName: 'YOLOv8m',
        dataset: 'GRAZPEDWRI-DX',
        imageSize: '640 × 640',
        batchSize: 16,
        maxEpochs: 100,
        patience: 20,
        optimizer: 'Auto',
        amp: true,
        gpu: 'Tesla T4',
      },
      confusionMatrix: {
        tp: 2045,
        fn: 197,
        fp: 134,
        tn: 1133,
      },
      lossHistory: [
        { epoch: 0, trainLoss: 2.35, valLoss: 1.95 },
        { epoch: 10, trainLoss: 1.62, valLoss: 1.48 },
        { epoch: 20, trainLoss: 1.25, valLoss: 1.15 },
        { epoch: 30, trainLoss: 1.02, valLoss: 0.98 },
        { epoch: 40, trainLoss: 0.88, valLoss: 0.86 },
        { epoch: 50, trainLoss: 0.76, valLoss: 0.78 },
        { epoch: 60, trainLoss: 0.67, valLoss: 0.71 },
        { epoch: 70, trainLoss: 0.58, valLoss: 0.66 },
        { epoch: 80, trainLoss: 0.51, valLoss: 0.62 },
        { epoch: 90, trainLoss: 0.45, valLoss: 0.58 },
        { epoch: 100, trainLoss: 0.39, valLoss: 0.55 },
      ],
      mapHistory: [
        { epoch: 0, map50: 0.12, map50_95: 0.05 },
        { epoch: 10, map50: 0.58, map50_95: 0.28 },
        { epoch: 20, map50: 0.78, map50_95: 0.42 },
        { epoch: 30, map50: 0.86, map50_95: 0.48 },
        { epoch: 40, map50: 0.89, map50_95: 0.51 },
        { epoch: 50, map50: 0.91, map50_95: 0.53 },
        { epoch: 60, map50: 0.92, map50_95: 0.54 },
        { epoch: 70, map50: 0.93, map50_95: 0.55 },
        { epoch: 80, map50: 0.94, map50_95: 0.56 },
        { epoch: 90, map50: 0.945, map50_95: 0.562 },
        { epoch: 100, map50: 0.9476, map50_95: 0.5659 },
      ],
      prCurve: [
        { recall: 0.0, precision: 0.98 },
        { recall: 0.2, precision: 0.97 },
        { recall: 0.4, precision: 0.96 },
        { recall: 0.6, precision: 0.95 },
        { recall: 0.8, precision: 0.93 },
        { recall: 0.87, precision: 0.90 },
        { recall: 0.92, precision: 0.75 },
        { recall: 0.96, precision: 0.45 },
        { recall: 1.0, precision: 0.05 },
      ],
      f1Curve: [
        { confidence: 0.0, f1: 0.42 },
        { confidence: 0.1, f1: 0.65 },
        { confidence: 0.2, f1: 0.78 },
        { confidence: 0.3, f1: 0.85 },
        { confidence: 0.4, f1: 0.89 },
        { confidence: 0.5, f1: 0.90 },
        { confidence: 0.6, f1: 0.88 },
        { confidence: 0.7, f1: 0.84 },
        { confidence: 0.8, f1: 0.76 },
        { confidence: 0.9, f1: 0.58 },
        { confidence: 1.0, f1: 0.15 },
      ],
    },
    yolov11: {
      modelId: 'm-yolov11',
      modelName: 'YOLOv11',
      datasetSplit: selectedSplit,
      metrics: {
        precision: 62.40,
        recall: 51.10,
        f1Score: 56.20,
        mAP50: 55.12,
        mAP50_95: 29.80,
      },
      trainingConfig: {
        modelName: 'YOLOv11',
        dataset: 'GRAZPEDWRI-DX',
        imageSize: '640 × 640',
        batchSize: 16,
        maxEpochs: 50,
        patience: 15,
        optimizer: 'AdamW',
        amp: true,
        gpu: 'Tesla T4',
      },
      confusionMatrix: {
        tp: 1240,
        fn: 980,
        fp: 750,
        tn: 1100,
      },
      lossHistory: [
        { epoch: 0, trainLoss: 2.8, valLoss: 2.6 },
        { epoch: 25, trainLoss: 1.6, valLoss: 1.8 },
        { epoch: 50, trainLoss: 1.1, valLoss: 1.4 },
        { epoch: 75, trainLoss: 0.9, valLoss: 1.3 },
        { epoch: 100, trainLoss: 0.8, valLoss: 1.2 },
      ],
      mapHistory: [
        { epoch: 0, map50: 0.05, map50_95: 0.02 },
        { epoch: 25, map50: 0.38, map50_95: 0.18 },
        { epoch: 50, map50: 0.52, map50_95: 0.28 },
        { epoch: 75, map50: 0.54, map50_95: 0.29 },
        { epoch: 100, map50: 0.5512, map50_95: 0.298 },
      ],
      prCurve: [
        { recall: 0.0, precision: 0.80 },
        { recall: 0.3, precision: 0.72 },
        { recall: 0.5, precision: 0.60 },
        { recall: 0.8, precision: 0.35 },
        { recall: 1.0, precision: 0.05 },
      ],
      f1Curve: [
        { confidence: 0.0, f1: 0.30 },
        { confidence: 0.3, f1: 0.52 },
        { confidence: 0.5, f1: 0.56 },
        { confidence: 0.7, f1: 0.48 },
        { confidence: 1.0, f1: 0.10 },
      ],
    },
    yolov26: {
      modelId: 'm-yolov26',
      modelName: 'YOLOv26',
      datasetSplit: selectedSplit,
      metrics: {
        precision: 91.80,
        recall: 86.40,
        f1Score: 89.02,
        mAP50: 93.48,
        mAP50_95: 54.80,
      },
      trainingConfig: {
        modelName: 'YOLOv26',
        dataset: 'GRAZPEDWRI-DX',
        imageSize: '640 × 640',
        batchSize: 16,
        maxEpochs: 100,
        patience: 20,
        optimizer: 'Auto',
        amp: true,
        gpu: 'Tesla T4',
      },
      confusionMatrix: {
        tp: 1980,
        fn: 262,
        fp: 180,
        tn: 1087,
      },
      lossHistory: [
        { epoch: 0, trainLoss: 2.4, valLoss: 2.0 },
        { epoch: 25, trainLoss: 1.3, valLoss: 1.2 },
        { epoch: 50, trainLoss: 0.8, valLoss: 0.85 },
        { epoch: 75, trainLoss: 0.6, valLoss: 0.72 },
        { epoch: 100, trainLoss: 0.42, valLoss: 0.61 },
      ],
      mapHistory: [
        { epoch: 0, map50: 0.10, map50_95: 0.04 },
        { epoch: 25, map50: 0.75, map50_95: 0.40 },
        { epoch: 50, map50: 0.88, map50_95: 0.50 },
        { epoch: 75, map50: 0.92, map50_95: 0.53 },
        { epoch: 100, map50: 0.9348, map50_95: 0.548 },
      ],
      prCurve: [
        { recall: 0.0, precision: 0.96 },
        { recall: 0.4, precision: 0.94 },
        { recall: 0.7, precision: 0.91 },
        { recall: 0.86, precision: 0.88 },
        { recall: 1.0, precision: 0.05 },
      ],
      f1Curve: [
        { confidence: 0.0, f1: 0.40 },
        { confidence: 0.3, f1: 0.82 },
        { confidence: 0.5, f1: 0.89 },
        { confidence: 0.7, f1: 0.82 },
        { confidence: 1.0, f1: 0.12 },
      ],
    },
    faster_rcnn: {
      modelId: 'm-rcnn',
      modelName: 'Faster R-CNN',
      datasetSplit: selectedSplit,
      metrics: {
        precision: 89.50,
        recall: 85.20,
        f1Score: 87.29,
        mAP50: 91.20,
        mAP50_95: 52.10,
      },
      trainingConfig: {
        modelName: 'Faster R-CNN',
        dataset: 'GRAZPEDWRI-DX',
        imageSize: '800 × 800',
        batchSize: 8,
        maxEpochs: 60,
        patience: 15,
        optimizer: 'SGD with Momentum',
        amp: true,
        gpu: 'Tesla T4',
      },
      confusionMatrix: {
        tp: 1910,
        fn: 332,
        fp: 224,
        tn: 1043,
      },
      lossHistory: [
        { epoch: 0, trainLoss: 2.6, valLoss: 2.2 },
        { epoch: 25, trainLoss: 1.4, valLoss: 1.35 },
        { epoch: 50, trainLoss: 0.9, valLoss: 0.95 },
        { epoch: 75, trainLoss: 0.65, valLoss: 0.80 },
        { epoch: 100, trainLoss: 0.48, valLoss: 0.72 },
      ],
      mapHistory: [
        { epoch: 0, map50: 0.08, map50_95: 0.03 },
        { epoch: 25, map50: 0.70, map50_95: 0.36 },
        { epoch: 50, map50: 0.85, map50_95: 0.46 },
        { epoch: 75, map50: 0.89, map50_95: 0.50 },
        { epoch: 100, map50: 0.912, map50_95: 0.521 },
      ],
      prCurve: [
        { recall: 0.0, precision: 0.94 },
        { recall: 0.4, precision: 0.91 },
        { recall: 0.7, precision: 0.88 },
        { recall: 0.85, precision: 0.85 },
        { recall: 1.0, precision: 0.05 },
      ],
      f1Curve: [
        { confidence: 0.0, f1: 0.38 },
        { confidence: 0.3, f1: 0.80 },
        { confidence: 0.5, f1: 0.87 },
        { confidence: 0.7, f1: 0.79 },
        { confidence: 1.0, f1: 0.12 },
      ],
    },
  };

  const currentData = evaluationData[selectedModelKey] || evaluationData.yolov8m;

  return (
    <div className="space-y-6">
      {/* Header & Filter Controls */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-1">
        <div>
          <h1 className="text-xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
            <BarChart3 className="w-5 h-5 text-blue-600" />
            <span>Đánh giá mô hình</span>
          </h1>
          <p className="text-xs text-slate-500 mt-1 font-medium">
            Phân tích hiệu năng mô hình <span className="font-bold text-slate-700">{currentData.modelName}</span> trên tập kiểm thử
          </p>
        </div>

        {/* Controls: Model Switcher & Split Dropdown */}
        <div className="flex items-center gap-3">
          {/* Model Switcher */}
          <div className="relative">
            <select
              value={selectedModelKey}
              onChange={(e) => setSelectedModelKey(e.target.value)}
              className="appearance-none bg-white border border-slate-200 text-xs font-bold text-slate-700 py-2 pl-3 pr-8 rounded-xl shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
            >
              <option value="yolov8m">YOLOv8m (Baseline)</option>
              <option value="yolov11">YOLOv11</option>
              <option value="yolov26">YOLOv26</option>
              <option value="faster_rcnn">Faster R-CNN</option>
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>

          {/* Dataset Split Selector */}
          <div className="relative">
            <select
              value={selectedSplit}
              onChange={(e) => setSelectedSplit(e.target.value as any)}
              className="appearance-none bg-white border border-slate-200 text-xs font-bold text-blue-700 py-2 pl-3 pr-8 rounded-xl shadow-xs hover:border-blue-300 focus:outline-none focus:ring-2 focus:ring-blue-500/20 cursor-pointer"
            >
              <option value="test">Test Set</option>
              <option value="val">Validation Set</option>
              <option value="train">Train Set</option>
            </select>
            <ChevronDown className="w-3.5 h-3.5 text-slate-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>
        </div>
      </div>

      {/* Row 1: 5 Top Metric Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
        
        {/* Metric 1: Precision */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col items-center justify-center text-center">
          <div className="w-8 h-8 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center mb-1">
            <Target className="w-4 h-4 stroke-[2.5]" />
          </div>
          <span className="text-xl font-black text-slate-800 tracking-tight">
            {currentData.metrics.precision.toFixed(2)}%
          </span>
          <span className="text-[11px] font-semibold text-slate-400 mt-0.5">
            Precision
          </span>
        </div>

        {/* Metric 2: Recall */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col items-center justify-center text-center">
          <div className="w-8 h-8 rounded-full bg-blue-50 text-blue-600 flex items-center justify-center mb-1">
            <RefreshCw className="w-4 h-4 stroke-[2.5]" />
          </div>
          <span className="text-xl font-black text-slate-800 tracking-tight">
            {currentData.metrics.recall.toFixed(2)}%
          </span>
          <span className="text-[11px] font-semibold text-slate-400 mt-0.5">
            Recall
          </span>
        </div>

        {/* Metric 3: F1-Score */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col items-center justify-center text-center">
          <div className="w-8 h-8 rounded-full bg-blue-50 text-blue-600 flex items-center justify-center mb-1">
            <Scale className="w-4 h-4 stroke-[2.5]" />
          </div>
          <span className="text-xl font-black text-slate-800 tracking-tight">
            {currentData.metrics.f1Score.toFixed(2)}%
          </span>
          <span className="text-[11px] font-semibold text-slate-400 mt-0.5">
            F1-Score
          </span>
        </div>

        {/* Metric 4: mAP@50 */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col items-center justify-center text-center">
          <div className="w-8 h-8 rounded-full bg-blue-50 text-blue-600 flex items-center justify-center mb-1">
            <Award className="w-4 h-4 stroke-[2.5]" />
          </div>
          <span className="text-xl font-black text-blue-600 tracking-tight">
            {currentData.metrics.mAP50.toFixed(2)}%
          </span>
          <span className="text-[11px] font-semibold text-slate-400 mt-0.5">
            mAP@50
          </span>
        </div>

        {/* Metric 5: mAP@50-95 */}
        <div className="bg-white rounded-2xl p-4 border border-slate-200/80 shadow-xs flex flex-col items-center justify-center text-center col-span-2 sm:col-span-1">
          <div className="w-8 h-8 rounded-full bg-teal-50 text-teal-600 flex items-center justify-center mb-1">
            <TrendingUp className="w-4 h-4 stroke-[2.5]" />
          </div>
          <span className="text-xl font-black text-slate-800 tracking-tight">
            {currentData.metrics.mAP50_95.toFixed(2)}%
          </span>
          <span className="text-[11px] font-semibold text-slate-400 mt-0.5">
            mAP@50-95
          </span>
        </div>

      </div>

      {/* Row 2: 4 Panels (Loss, mAP Epoch, PR Curve, Training Info) */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
        <LossChart data={currentData.lossHistory} />
        <MAPEpochChart data={currentData.mapHistory} />
        <PrecisionRecallChart data={currentData.prCurve} />
        <TrainingInfoCard config={currentData.trainingConfig} />
      </div>

      {/* Row 3: Confusion Matrix & F1 Curve */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-stretch">
        <div className="lg:col-span-5">
          <ConfusionMatrixHeatmap data={currentData.confusionMatrix} />
        </div>
        <div className="lg:col-span-7">
          <F1CurveChart data={currentData.f1Curve} />
        </div>
      </div>
    </div>
  );
};
