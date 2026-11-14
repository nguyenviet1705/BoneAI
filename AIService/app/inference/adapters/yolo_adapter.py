import logging
import os
from typing import List, Optional
from PIL import Image
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.schemas.inference import BoundingBox, DetectionResult

logger = logging.getLogger(__name__)

class YoloInferenceAdapter(BaseInferenceAdapter):
    """
    Adapter for Ultralytics YOLO models (.pt format) for object detection.
    """

    def __init__(self, model_key: str, artifact_path: str, confidence_threshold: float = 0.25):
        self.model_key = model_key
        self.artifact_path = artifact_path
        self.confidence_threshold = confidence_threshold
        self._model = None
        self._is_loaded = False

    def load(self) -> None:
        if not os.path.exists(self.artifact_path):
            raise FileNotFoundError(f"YOLO model weights file not found at: {self.artifact_path}")

        try:
            from ultralytics import YOLO
            self._model = YOLO(self.artifact_path)
            self._is_loaded = True
            logger.info(f"Loaded YOLO model '{self.model_key}' from {self.artifact_path}")
        except Exception as e:
            self._is_loaded = False
            raise RuntimeError(f"Failed to load YOLO weights '{self.artifact_path}': {e}") from e

    def is_loaded(self) -> bool:
        return self._is_loaded and self._model is not None

    def unload(self) -> None:
        self._model = None
        self._is_loaded = False

    def predict(self, image: Image.Image, run_id: str) -> List[DetectionResult]:
        if not self.is_loaded():
            raise RuntimeError(f"Model '{self.model_key}' is not loaded.")

        results = self._model.predict(source=image, conf=self.confidence_threshold, verbose=False)
        detections: List[DetectionResult] = []

        for r in results:
            boxes = r.boxes
            if boxes is None:
                continue

            for box in boxes:
                xyxy = box.xyxy[0].tolist()  # [x1, y1, x2, y2]
                conf = float(box.conf[0])
                cls_id = int(box.cls[0])
                cls_name = r.names.get(cls_id, f"class_{cls_id}")

                x1, y1, x2, y2 = xyxy
                width = max(0.0, x2 - x1)
                height = max(0.0, y2 - y1)

                detections.append(DetectionResult(
                    className=cls_name,
                    confidence=round(conf, 4),
                    boundingBox=BoundingBox(
                        x=round(x1, 2),
                        y=round(y1, 2),
                        width=round(width, 2),
                        height=round(height, 2)
                    )
                ))

        return detections
