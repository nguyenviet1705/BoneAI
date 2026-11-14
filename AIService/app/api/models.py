from fastapi import APIRouter
from app.registry.model_registry import registry
from app.schemas.models import ModelInfo, ModelListResponse, ModelSyncResponse

router = APIRouter(prefix="/api/v1/models", tags=["Models"])

@router.get("", response_model=ModelListResponse)
async def list_models():
    """Retrieve all models registered in the inference service."""
    models = registry.list_models()
    return ModelListResponse(models=models, count=len(models))

@router.get("/{model_key}", response_model=ModelInfo)
async def get_model_details(model_key: str):
    """Retrieve metadata and readiness status for a specific model."""
    return registry.get_model(model_key)

@router.post("/discover", response_model=ModelSyncResponse)
async def discover_models():
    """Trigger dynamic scanning of model manifests and reload registry."""
    return registry.discover_models()
