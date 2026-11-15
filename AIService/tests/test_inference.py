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
        "modelKey": "mock-bone-fracture-v1",
        "modelVersion": "1.0.0"
    }

    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 200
    res = response.json()

    assert res["analysisRunId"] == "11111111-2222-3333-4444-555555555555"
    assert res["modelKey"] == "mock-bone-fracture-v1"
    assert res["imageWidth"] == 200
    assert res["imageHeight"] == 200
    assert len(res["detections"]) > 0

    det = res["detections"][0]
    assert det["className"] == "fracture"
    assert 0.0 <= det["confidence"] <= 1.0

    box = det["boundingBox"]
    assert box["x"] >= 0 and box["y"] >= 0
    assert box["x"] + box["width"] <= 200
    assert box["y"] + box["height"] <= 200

    # Verify provenance
    assert res["isMockResult"] is True
    assert "mock" in res["warning"].lower()

def test_inference_empty_image_fails():
    files = {"image": ("empty.jpg", b"", "image/jpeg")}
    data = {
        "analysisRunId": "11111111-2222-3333-4444-555555555555",
        "modelKey": "mock-bone-fracture-v1"
    }
    response = client.post("/api/v1/inference", data=data, files=files)
    assert response.status_code == 400

def test_inference_corrupt_image_fails():
    files = {"image": ("corrupt.jpg", b"garbage-bytes-not-image", "image/jpeg")}
    data = {
        "analysisRunId": "11111111-2222-3333-4444-555555555555",
        "modelKey": "mock-bone-fracture-v1"
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
