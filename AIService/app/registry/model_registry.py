import logging
from typing import Dict, List, Optional
from app.core.errors import ModelNotFoundException, ModelNotReadyException
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.inference.adapters.mock_adapter import MockInferenceAdapter
from app.schemas.models import ModelInfo, ModelStatus

logger = logging.getLogger(__name__)

class ModelRegistry:
    def __init__(self):
        self._models: Dict[str, ModelInfo] = {}
        self._adapters: Dict[str, BaseInferenceAdapter] = {}
        self._initialize_default_registry()

    def _initialize_default_registry(self) -> None:
        """Register the development/mock model provider."""
        mock_model = ModelInfo(
            modelKey="mock-bone-fracture-v1",
            modelName="Mock Pediatric Wrist Fracture Detector",
            version="1.0.0",
            framework="MockProvider",
            artifactPath=None,
            isActive=True,
            status=ModelStatus.REGISTERED,
            supportedInputFormats=["image/jpeg", "image/png"],
            description="Development and contract-testing mock adapter for pediatric wrist fracture detection"
        )
        self.register_model(mock_model, MockInferenceAdapter("mock-bone-fracture-v1"))

    def register_model(self, info: ModelInfo, adapter: Optional[BaseInferenceAdapter] = None) -> None:
        self._models[info.modelKey] = info
        if adapter is not None:
            self._adapters[info.modelKey] = adapter
            try:
                adapter.load()
                info.status = ModelStatus.LOADED
                logger.info(f"Model '{info.modelKey}' successfully loaded into registry.")
            except Exception as e:
                info.status = ModelStatus.FAILED
                logger.error(f"Failed to load adapter for model '{info.modelKey}': {e}")
        else:
            info.status = ModelStatus.REGISTERED

    def get_model(self, model_key: str) -> ModelInfo:
        if model_key not in self._models:
            raise ModelNotFoundException(model_key)
        return self._models[model_key]

    def list_models(self) -> List[ModelInfo]:
        return list(self._models.values())

    def get_adapter(self, model_key: str) -> BaseInferenceAdapter:
        if model_key not in self._models:
            raise ModelNotFoundException(model_key)

        model = self._models[model_key]
        if not model.isActive:
            raise ModelNotReadyException(model_key, "Model is marked inactive.")

        adapter = self._adapters.get(model_key)
        if adapter is None or not adapter.is_loaded():
            raise ModelNotReadyException(model_key, f"Model status is '{model.status}'. Adapter not loaded.")

        return adapter

    def get_loaded_model_keys(self) -> List[str]:
        return [
            key for key, adapter in self._adapters.items()
            if adapter.is_loaded() and self._models.get(key, ModelInfo(modelKey="", modelName="", version="", framework="")).isActive
        ]

# Global singleton registry instance
registry = ModelRegistry()
