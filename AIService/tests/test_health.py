from fastapi.testclient import TestClient
from app.main import app

client = TestClient(app)

def test_health_liveness():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "healthy"
    assert "version" in data

def test_readiness():
    response = client.get("/ready")
    assert response.status_code == 200
    data = response.json()
    assert data["isReady"] is True
    assert "mock-bone-fracture-v1" in data["loadedModels"]
