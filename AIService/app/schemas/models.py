from enum import Enum
from typing import Any, Dict, List, Optional
from pydantic import BaseModel

class ModelStatus(str, Enum):
    REGISTERED = "Registered"
    AVAILABLE = "Available"
    LOADED = "Loaded"
    UNAVAILABLE = "Unavailable"
    LOADING = "Loading"
    FAILED = "Failed"

class ModelInfo(BaseModel):
    modelKey: str
    modelName: str
    version: str
    framework: str
    artifactPath: Optional[str] = None
    adapterId: Optional[str] = None
    isActive: bool = True
    status: ModelStatus = ModelStatus.REGISTERED
    supportedInputFormats: List[str] = ["image/jpeg", "image/png"]
    description: Optional[str] = None
    classMapping: Optional[Dict[str, str]] = None
    metadata: Dict[str, Any] = {}
    errorMessage: Optional[str] = None

class ModelListResponse(BaseModel):
    models: List[ModelInfo]
    count: int

class ModelSyncResponse(BaseModel):
    discoveredCount: int
    loadedCount: int
    failedCount: int
    models: List[ModelInfo]
