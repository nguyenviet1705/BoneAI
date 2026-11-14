from fastapi import APIRouter
from app.registry.model_registry import registry
from app.schemas.health import HealthResponse, ReadinessResponse

router = APIRouter(tags=["Health"])

@router.get("/health", response_model=HealthResponse)
async def liveness_check():
    """Liveness probe: returns 200 if the Python service process is running."""
    return HealthResponse()

@router.get("/ready", response_model=ReadinessResponse)
async def readiness_check():
    """Readiness probe: returns 200 if at least one AI model is loaded and ready."""
    loaded = registry.get_loaded_model_keys()
    is_ready = len(loaded) > 0
    return ReadinessResponse(
        status="ready" if is_ready else "not_ready",
        isReady=is_ready,
        loadedModels=loaded,
        totalRegistered=len(registry.list_models())
    )
