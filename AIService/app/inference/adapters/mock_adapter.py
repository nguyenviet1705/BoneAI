from typing import List
from PIL import Image
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.schemas.inference import BoundingBox, DetectionResult

class MockInferenceAdapter(BaseInferenceAdapter):
    """
    Mock inference adapter for contract verification and development testing.
    Clearly marks and returns deterministic detection bounding boxes relative to original image size.
    """

    def __init__(self, model_key: str = "mock-bone-fracture-v1"):
        self.model_key = model_key
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
        x = round(w * 0.30, 2)
        y = round(h * 0.35, 2)
        box_w = min(round(w * 0.25, 2), w - x)
        box_h = min(round(h * 0.20, 2), h - y)

        detection = DetectionResult(
            className="fracture",
            confidence=0.925,
            boundingBox=BoundingBox(
                x=x,
                y=y,
                width=box_w,
                height=box_h
            )
        )

        return [detection]
