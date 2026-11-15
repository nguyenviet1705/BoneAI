import asyncio
import io
import time
from typing import Optional
import cv2
from fastapi import APIRouter, File, Form, UploadFile
import numpy as np
from PIL import Image, UnidentifiedImageError
from starlette.concurrency import run_in_threadpool

from app.core.config import settings
from app.core.errors import InvalidImageException, ModelNotReadyException
from app.registry.model_registry import registry
from app.schemas.inference import InferenceResponse

router = APIRouter(prefix="/api/v1/inference", tags=["Inference"])

ALLOWED_MIME_TYPES = {"image/jpeg", "image/png"}

# Concurrency semaphore to guard GPU/CPU resources
_inference_semaphore = asyncio.Semaphore(settings.max_concurrent_inferences)

@router.post("", response_model=InferenceResponse)
async def run_inference(
    analysisRunId: str = Form(..., description="Correlated AnalysisRunId from Backend"),
    modelKey: str = Form(..., description="Key of the requested model in the registry"),
    modelVersion: Optional[str] = Form(None, description="Optional requested model version"),
    image: UploadFile = File(..., description="Raw original X-ray image stream (JPEG or PNG)")
):
    """
    Execute AI inference on an uploaded X-ray image using the specified registered model.
    Governed by concurrency control to protect computing resources.
    """
    start_time = time.perf_counter()

    # Validate Content-Type
    if image.content_type and image.content_type.lower() not in ALLOWED_MIME_TYPES:
        raise InvalidImageException(
            f"Unsupported image MIME type: {image.content_type}. Allowed: {', '.join(ALLOWED_MIME_TYPES)}"
        )

    # Read and decode image
    try:
        contents = await image.read()
        if not contents or len(contents) == 0:
            raise InvalidImageException("Uploaded image payload is empty (zero bytes).")

        try:
            pil_image = Image.open(io.BytesIO(contents))
            pil_image.verify()  # Verify integrity
            pil_image = Image.open(io.BytesIO(contents))
        except Exception:
            raise InvalidImageException("The provided file could not be decoded as a valid image.")

        # Decode standard 8-bit 3-channel OpenCV image (properly downscaling 16-bit integer X-rays)
        nparr = np.frombuffer(contents, np.uint8)
        cv_img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        if cv_img is not None:
            img_height, img_width = cv_img.shape[:2]
            processed_image = cv_img
        else:
            img_width, img_height = pil_image.size
            processed_image = pil_image
    except UnidentifiedImageError:
        raise InvalidImageException("The provided file could not be decoded as a valid image.")
    except Exception as e:
        if isinstance(e, InvalidImageException):
            raise
        raise InvalidImageException(f"Error decoding image stream: {str(e)}")

    # Get model and adapter
    model_info = registry.get_model(modelKey)
    if modelVersion:
        # Flexible version check: ignore parenthetical labels and minor subversion format differences
        norm_req = modelVersion.split("(")[0].strip().rstrip(".")
        norm_mod = model_info.version.split("(")[0].strip().rstrip(".")
        if norm_req and norm_mod and not (norm_req in norm_mod or norm_mod in norm_req):
            raise ModelNotReadyException(
                modelKey, f"Version mismatch: requested '{modelVersion}', but registered version is '{model_info.version}'."
            )

    adapter = registry.get_adapter(modelKey)

    # Execute model prediction under concurrency control via thread pool
    async with _inference_semaphore:
        detections = await run_in_threadpool(adapter.predict, processed_image, analysisRunId)

    elapsed_ms = int((time.perf_counter() - start_time) * 1000)

    is_mock = bool(getattr(adapter, "is_mock", False))
    warning = getattr(adapter, "mock_warning", None) if is_mock else None

    return InferenceResponse(
        analysisRunId=analysisRunId,
        modelKey=modelKey,
        modelVersion=model_info.version,
        imageWidth=img_width,
        imageHeight=img_height,
        processingTimeMs=elapsed_ms,
        detections=detections,
        isMockResult=is_mock,
        warning=warning
    )
