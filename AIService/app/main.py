import logging
from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from app.api import health, inference, models
from app.core.config import settings
from app.core.errors import AIServiceException

logging.basicConfig(level=settings.log_level.upper())
logger = logging.getLogger("aiservice")

app = FastAPI(
    title=settings.app_name,
    version=settings.app_version,
    description="Python FastAPI Inference Service for BoneAI Pediatric Wrist Fracture Detection"
)

# Exception handlers
@app.exception_handler(AIServiceException)
async def ai_service_exception_handler(request: Request, exc: AIServiceException):
    return JSONResponse(
        status_code=exc.status_code,
        content={"error": exc.message, "type": exc.__class__.__name__}
    )

@app.exception_handler(Exception)
async def generic_exception_handler(request: Request, exc: Exception):
    logger.error(f"Unhandled exception during request {request.url}: {exc}", exc_info=True)
    return JSONResponse(
        status_code=500,
        content={"error": "An unexpected internal error occurred in the AI Service.", "details": str(exc)}
    )

# Include API routers
app.include_router(health.router)
app.include_router(models.router)
app.include_router(inference.router)

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("app.main:app", host=settings.host, port=settings.port, reload=True)
