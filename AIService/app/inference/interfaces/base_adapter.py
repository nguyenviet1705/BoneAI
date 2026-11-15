from abc import ABC, abstractmethod
from typing import Any, List
from app.schemas.inference import DetectionResult

class BaseInferenceAdapter(ABC):
    """
    Common inference interface for all model architectures (YOLO, Faster R-CNN, etc.).
    Decouples model execution from the API layer.
    """
    is_mock: bool = False
    mock_warning: str = ""

    @abstractmethod
    def load(self) -> None:
        """Load model weights/artifacts into memory."""
        pass

    @abstractmethod
    def predict(self, image: Any, run_id: str) -> List[DetectionResult]:
        """
        Run inference on the given input image (PIL Image or NumPy array).
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
