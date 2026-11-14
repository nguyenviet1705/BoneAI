import logging
import os
from typing import Dict, List, Optional
from PIL import Image
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.schemas.inference import BoundingBox, DetectionResult

logger = logging.getLogger(__name__)

class TorchvisionFasterRCNNAdapter(BaseInferenceAdapter):
    """
    Adapter for PyTorch Torchvision detection models (e.g., Faster R-CNN .pt / .pth).
    """

    def __init__(
        self,
        model_key: str,
        artifact_path: str,
        confidence_threshold: float = 0.25,
        class_mapping: Optional[Dict[str, str]] = None
    ):
        self.model_key = model_key
        self.artifact_path = artifact_path
        self.confidence_threshold = confidence_threshold
        self.class_mapping = class_mapping or {"1": "fracture"}
        self._model = None
        self._is_loaded = False

    def load(self) -> None:
        if not os.path.exists(self.artifact_path):
            raise FileNotFoundError(f"PyTorch model weights not found at: {self.artifact_path}")

        try:
            import torch
            # Load model in evaluation mode
            device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
            self._model = torch.load(self.artifact_path, map_location=device)
            if hasattr(self._model, "eval"):
                self._model.eval()
            self._is_loaded = True
            logger.info(f"Loaded Torchvision model '{self.model_key}' from {self.artifact_path}")
        except Exception as e:
            self._is_loaded = False
            raise RuntimeError(f"Failed to load Torchvision weights '{self.artifact_path}': {e}") from e

    def is_loaded(self) -> bool:
        return self._is_loaded and self._model is not None

    def unload(self) -> None:
        self._model = None
        self._is_loaded = False

    def predict(self, image: Image.Image, run_id: str) -> List[DetectionResult]:
        if not self.is_loaded():
            raise RuntimeError(f"Model '{self.model_key}' is not loaded.")

        import torch
        import torchvision.transforms.functional as F

        device = next(self._model.parameters()).device if hasattr(self._model, "parameters") else torch.device("cpu")
        tensor_img = F.to_tensor(image).unsqueeze(0).to(device)

        with torch.no_grad():
            outputs = self._model(tensor_img)

        detections: List[DetectionResult] = []
        if isinstance(outputs, list) and len(outputs) > 0:
            pred = outputs[0]
            boxes = pred.get("boxes", [])
            labels = pred.get("labels", [])
            scores = pred.get("scores", [])

            for i in range(len(scores)):
                score = float(scores[i])
                if score < self.confidence_threshold:
                    continue

                box = boxes[i].tolist()
                label_id = str(int(labels[i]))
                class_name = self.class_mapping.get(label_id, f"class_{label_id}")

                x1, y1, x2, y2 = box
                width = max(0.0, x2 - x1)
                height = max(0.0, y2 - y1)

                detections.append(DetectionResult(
                    className=class_name,
                    confidence=round(score, 4),
                    boundingBox=BoundingBox(
                        x=round(x1, 2),
                        y=round(y1, 2),
                        width=round(width, 2),
                        height=round(height, 2)
                    )
                ))

        return detections
