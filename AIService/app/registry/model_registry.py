import json
import logging
import os
from typing import Dict, List, Optional
import yaml
from pydantic import ValidationError

from app.core.config import settings
from app.core.errors import ModelNotFoundException, ModelNotReadyException
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.inference.adapters.mock_adapter import MockInferenceAdapter
from app.inference.adapters.yolo_adapter import YoloInferenceAdapter
from app.inference.adapters.torchvision_adapter import TorchvisionFasterRCNNAdapter
from app.schemas.manifest import ModelManifest
from app.schemas.models import ModelInfo, ModelStatus, ModelSyncResponse

logger = logging.getLogger(__name__)

class ModelRegistry:
    def __init__(self, models_dir: Optional[str] = None):
        self._models_dir = models_dir or settings.models_dir
        self._models: Dict[str, ModelInfo] = {}
        self._adapters: Dict[str, BaseInferenceAdapter] = {}
        self._initialize_default_registry()
        self.discover_models()

    def _initialize_default_registry(self) -> None:
        """Register the built-in development/mock model provider."""
        mock_model = ModelInfo(
            modelKey="mock-bone-fracture-v1",
            modelName="Mock Pediatric Wrist Fracture Detector",
            version="1.0.0",
            framework="MockProvider",
            artifactPath=None,
            adapterId="mock",
            isActive=True,
            status=ModelStatus.REGISTERED,
            supportedInputFormats=["image/jpeg", "image/png"],
            description="Development and contract-testing mock adapter for pediatric wrist fracture detection",
            classMapping={"0": "fracture"}
        )
        self.register_model(mock_model, MockInferenceAdapter("mock-bone-fracture-v1"))

    def register_model(self, info: ModelInfo, adapter: Optional[BaseInferenceAdapter] = None) -> None:
        """Register a model and attempt adapter initialization without crashing on errors."""
        self._models[info.modelKey] = info
        if adapter is not None:
            self._adapters[info.modelKey] = adapter
            try:
                adapter.load()
                info.status = ModelStatus.LOADED
                info.errorMessage = None
                logger.info(f"Model '{info.modelKey}' (v{info.version}) successfully loaded into registry.")
            except Exception as e:
                info.status = ModelStatus.FAILED
                info.errorMessage = f"Adapter load failed: {str(e)}"
                logger.error(f"Failed to load adapter for model '{info.modelKey}': {e}")
        else:
            if info.status != ModelStatus.UNAVAILABLE:
                info.status = ModelStatus.REGISTERED

    def discover_models(self, directory: Optional[str] = None) -> ModelSyncResponse:
        """
        Scan directory for model manifests (manifest.yaml or manifest.json),
        validate metadata, and independently load compatible model adapters.
        """
        target_dir = directory or self._models_dir
        discovered_count = 0
        loaded_count = 0
        failed_count = 0

        if not os.path.isdir(target_dir):
            logger.info(f"Models directory '{target_dir}' does not exist. Skipping filesystem discovery.")
            return ModelSyncResponse(
                discoveredCount=0,
                loadedCount=len(self.get_loaded_model_keys()),
                failedCount=0,
                models=self.list_models()
            )

        logger.info(f"Scanning models directory: {target_dir}")
        for entry in os.listdir(target_dir):
            sub_path = os.path.join(target_dir, entry)
            if not os.path.isdir(sub_path):
                continue

            manifest_path = None
            for candidate in ("manifest.yaml", "manifest.yml", "manifest.json"):
                cand_path = os.path.join(sub_path, candidate)
                if os.path.isfile(cand_path):
                    manifest_path = cand_path
                    break

            if not manifest_path:
                continue

            discovered_count += 1
            try:
                manifest = self._parse_manifest(manifest_path)
            except Exception as ex:
                failed_count += 1
                logger.warning(f"Failed to parse manifest at '{manifest_path}': {ex}")
                # Track as failed model info
                failed_info = ModelInfo(
                    modelKey=entry,
                    modelName=entry,
                    version="unknown",
                    framework="unknown",
                    isActive=False,
                    status=ModelStatus.FAILED,
                    errorMessage=f"Invalid manifest: {str(ex)}"
                )
                self._models[entry] = failed_info
                continue

            # Check artifact existence if specified
            artifact_full_path = None
            if manifest.artifactFile:
                artifact_full_path = os.path.join(sub_path, manifest.artifactFile)

            model_info = ModelInfo(
                modelKey=manifest.modelKey,
                modelName=manifest.modelName,
                version=manifest.version,
                framework=manifest.framework,
                artifactPath=artifact_full_path,
                adapterId=manifest.adapterId,
                isActive=manifest.isActive,
                status=ModelStatus.REGISTERED,
                supportedInputFormats=manifest.supportedInputFormats,
                description=manifest.description,
                classMapping=manifest.classMapping,
                metadata={
                    "preprocessing": manifest.preprocessing.model_dump(),
                    "postprocessing": manifest.postprocessing.model_dump(),
                    "inputRequirements": manifest.inputRequirements.model_dump(),
                    "inferenceSettings": manifest.inferenceSettings
                }
            )

            # Check if artifact exists
            if manifest.artifactFile and not os.path.isfile(artifact_full_path):
                model_info.status = ModelStatus.UNAVAILABLE
                model_info.errorMessage = f"Artifact file '{manifest.artifactFile}' not found in '{sub_path}'"
                self._models[manifest.modelKey] = model_info
                failed_count += 1
                logger.warning(f"Model '{manifest.modelKey}' marked UNAVAILABLE: artifact missing.")
                continue

            # Resolve adapter
            adapter = self._resolve_adapter(manifest, sub_path, artifact_full_path)
            if adapter is None:
                model_info.status = ModelStatus.FAILED
                model_info.errorMessage = f"No supported adapter found for adapterId '{manifest.adapterId}'"
                self._models[manifest.modelKey] = model_info
                failed_count += 1
                continue

            self.register_model(model_info, adapter)
            if model_info.status == ModelStatus.LOADED:
                loaded_count += 1
            else:
                failed_count += 1

        return ModelSyncResponse(
            discoveredCount=discovered_count,
            loadedCount=len(self.get_loaded_model_keys()),
            failedCount=failed_count,
            models=self.list_models()
        )

    def _parse_manifest(self, file_path: str) -> ModelManifest:
        with open(file_path, "r", encoding="utf-8") as f:
            if file_path.endswith(".json"):
                data = json.load(f)
            else:
                data = yaml.safe_load(f)
        return ModelManifest(**data)

    def _resolve_adapter(
        self,
        manifest: ModelManifest,
        sub_path: str,
        artifact_path: Optional[str]
    ) -> Optional[BaseInferenceAdapter]:
        adapter_id = manifest.adapterId.lower()
        if adapter_id == "mock":
            # Extract mock parameters from inference settings if available
            default_cls = manifest.classMapping.get("0", "fracture")
            conf = manifest.postprocessing.confidenceThreshold if manifest.postprocessing.confidenceThreshold > 0 else 0.90
            offset_x = float(manifest.inferenceSettings.get("boxOffsetX", 0.30))
            offset_y = float(manifest.inferenceSettings.get("boxOffsetY", 0.35))
            width_ratio = float(manifest.inferenceSettings.get("boxWidthRatio", 0.25))
            height_ratio = float(manifest.inferenceSettings.get("boxHeightRatio", 0.20))
            return MockInferenceAdapter(
                model_key=manifest.modelKey,
                default_class=default_cls,
                confidence=conf,
                box_offset_x=offset_x,
                box_offset_y=offset_y,
                box_width_ratio=width_ratio,
                box_height_ratio=height_ratio
            )
        elif adapter_id == "yolo" and artifact_path:
            return YoloInferenceAdapter(
                model_key=manifest.modelKey,
                artifact_path=artifact_path,
                confidence_threshold=manifest.postprocessing.confidenceThreshold
            )
        elif adapter_id in ("torchvision", "faster_rcnn") and artifact_path:
            return TorchvisionFasterRCNNAdapter(
                model_key=manifest.modelKey,
                artifact_path=artifact_path,
                confidence_threshold=manifest.postprocessing.confidenceThreshold,
                class_mapping=manifest.classMapping
            )
        return None

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

        if model.status == ModelStatus.UNAVAILABLE:
            raise ModelNotReadyException(model_key, f"Model is unavailable: {model.errorMessage}")

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
