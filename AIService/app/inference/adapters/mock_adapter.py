from typing import List, Optional
from PIL import Image
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.schemas.inference import BoundingBox, DetectionResult

class MockInferenceAdapter(BaseInferenceAdapter):
    """
    Mock inference adapter for contract verification and development testing.
    Clearly marks and returns deterministic detection bounding boxes relative to original image size.
    Supports customization of class name, confidence, and box location per model instance.
    """
    is_mock: bool = True
    mock_warning: str = "Inference evaluated via mock adapter (synthetic detections)."

    def __init__(
        self,
        model_key: str = "mock-bone-fracture-v1",
        default_class: str = "fracture",
        confidence: float = 0.925,
        box_offset_x: float = 0.30,
        box_offset_y: float = 0.35,
        box_width_ratio: float = 0.25,
        box_height_ratio: float = 0.20
    ):
        self.model_key = model_key
        self.default_class = default_class
        self.confidence = confidence
        self.box_offset_x = box_offset_x
        self.box_offset_y = box_offset_y
        self.box_width_ratio = box_width_ratio
        self.box_height_ratio = box_height_ratio
        self._is_loaded = False

    def load(self) -> None:
        self._is_loaded = True

    def is_loaded(self) -> bool:
        return self._is_loaded

    def unload(self) -> None:
        self._is_loaded = False

    def predict(self, image: Image.Image, run_id: str) -> List[DetectionResult]:
        if not self._is_loaded:
            raise RuntimeError(f"Model adapter '{self.model_key}' must be loaded before running inference.")

        w, h = image.size

        # Compute safe bounding box located in wrist area (proportional to image size)
        x = round(w * self.box_offset_x, 2)
        y = round(h * self.box_offset_y, 2)
        box_w = min(round(w * self.box_width_ratio, 2), w - x)
        box_h = min(round(h * self.box_height_ratio, 2), h - y)

        detection = DetectionResult(
            className=self.default_class,
            confidence=round(self.confidence, 4),
            boundingBox=BoundingBox(
                x=x,
                y=y,
                width=box_w,
                height=box_h
            )
        )

        return [detection]
