import glob
import os
import cv2
import numpy as np
import pytest
from fastapi.testclient import TestClient
from PIL import Image
from ultralytics import YOLO

from app.main import app
from app.inference.adapters.yolo_adapter import YoloInferenceAdapter

client = TestClient(app)

WEIGHTS_PT = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "models", "yolov8m-bone-fracture-v1", "weights", "best.pt"
)
WEIGHTS_ONNX = os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
    "models", "yolov8m-bone-fracture-v1", "weights", "best.onnx"
)

# Test images from storage
TEST_16BIT_PNG = r"C:\Users\Admin\source\repos\BoneAI\BoneAI.API\Storage\Images\2026\11\236048fe9e024d42a8f166b01ef2715f.png"
TEST_L_PNG = r"C:\Users\Admin\source\repos\BoneAI\BoneAI.API\Storage\Images\2026\11\209112fb97d24ef496cd406c5e603681.png"
TEST_RGBA_PNG = r"C:\Users\Admin\source\repos\BoneAI\BoneAI.API\Storage\Images\2026\11\ab30b3e3066543819dfcfd11d744ed03.png"

@pytest.fixture(scope="module")
def yolo_model():
    assert os.path.exists(WEIGHTS_PT), f"Weights file missing: {WEIGHTS_PT}"
    return YOLO(WEIGHTS_PT)

@pytest.fixture(scope="module")
def yolo_adapter():
    adapter = YoloInferenceAdapter(
        model_key="yolov8m",
        artifact_path=WEIGHTS_PT,
        confidence_threshold=0.20,
        iou_threshold=0.45,
        image_size=640
    )
    adapter.load()
    return adapter

def test_16bit_image_pipeline_matches_ultralytics(yolo_model, yolo_adapter):
    """
    Test 1: 16-bit Grayscale medical X-ray (I;16).
    Verifies that the project pipeline decodes and predicts identically to Ultralytics YOLO.predict on file path (Colab style).
    """
    assert os.path.exists(TEST_16BIT_PNG), f"Sample image missing: {TEST_16BIT_PNG}"

    # 1. Standard Ultralytics prediction (exact Colab style: no rect parameter)
    res_standard = yolo_model.predict(source=TEST_16BIT_PNG, imgsz=640, conf=0.20, iou=0.45, verbose=False)[0]
    expected_boxes = res_standard.boxes.xyxy.tolist()
    expected_confs = [round(float(c), 4) for c in res_standard.boxes.conf]

    assert len(expected_boxes) > 0, "Reference model should detect fractures in test sample"

    # 2. Project adapter prediction from PIL Image (mode I;16)
    pil_image = Image.open(TEST_16BIT_PNG)
    assert pil_image.mode == "I;16"
    adapter_results = yolo_adapter.predict(pil_image, run_id="test-run-16bit")

    assert len(adapter_results) == len(expected_boxes), (
        f"Detection count mismatch on 16-bit image! Expected {len(expected_boxes)}, got {len(adapter_results)}"
    )

    for i, det in enumerate(adapter_results):
        exp_box = expected_boxes[i]
        exp_conf = expected_confs[i]

        exp_x1, exp_y1, exp_x2, exp_y2 = exp_box
        exp_w = exp_x2 - exp_x1
        exp_h = exp_y2 - exp_y1

        # Check coordinates error is zero (within 0.05px rounding)
        assert abs(det.boundingBox.x - round(exp_x1, 2)) <= 0.05
        assert abs(det.boundingBox.y - round(exp_y1, 2)) <= 0.05
        assert abs(det.boundingBox.width - round(exp_w, 2)) <= 0.05
        assert abs(det.boundingBox.height - round(exp_h, 2)) <= 0.05

        # Check confidence error is zero
        assert abs(det.confidence - exp_conf) <= 0.0001
        assert det.className == "fracture"

def test_8bit_grayscale_image_pipeline_matches_ultralytics(yolo_model, yolo_adapter):
    """
    Test 2: 8-bit Grayscale image (mode L).
    """
    assert os.path.exists(TEST_L_PNG)

    # Colab style: source=path, no rect param
    res_standard = yolo_model.predict(source=TEST_L_PNG, imgsz=640, conf=0.20, iou=0.45, verbose=False)[0]
    expected_boxes = res_standard.boxes.xyxy.tolist()
    expected_confs = [round(float(c), 4) for c in res_standard.boxes.conf]

    pil_image = Image.open(TEST_L_PNG)
    assert pil_image.mode == "L"
    adapter_results = yolo_adapter.predict(pil_image, run_id="test-run-l")

    assert len(adapter_results) == len(expected_boxes)
    for i, det in enumerate(adapter_results):
        exp_box = expected_boxes[i]
        assert abs(det.confidence - expected_confs[i]) <= 0.0001
        assert abs(det.boundingBox.x - round(exp_box[0], 2)) <= 0.05

def test_rgba_image_pipeline_matches_ultralytics(yolo_model, yolo_adapter):
    """
    Test 3: RGBA image.
    """
    assert os.path.exists(TEST_RGBA_PNG)

    # Colab style: source=path, no rect param
    res_standard = yolo_model.predict(source=TEST_RGBA_PNG, imgsz=640, conf=0.20, iou=0.45, verbose=False)[0]
    expected_boxes = res_standard.boxes.xyxy.tolist()
    expected_confs = [round(float(c), 4) for c in res_standard.boxes.conf]

    pil_image = Image.open(TEST_RGBA_PNG)
    assert pil_image.mode == "RGBA"
    adapter_results = yolo_adapter.predict(pil_image, run_id="test-run-rgba")

    assert len(adapter_results) == len(expected_boxes)
    for i, det in enumerate(adapter_results):
        exp_box = expected_boxes[i]
        assert abs(det.confidence - expected_confs[i]) <= 0.0001
        assert abs(det.boundingBox.x - round(exp_box[0], 2)) <= 0.05

def test_onnx_runtime_pipeline_alignment(yolo_model):
    """
    Test 4: ONNX Runtime engine.
    Verifies pure Letterbox, (114, 114, 114) padding, [1, 5, 8400] transpose, and unpadding alignment.
    """
    if not os.path.exists(WEIGHTS_ONNX):
        pytest.skip("ONNX model best.onnx not present.")

    adapter_onnx = YoloInferenceAdapter(
        model_key="yolov8m-onnx",
        artifact_path=WEIGHTS_ONNX,
        confidence_threshold=0.20,
        iou_threshold=0.45,
        image_size=640
    )
    adapter_onnx.load()

    bgr = cv2.imread(TEST_16BIT_PNG)
    res_pt = yolo_model.predict(source=bgr, imgsz=640, conf=0.20, iou=0.45, rect=False, verbose=False)[0]
    expected_boxes = res_pt.boxes.xyxy.tolist()
    expected_confs = res_pt.boxes.conf.tolist()

    onnx_results = adapter_onnx.predict(bgr, run_id="test-onnx")
    assert len(onnx_results) == len(expected_boxes)

    for i, det in enumerate(onnx_results):
        exp_box = expected_boxes[i]
        exp_conf = expected_confs[i]

        # Coordinates difference < 2 pixels due to FP32 rounding & unpadding
        assert abs(det.boundingBox.x - exp_box[0]) < 2.0
        assert abs(det.boundingBox.y - exp_box[1]) < 2.0
        # Confidence difference < 0.005
        assert abs(det.confidence - exp_conf) < 0.005

def test_fastapi_inference_endpoint_with_16bit_image():
    """
    Test 5: End-to-end HTTP inference endpoint with 16-bit PNG upload.
    """
    with open(TEST_16BIT_PNG, "rb") as f:
        file_bytes = f.read()

    files = {"image": ("wrist_16bit.png", file_bytes, "image/png")}
    data = {
        "analysisRunId": "99999999-8888-7777-6666-555555555555",
        "modelKey": "yolov8m",
        "modelVersion": "1.0.0"
    }

    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 200
    res = response.json()

    assert res["modelKey"] == "yolov8m"
    assert res["isMockResult"] is False
    assert len(res["detections"]) > 0, "16-bit medical image should return valid fracture detections"

    for det in res["detections"]:
        assert det["className"] == "fracture"
        assert det["confidence"] >= 0.20
        box = det["boundingBox"]
        assert box["x"] >= 0
        assert box["y"] >= 0
        assert box["width"] > 0
        assert box["height"] > 0

def test_invalid_model_path_raises_error():
    """Test 6: Invalid model weights path produces meaningful FileNotFoundError."""
    adapter = YoloInferenceAdapter(
        model_key="invalid-model",
        artifact_path="non/existent/path/weights.pt"
    )
    with pytest.raises(FileNotFoundError) as exc_info:
        adapter.load()
    assert "not found" in str(exc_info.value).lower()

def test_model_sha256_checksum_calculated(yolo_adapter):
    """Test 7: Verify SHA256 checksum is correctly calculated during model load."""
    assert yolo_adapter.sha256 is not None
    assert len(yolo_adapter.sha256) == 64
    # Check known SHA256 hash prefix for best.pt
    assert yolo_adapter.sha256.startswith("9710aec0682d4303")

def test_blank_image_returns_empty_detections(yolo_adapter):
    """Test 8: Blank image produces zero detections cleanly without crashing."""
    blank_img = np.zeros((400, 400, 3), dtype=np.uint8)
    results = yolo_adapter.predict(blank_img, run_id="test-blank")
    assert isinstance(results, list)
    assert len(results) == 0

def test_boundary_clipping_strictly_within_bounds(yolo_adapter):
    """Test 9: Verify boundary clipping guarantees all boxes stay strictly within [0, W] and [0, H]."""
    test_img = cv2.imread(TEST_16BIT_PNG)
    h, w = test_img.shape[:2]
    results = yolo_adapter.predict(test_img, run_id="test-bounds")
    for det in results:
        b = det.boundingBox
        assert b.x >= 0.0
        assert b.y >= 0.0
        assert b.x + b.width <= w + 1e-2
        assert b.y + b.height <= h + 1e-2
