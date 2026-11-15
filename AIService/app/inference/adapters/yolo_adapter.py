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
    Directly aligns with standard Ultralytics YOLO inference pipeline:
      - YOLO(best.pt)
      - conf=CONF_THRESHOLD (default 0.25)
      - iou=IOU_THRESHOLD (default 0.45)
      - box extraction: class_id, confidence, xyxy[0]
      - class_name from model.names
    """

    def __init__(
        self,
        model_key: str,
        artifact_path: str,
        confidence_threshold: float = 0.25,
        iou_threshold: float = 0.45
    ):
        self.model_key = model_key
        self.artifact_path = artifact_path
        self.confidence_threshold = confidence_threshold
        self.iou_threshold = iou_threshold
        self._model = None
        self._is_loaded = False

    def load(self) -> None:
        if not os.path.exists(self.artifact_path):
            raise FileNotFoundError(f"YOLO model weights file not found at: {self.artifact_path}")

        try:
            from ultralytics import YOLO
            logger.info(f"Loading YOLO model from: {self.artifact_path} ({os.path.getsize(self.artifact_path) / (1024**2):.2f} MB)")
            self._model = YOLO(self.artifact_path)
            self._is_loaded = True
            logger.info(f"[OK] YOLO model '{self.model_key}' loaded successfully! Classes: {getattr(self._model, 'names', {})}")
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

        # Execute YOLO predict exactly as in test cell
        results = self._model.predict(
            source=image,
            conf=self.confidence_threshold,
            iou=self.iou_threshold,
            save=False,
            verbose=False
        )
        detections: List[DetectionResult] = []

        for r in results:
            if r.boxes is None or len(r.boxes) == 0:
                continue

            for box in r.boxes:
                # Extract classification and confidence
                class_id = int(box.cls[0].item() if hasattr(box.cls[0], "item") else box.cls[0])
                confidence = float(box.conf[0].item() if hasattr(box.conf[0], "item") else box.conf[0])

                # Bounding box xyxy coordinates
                xyxy = box.xyxy[0].tolist()
                x1, y1, x2, y2 = xyxy

                # Retrieve class name from model.names
                if hasattr(self._model, "names") and class_id in self._model.names:
                    class_name = self._model.names[class_id]
                elif hasattr(r, "names") and class_id in r.names:
                    class_name = r.names[class_id]
                else:
                    class_name = "fracture"

                width = max(0.0, x2 - x1)
                height = max(0.0, y2 - y1)

                detections.append(DetectionResult(
                    className=class_name,
                    confidence=round(confidence, 4),
                    boundingBox=BoundingBox(
                        x=round(x1, 2),
                        y=round(y1, 2),
                        width=round(width, 2),
                        height=round(height, 2)
                    )
                ))

        return detections
