from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)

def test_list_models():
    response = client.get("/api/v1/models")
    assert response.status_code == 200
    data = response.json()
    assert data["count"] >= 1
    keys = [m["modelKey"] for m in data["models"]]
    assert "mock-bone-fracture-v1" in keys

def test_get_model_details():
    response = client.get("/api/v1/models/mock-bone-fracture-v1")
    assert response.status_code == 200
    data = response.json()
    assert data["modelKey"] == "mock-bone-fracture-v1"
    assert data["status"] == "Loaded"
    assert data["isActive"] is True

def test_get_nonexistent_model():
    response = client.get("/api/v1/models/non-existent-model")
    assert response.status_code == 404
