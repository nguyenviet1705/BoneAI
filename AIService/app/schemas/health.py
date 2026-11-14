from typing import List
from pydantic import BaseModel

class HealthResponse(BaseModel):
    status: str = "healthy"
    app: str = "BoneAI Inference Service"
    version: str = "1.0.0"

class ReadinessResponse(BaseModel):
    status: str
    isReady: bool
    loadedModels: List[str]
    totalRegistered: int
