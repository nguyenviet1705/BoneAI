# BoneAI Python AI Service

FastAPI-based inference service for BoneAI pediatric wrist fracture detection.

## Architecture

- **`app/api/`**: API endpoints (`health.py`, `models.py`, `inference.py`).
- **`app/registry/`**: Model registry managing metadata and adapter instances.
- **`app/inference/interfaces/`**: Common `BaseInferenceAdapter` interface.
- **`app/inference/adapters/`**: Model architecture implementations (e.g., `MockInferenceAdapter`).
- **`app/schemas/`**: Pydantic request/response schemas.

## Running the Service

```bash
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

## Running Tests

```bash
pytest
```
