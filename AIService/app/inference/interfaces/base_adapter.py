from abc import ABC, abstractmethod
from typing import List
from PIL import Image
from app.schemas.inference import DetectionResult

class BaseInferenceAdapter(ABC):
    """
    Common inference interface for all model architectures (YOLO, Faster R-CNN, etc.).
    Decouples model execution from the API layer.
    """

    @abstractmethod
    def load(self) -> None:
        """Load model weights/artifacts into memory."""
        pass

    @abstractmethod
    def predict(self, image: Image.Image, run_id: str) -> List[DetectionResult]:
        """
        Run inference on the given PIL Image.
        Returns a list of DetectionResult objects with pixel bounding boxes.
        """
        pass

    @abstractmethod
    def is_loaded(self) -> bool:
        """Check whether the model is loaded and ready for prediction."""
        pass

    @abstractmethod
    def unload(self) -> None:
        """Unload model from memory."""
        pass
