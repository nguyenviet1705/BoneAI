export interface ModelMetrics {
  precision: number;
  recall: number;
  f1Score: number;
  mAP50: number;
  mAP50_95: number;
}

export interface TrainingConfig {
  modelName: string;
  dataset: string;
  imageSize: string;
  batchSize: number;
  maxEpochs: number;
  patience: number;
  optimizer: string;
  amp: boolean;
  gpu: string;
}

export interface ConfusionMatrixData {
  tp: number;
  fn: number;
  fp: number;
  tn: number;
}

export interface ModelEvaluationData {
  modelId: string;
  modelName: string;
  datasetSplit: 'test' | 'val' | 'train';
  metrics: ModelMetrics;
  trainingConfig: TrainingConfig;
  confusionMatrix: ConfusionMatrixData;
  lossHistory: Array<{ epoch: number; trainLoss: number; valLoss: number }>;
  mapHistory: Array<{ epoch: number; map50: number; map50_95: number }>;
  prCurve: Array<{ recall: number; precision: number }>;
  f1Curve: Array<{ confidence: number; f1: number }>;
}
