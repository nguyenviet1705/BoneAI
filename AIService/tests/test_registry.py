import json
import os
import tempfile
from fastapi.testclient import TestClient
from app.main import app
from app.registry.model_registry import ModelRegistry
from app.schemas.models import ModelStatus

client = TestClient(app)

def test_list_models():
    response = client.get("/api/v1/models")
    assert response.status_code == 200
    data = response.json()
    assert data["count"] >= 1
    keys = [m["modelKey"] for m in data["models"]]
    assert "yolov8m" in keys

def test_get_model_details():
    response = client.get("/api/v1/models/yolov8m")
    assert response.status_code == 200
    data = response.json()
    assert data["modelKey"] == "yolov8m"
    assert data["status"] == "Loaded"
    assert data["isActive"] is True

def test_get_nonexistent_model():
    response = client.get("/api/v1/models/non-existent-model")
    assert response.status_code == 404

def test_discover_models_endpoint():
    response = client.post("/api/v1/models/discover")
    assert response.status_code == 200
    data = response.json()
    assert data["discoveredCount"] >= 1
    assert data["loadedCount"] >= 1

def test_isolated_registry_with_invalid_manifest():
    with tempfile.TemporaryDirectory() as tmp_dir:
        # Create a model dir with invalid manifest
        bad_dir = os.path.join(tmp_dir, "bad-model")
        os.makedirs(bad_dir, exist_ok=True)
        with open(os.path.join(bad_dir, "manifest.yaml"), "w") as f:
            f.write("invalid_yaml: [}")

        reg = ModelRegistry(models_dir=tmp_dir)
        # Bad model is recorded as Failed
        bad_model = reg.get_model("bad-model")
        assert bad_model.status == ModelStatus.FAILED
