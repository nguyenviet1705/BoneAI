from typing import List, Optional
from pydantic import BaseModel, Field

class BoundingBox(BaseModel):
    x: float = Field(..., description="Top-left X pixel coordinate relative to original image")
    y: float = Field(..., description="Top-left Y pixel coordinate relative to original image")
    width: float = Field(..., description="Bounding box width in pixels")
    height: float = Field(..., description="Bounding box height in pixels")

class DetectionResult(BaseModel):
    className: str = Field(..., description="Predicted class label, e.g. fracture")
    confidence: float = Field(..., ge=0.0, le=1.0, description="Detection confidence score between 0.0 and 1.0")
    boundingBox: BoundingBox

class InferenceResponse(BaseModel):
    analysisRunId: str
    modelKey: str
    modelVersion: str
    imageWidth: int
    imageHeight: int
    processingTimeMs: int
    detections: List[DetectionResult]
    isMockResult: bool = False
    warning: Optional[str] = None
