import os
from pydantic import BaseModel

class Settings(BaseModel):
    app_name: str = "BoneAI Inference Service"
    app_version: str = "1.0.0"
    host: str = os.getenv("AI_SERVICE_HOST", "0.0.0.0")
    port: int = int(os.getenv("AI_SERVICE_PORT", "8000"))
    environment: str = os.getenv("ENVIRONMENT", "development")
    log_level: str = os.getenv("LOG_LEVEL", "info")

settings = Settings()
