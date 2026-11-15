from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field, model_validator

class PreprocessingConfig(BaseModel):
    inputSize: List[int] = Field(default_factory=lambda: [640, 640])
    normalize: bool = True
    mean: Optional[List[float]] = None
    std: Optional[List[float]] = None

class PostprocessingConfig(BaseModel):
    confidenceThreshold: float = 0.20
    iouThreshold: float = 0.45
    maxDetections: Optional[int] = None

class InputRequirements(BaseModel):
    minWidth: int = 32
    minHeight: int = 32
    maxFileSizeMb: int = 30

class ModelManifest(BaseModel):
    modelKey: str = Field(..., min_length=1, max_length=100)
    modelName: str = Field(..., min_length=1, max_length=100)
    version: str = Field(..., min_length=1, max_length=50)
    framework: str = Field(..., min_length=1, max_length=50)
    adapterId: str = Field(..., min_length=1, max_length=50)
    artifactFile: Optional[str] = None
    isActive: bool = True
    description: Optional[str] = None
    supportedInputFormats: List[str] = Field(default_factory=lambda: ["image/jpeg", "image/png"])
    classMapping: Dict[str, str] = Field(default_factory=lambda: {"0": "fracture"})
    preprocessing: PreprocessingConfig = Field(default_factory=PreprocessingConfig)
    postprocessing: PostprocessingConfig = Field(default_factory=PostprocessingConfig)
    inputRequirements: InputRequirements = Field(default_factory=InputRequirements)
    inferenceSettings: Dict[str, Any] = Field(default_factory=dict)
    training: Optional[Dict[str, Any]] = None
    bestMetrics: Optional[Dict[str, Any]] = None

    @model_validator(mode="before")
    @classmethod
    def normalize_aliases(cls, data: Any) -> Any:
        if not isinstance(data, dict):
            return data

        # Map snake_case / alternate names to standard schema
        key = data.get("modelKey") or data.get("model_id") or data.get("id")
        name = data.get("modelName") or data.get("model_name") or data.get("name") or key
        ver = data.get("version") or data.get("model_version") or "1.0.0"
        fw = data.get("framework") or data.get("architecture") or "Ultralytics"
        weights = data.get("artifactFile") or data.get("weights") or data.get("artifact_path") or data.get("artifactPath")

        # Infer adapterId if not explicit
        adapter = data.get("adapterId") or data.get("adapter_id")
        if not adapter:
            arch_str = str(data.get("architecture", "")).lower()
            fw_str = str(fw).lower()
            if "yolo" in arch_str or "yolo" in fw_str or "ultralytics" in fw_str:
                adapter = "yolo"
            elif "torchvision" in arch_str or "faster" in arch_str:
                adapter = "torchvision"
            else:
                adapter = "mock"

        # Map classes dictionary
        classes = data.get("classMapping") or data.get("classes") or {"0": "fracture"}
        if isinstance(classes, dict):
            class_mapping = {str(k): str(v) for k, v in classes.items()}
        elif isinstance(classes, list):
            class_mapping = {str(i): str(name) for i, name in enumerate(classes)}
        else:
            class_mapping = {"0": "fracture"}

        # Normalize inference thresholds
        inference_block = data.get("inference") or {}
        postproc = data.get("postprocessing") or {}
        conf = postproc.get("confidenceThreshold") or inference_block.get("confidence_threshold") or 0.20
        iou = postproc.get("iouThreshold") or inference_block.get("iou_threshold") or 0.45
        img_size = inference_block.get("image_size") or 640
        max_dets = postproc.get("maxDetections") or inference_block.get("max_detections")

        normalized = dict(data)
        normalized["modelKey"] = str(key) if key else "unnamed_model"
        normalized["modelName"] = str(name) if name else normalized["modelKey"]
        normalized["version"] = str(ver)
        normalized["framework"] = str(fw)
        normalized["adapterId"] = str(adapter)
        normalized["artifactFile"] = str(weights) if weights else None
        normalized["classMapping"] = class_mapping

        if "postprocessing" not in normalized:
            normalized["postprocessing"] = {
                "confidenceThreshold": float(conf),
                "iouThreshold": float(iou),
                "maxDetections": max_dets
            }
        if "preprocessing" not in normalized:
            normalized["preprocessing"] = {
                "inputSize": [int(img_size), int(img_size)],
                "normalize": True
            }

        return normalized
