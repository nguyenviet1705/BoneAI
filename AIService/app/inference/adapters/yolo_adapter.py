import logging
import os
from typing import Any, List, Optional, Union
import cv2
import numpy as np
from PIL import Image
from app.inference.interfaces.base_adapter import BaseInferenceAdapter
from app.schemas.inference import BoundingBox, DetectionResult

logger = logging.getLogger(__name__)

def to_cv_bgr(image: Union[Image.Image, np.ndarray]) -> np.ndarray:
    """
    Normalizes any image input (PIL Image or NumPy array) into a standard 3-channel 8-bit BGR array (uint8 [0, 255]).
    Crucially handles medical X-rays with high dynamic bit-depth (16-bit integer 'I;16'):
      - Downscales 16-bit integer depths to 8-bit via bit-shift (val >> 8), matching cv2.imread behavior.
      - Replicates single-channel grayscale (L, 2D) into 3 channels.
      - Strips alpha channels (RGBA / BGRA) cleanly.
    """
    if isinstance(image, Image.Image):
        mode = image.mode
        if mode in ("I;16", "I;16B", "I;16L", "I;16N", "I"):
            arr = np.array(image)
            if arr.dtype == np.uint16 or arr.max() > 255:
                arr = (arr >> 8).astype(np.uint8)
            else:
                arr = arr.astype(np.uint8)
            return cv2.cvtColor(arr, cv2.COLOR_GRAY2BGR)
        elif mode == "RGBA":
            arr = np.array(image)
            return cv2.cvtColor(arr, cv2.COLOR_RGBA2BGR)
        elif mode == "L":
            arr = np.array(image)
            return cv2.cvtColor(arr, cv2.COLOR_GRAY2BGR)
        else:
            arr = np.array(image.convert("RGB"))
            return cv2.cvtColor(arr, cv2.COLOR_RGB2BGR)

    elif isinstance(image, np.ndarray):
        arr = image
        # Handle 16-bit array
        if arr.dtype == np.uint16 or arr.max() > 255:
            arr = (arr >> 8).astype(np.uint8)
        else:
            arr = arr.astype(np.uint8)

        # Handle grayscale 2D
        if arr.ndim == 2:
            return cv2.cvtColor(arr, cv2.COLOR_GRAY2BGR)
        # Handle 3D with 1 channel
        if arr.ndim == 3 and arr.shape[2] == 1:
            return cv2.cvtColor(arr, cv2.COLOR_GRAY2BGR)
        # Handle 4 channels (BGRA)
        if arr.ndim == 3 and arr.shape[2] == 4:
            return cv2.cvtColor(arr, cv2.COLOR_BGRA2BGR)
        return arr

    raise ValueError(f"Unsupported image type: {type(image)}")

class YoloInferenceAdapter(BaseInferenceAdapter):
    """
    Adapter for Ultralytics YOLO models (.pt and .onnx formats) for object detection.
    Directly aligns 100% with standard Ultralytics YOLO inference pipeline:
      - imgsz=640 (preserving aspect ratio via Letterbox padding with value 114)
      - conf=0.20 (confidence threshold)
      - iou=0.45 (NMS IoU threshold)
      - 3-channel 8-bit BGR input normalization
      - unpad Letterbox coordinates back to original image space
    """
    is_mock: bool = False

    def __init__(
        self,
        model_key: str,
        artifact_path: str,
        confidence_threshold: float = 0.20,
        iou_threshold: float = 0.45,
        image_size: int = 640,
        max_detections: Optional[int] = None
    ):
        self.model_key = model_key
        self.artifact_path = artifact_path
        self.confidence_threshold = confidence_threshold
        self.iou_threshold = iou_threshold
        self.image_size = image_size
        self.max_detections = max_detections
        self._model = None
        self._ort_session = None
        self._ort_input_name = None
        self._is_loaded = False
        self._is_onnx = artifact_path.lower().endswith(".onnx")

    def load(self) -> None:
        if not os.path.exists(self.artifact_path):
            raise FileNotFoundError(f"YOLO model weights file not found at: {self.artifact_path}")

        try:
            # NumPy 2.4+ compatibility fix for Ultralytics
            if not hasattr(np, "trapz") and hasattr(np, "trapezoid"):
                np.trapz = np.trapezoid

            if self._is_onnx:
                import onnxruntime as ort
                logger.info(f"Loading YOLO ONNX model from: {self.artifact_path}")
                self._ort_session = ort.InferenceSession(self.artifact_path, providers=["CPUExecutionProvider"])
                self._ort_input_name = self._ort_session.get_inputs()[0].name
                self._is_loaded = True
                logger.info(f"[OK] YOLO ONNX model '{self.model_key}' loaded successfully!")
            else:
                from ultralytics import YOLO
                logger.info(f"Loading YOLO PyTorch model from: {self.artifact_path} ({os.path.getsize(self.artifact_path) / (1024**2):.2f} MB)")
                self._model = YOLO(self.artifact_path)
                self._is_loaded = True
                logger.info(f"[OK] YOLO model '{self.model_key}' loaded successfully! Classes: {getattr(self._model, 'names', {})}")
        except Exception as e:
            self._is_loaded = False
            raise RuntimeError(f"Failed to load YOLO weights '{self.artifact_path}': {e}") from e

    def is_loaded(self) -> bool:
        if self._is_onnx:
            return self._is_loaded and self._ort_session is not None
        return self._is_loaded and self._model is not None

    def unload(self) -> None:
        self._model = None
        self._ort_session = None
        self._is_loaded = False

    def predict(self, image: Any, run_id: str) -> List[DetectionResult]:
        if not self.is_loaded():
            raise RuntimeError(f"Model '{self.model_key}' is not loaded.")

        # Normalize input image into 3-channel 8-bit BGR OpenCV format
        cv_image = to_cv_bgr(image)
        orig_h, orig_w = cv_image.shape[:2]

        if self._is_onnx:
            return self._predict_onnx(cv_image, orig_w, orig_h)
        else:
            return self._predict_ultralytics(cv_image)

    def _predict_ultralytics(self, cv_image: np.ndarray) -> List[DetectionResult]:
        # Execute YOLO predict with manifest-driven parameters
        predict_kwargs = {
            "source": cv_image,
            "conf": self.confidence_threshold,
            "iou": self.iou_threshold,
            "imgsz": self.image_size,
            "rect": False,
            "save": False,
            "verbose": False
        }
        if self.max_detections is not None:
            predict_kwargs["max_det"] = self.max_detections

        results = self._model.predict(**predict_kwargs)
        detections: List[DetectionResult] = []

        for r in results:
            if r.boxes is None or len(r.boxes) == 0:
                continue

            for box in r.boxes:
                class_id = int(box.cls[0].item() if hasattr(box.cls[0], "item") else box.cls[0])
                confidence = float(box.conf[0].item() if hasattr(box.conf[0], "item") else box.conf[0])

                xyxy = box.xyxy[0].tolist()
                x1, y1, x2, y2 = xyxy

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

    def _predict_onnx(self, cv_image: np.ndarray, orig_w: int, orig_h: int) -> List[DetectionResult]:
        # 1. Preprocessing: Standard Letterbox to target image_size x image_size (640x640)
        target_size = self.image_size
        gain = min(target_size / orig_h, target_size / orig_w)
        new_unpad = (round(orig_w * gain), round(orig_h * gain))
        dw = (target_size - new_unpad[0]) / 2.0
        dh = (target_size - new_unpad[1]) / 2.0
        top, bottom = round(dh - 0.1), round(dh + 0.1)
        left, right = round(dw - 0.1), round(dw + 0.1)

        resized = cv2.resize(cv_image, new_unpad, interpolation=cv2.INTER_LINEAR)
        padded = cv2.copyMakeBorder(resized, top, bottom, left, right, cv2.BORDER_CONSTANT, value=(114, 114, 114))

        # 2. Color space: BGR to RGB, Float32, normalize [0.0, 1.0], shape (1, 3, 640, 640)
        rgb = cv2.cvtColor(padded, cv2.COLOR_BGR2RGB)
        tensor = rgb.transpose((2, 0, 1))[None].astype(np.float32) / 255.0

        # 3. ONNX Inference
        outputs = self._ort_session.run(None, {self._ort_input_name: tensor})
        # Output tensor shape: [1, 5, 8400] -> Transpose to [8400, 5]
        preds = outputs[0][0].T

        # 4. Filter by confidence threshold
        conf_mask = preds[:, 4] >= self.confidence_threshold
        candidates = preds[conf_mask]
        if len(candidates) == 0:
            return []

        xc, yc, w, h, conf = candidates[:, 0], candidates[:, 1], candidates[:, 2], candidates[:, 3], candidates[:, 4]

        # 5. Remove Letterbox padding using exact ops.scale_boxes formulation
        pad_x = round((target_size - orig_w * gain) / 2.0 - 0.1)
        pad_y = round((target_size - orig_h * gain) / 2.0 - 0.1)

        x1 = np.clip((xc - w / 2.0 - pad_x) / gain, 0, orig_w)
        y1 = np.clip((yc - h / 2.0 - pad_y) / gain, 0, orig_h)
        x2 = np.clip((xc + w / 2.0 - pad_x) / gain, 0, orig_w)
        y2 = np.clip((yc + h / 2.0 - pad_y) / gain, 0, orig_h)

        # 6. Apply Non-Maximum Suppression (IoU threshold = 0.45)
        boxes_xywh = np.stack([x1, y1, x2 - x1, y2 - y1], axis=1).tolist()
        indices = cv2.dnn.NMSBoxes(boxes_xywh, conf.tolist(), self.confidence_threshold, self.iou_threshold)

        detections: List[DetectionResult] = []
        if len(indices) > 0:
            for idx in indices.flatten():
                box_x = float(x1[idx])
                box_y = float(y1[idx])
                box_w = max(0.0, float(x2[idx] - x1[idx]))
                box_h = max(0.0, float(y2[idx] - y1[idx]))

                detections.append(DetectionResult(
                    className="fracture",
                    confidence=round(float(conf[idx]), 4),
                    boundingBox=BoundingBox(
                        x=round(box_x, 2),
                        y=round(box_y, 2),
                        width=round(box_w, 2),
                        height=round(box_h, 2)
                    )
                ))

        return detections
