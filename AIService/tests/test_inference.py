import io
from fastapi.testclient import TestClient
from PIL import Image
from app.main import app

client = TestClient(app)

def _create_sample_jpeg() -> bytes:
    img = Image.new("RGB", (200, 200), color=(128, 128, 128))
    buf = io.BytesIO()
    img.save(buf, format="JPEG")
    return buf.getvalue()

def test_inference_successful():
    jpeg_bytes = _create_sample_jpeg()
    files = {"image": ("wrist.jpg", jpeg_bytes, "image/jpeg")}
    data = {
        "analysisRunId": "11111111-2222-3333-4444-555555555555",
        "modelKey": "yolov8m",
        "modelVersion": "1.0.0"
    }

    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 200
    res = response.json()

    assert res["analysisRunId"] == "11111111-2222-3333-4444-555555555555"
    assert res["modelKey"] == "yolov8m"
    assert res["imageWidth"] == 200
    assert res["imageHeight"] == 200

    # Verify provenance
    assert res["isMockResult"] is False
    assert res["warning"] is None

def test_inference_empty_image_fails():
    files = {"image": ("empty.jpg", b"", "image/jpeg")}
    data = {
        "analysisRunId": "11111111-2222-3333-4444-555555555555",
        "modelKey": "yolov8m"
    }
    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 400

def test_inference_corrupt_image_fails():
    files = {"image": ("corrupt.jpg", b"garbage-bytes-not-image", "image/jpeg")}
    data = {
        "analysisRunId": "11111111-2222-3333-4444-555555555555",
        "modelKey": "yolov8m"
    }
    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 400

def test_inference_unknown_model_fails():
    jpeg_bytes = _create_sample_jpeg()
    files = {"image": ("wrist.jpg", jpeg_bytes, "image/jpeg")}
    data = {
        "analysisRunId": "11111111-2222-3333-4444-555555555555",
        "modelKey": "unknown-model-xyz"
    }
    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 404
