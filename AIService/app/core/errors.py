class AIServiceException(Exception):
    def __init__(self, message: str, status_code: int = 400):
        super().__init__(message)
        self.message = message
        self.status_code = status_code

class ModelNotFoundException(AIServiceException):
    def __init__(self, model_key: str):
        super().__init__(f"Model '{model_key}' was not found in registry.", status_code=404)

class ModelNotReadyException(AIServiceException):
    def __init__(self, model_key: str, reason: str = "Model is not loaded."):
        super().__init__(f"Model '{model_key}' is not ready for inference: {reason}", status_code=503)

class InvalidImageException(AIServiceException):
    def __init__(self, message: str = "The provided file is not a valid or decodable image."):
        super().__init__(message, status_code=400)
