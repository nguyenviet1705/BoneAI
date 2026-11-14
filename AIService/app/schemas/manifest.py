from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field

class PreprocessingConfig(BaseModel):
    inputSize: List[int] = Field(default_factory=lambda: [640, 640])
    normalize: bool = True
    mean: Optional[List[float]] = None
    std: Optional[List[float]] = None

class PostprocessingConfig(BaseModel):
    confidenceThreshold: float = 0.25
    iouThreshold: float = 0.45
    maxDetections: int = 100

class InputRequirements(BaseModel):
    minWidth: int = 32
    minHeight: int = 32
    maxFileSizeMb: int = 20

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
