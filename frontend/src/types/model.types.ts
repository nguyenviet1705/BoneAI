export interface AIModel {
  id: string;
  modelKey: string;
  name: string;
  version: string;
  framework: string;
  description?: string;
  isActive: boolean;
  isAvailable: boolean;
  mAP50?: number;
  fps?: number;
  dataset?: string;
  inputSize?: string;
  weightsName?: string;
}
