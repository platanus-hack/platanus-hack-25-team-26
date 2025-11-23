import base64
import time
import os
import asyncio
import io
import hashlib
from contextlib import AsyncExitStack
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from langchain_anthropic import ChatAnthropic
from langchain_core.messages import HumanMessage, SystemMessage
from PIL import Image
import aioboto3
from botocore.config import Config
from cachetools import TTLCache

from pydantic import BaseModel, Field

from prompts import EMAIL_PHISHING_PROMPT, SOCIAL_ENGINEERING_PROMPT, UNIFIED_EVALUATION_PROMPT
from email_service import send_phishing_alert, send_whatsapp_notification
from dotenv import load_dotenv
from pathlib import Path

# Load environment variables from api/.env (if present)
env_path = Path(__file__).resolve().parent / ".env"
load_dotenv(dotenv_path=env_path)

app = FastAPI()

# Request tracking for monitoring (in-memory metrics)
active_requests = {"count": 0}

@app.on_event("startup")
async def startup_event():
    """Initialize reusable clients on startup"""
    await get_textract_client()

@app.on_event("shutdown")
async def shutdown_event():
    """Clean up reusable clients on shutdown"""
    global textract_client
    if textract_client is not None:
        try:
            await textract_client.__aexit__(None, None, None)
        except Exception:
            pass
        textract_client = None

@app.middleware("http")
async def track_requests(request, call_next):
    """Middleware to track concurrent requests"""
    active_requests["count"] += 1
    start_time = time.time()
    try:
        response = await call_next(request)
        process_time = time.time() - start_time
        response.headers["X-Process-Time"] = str(process_time)
        return response
    finally:
        active_requests["count"] -= 1

# Configurar CORS para permitir peticiones desde cualquier origen
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Permite todos los orígenes
    allow_credentials=True,
    allow_methods=["*"],  # Permite todos los métodos (GET, POST, etc.)
    allow_headers=["*"],  # Permite todos los headers
)

# API Configuration (read from environment/.env)
api_key = os.getenv('ANTHROPIC_API_KEY') or os.getenv('API_KEY')

# AWS credentials (read from environment/.env)
aws_access_key_id = os.getenv('AWS_ACCESS_KEY_ID')
aws_secret_access_key = os.getenv('AWS_SECRET_ACCESS_KEY')
aws_region = os.getenv('AWS_REGION', 'us-east-1')

# Create async boto3 session (reused across requests for connection pooling)
boto3_session = aioboto3.Session()

# Botocore config with aligned connection pool
textract_config = Config(max_pool_connections=15)

# Global Textract client (reused across requests)
textract_client = None
textract_client_lock = asyncio.Lock()

# Semaphores to limit concurrent external API calls and prevent overload
# Limit concurrent Claude API calls to prevent rate limiting
claude_semaphore = asyncio.Semaphore(10)
# Limit concurrent Textract calls to prevent AWS throttling (aligned with pool)
textract_semaphore = asyncio.Semaphore(15)

# Response cache (TTL: 1 hour, max 1000 entries)
response_cache = TTLCache(maxsize=1000, ttl=3600)

# Timeout configurations (in seconds)
CLAUDE_TIMEOUT = 30  # 30 seconds for Claude API calls
TEXTRACT_TIMEOUT = 20  # 20 seconds for Textract calls
IMAGE_OPTIMIZATION_TIMEOUT = 10  # 10 seconds for image optimization

# Thresholds for smart optimization
IMAGE_SIZE_THRESHOLD_KB = 500  # Skip optimization for images < 500KB
IMAGE_WIDTH_THRESHOLD = 1500  # Skip optimization if width < 1500px

def get_model():
    """
    Factory function to create a LangChain Anthropic async model instance.
    Uses native async client to avoid threadpool fallback.
    """
    model_b = ChatAnthropic(
        model="claude-sonnet-4-5-20250929",
        anthropic_api_key=api_key,
        max_tokens=1024,
        timeout=CLAUDE_TIMEOUT,
    )

    return ChatAnthropic(
        model="claude-haiku-4-5-20251001",
        anthropic_api_key=api_key,
        max_tokens=1024,
        timeout=CLAUDE_TIMEOUT
    ).with_fallbacks([model_b])

def optimize_image_for_textract(image_bytes: bytes, max_width: int = 1500, jpeg_quality: int = 85) -> bytes:
    """
    Optimizes PNG images for Textract processing:
    - Resizes to max_width (maintains aspect ratio)
    - Converts to grayscale
    - Converts to JPEG format
    - Uses high-quality Lanczos resampling
    
    Args:
        image_bytes: Original image bytes (PNG format)
        max_width: Maximum width in pixels (default 1500)
        jpeg_quality: JPEG quality 1-100 (default 85)
    
    Returns:
        Optimized image bytes in JPEG format
    """
    try:
        # Open image from bytes
        image = Image.open(io.BytesIO(image_bytes))
        
        # Get original dimensions
        original_width, original_height = image.size
        
        # Resize if needed (maintain aspect ratio)
        if original_width > max_width:
            # Calculate new height maintaining aspect ratio
            aspect_ratio = original_height / original_width
            new_width = max_width
            new_height = int(max_width * aspect_ratio)
            
            # Resize with high-quality Lanczos resampling
            image = image.resize((new_width, new_height), Image.Resampling.LANCZOS)
        
        # Convert to grayscale (Textract works well with grayscale)
        if image.mode != 'L':
            image = image.convert('L')
        
        # Convert to JPEG and save to bytes
        output_buffer = io.BytesIO()
        image.save(output_buffer, format='JPEG', quality=jpeg_quality, optimize=True)
        output_buffer.seek(0)
        
        return output_buffer.getvalue()
        
    except Exception:
        # If optimization fails, return original image bytes
        return image_bytes

def should_optimize_image(image_bytes: bytes) -> bool:
    """
    Determine if image optimization is worthwhile.
    Skip optimization for small images or images that are already appropriately sized.
    """
    try:
        size_kb = len(image_bytes) / 1024
        if size_kb < IMAGE_SIZE_THRESHOLD_KB:
            return False
        
        # Quick dimension check without full decode
        image = Image.open(io.BytesIO(image_bytes))
        width, _ = image.size
        return width > IMAGE_WIDTH_THRESHOLD
    except Exception:
        # If check fails, optimize to be safe
        return True

async def optimize_image_async(image_bytes: bytes) -> bytes:
    """
    Async wrapper for image optimization with timeout.
    Uses asyncio.to_thread for elastic threadpool management.
    Skips optimization for small/appropriately-sized images.
    """
    # Skip optimization if not needed
    if not should_optimize_image(image_bytes):
        return image_bytes
    
    try:
        return await asyncio.wait_for(
            asyncio.to_thread(optimize_image_for_textract, image_bytes),
            timeout=IMAGE_OPTIMIZATION_TIMEOUT
        )
    except asyncio.TimeoutError:
        return image_bytes

async def get_textract_client():
    """
    Get or create a reusable Textract client.
    Uses a lock to prevent race conditions during initialization.
    """
    global textract_client
    
    if textract_client is None:
        async with textract_client_lock:
            if textract_client is None:
                textract_client = await boto3_session.client(
                    'textract',
                    aws_access_key_id=aws_access_key_id,
                    aws_secret_access_key=aws_secret_access_key,
                    region_name=aws_region,
                    config=textract_config
                ).__aenter__()
    
    return textract_client

async def extract_text_with_textract(image_bytes: bytes) -> str:
    """
    Async text extraction using AWS Textract with reusable client.
    Uses semaphore to limit concurrent calls and prevent AWS throttling.
    """
    async with textract_semaphore:  # Limit concurrent Textract calls
        try:
            client = await get_textract_client()
            
            # Add timeout to prevent hanging
            response = await asyncio.wait_for(
                client.detect_document_text(
                    Document={'Bytes': image_bytes}
                ),
                timeout=TEXTRACT_TIMEOUT
            )
            
            # Extraer todo el texto detectado
            text_lines = []
            for block in response.get('Blocks', []):
                if block['BlockType'] == 'LINE':
                    text_lines.append(block['Text'])
            
            return '\n'.join(text_lines) if text_lines else "No se pudo extraer texto"
            
        except asyncio.TimeoutError:
            return f"Error en Textract: Timeout después de {TEXTRACT_TIMEOUT}s"
        except Exception as e:
            return f"Error en Textract: {str(e)}"

class PhishingEvaluation(BaseModel):
    scoring: int = Field(description="The scoring of the phishing email from 1 - 10")
    reason: str | None = Field(description="The reason for the phishing email. Use max 15 words")

class SocialEngineeringEvaluation(BaseModel):
    scoring: int = Field(description="The scoring of the social engineering attempt from 1 - 10")
    reason: str | None = Field(description="The reason for the social engineering detection. Use max 15 words")

class Evaluation(BaseModel):
    scoring: int = Field(description="El scoring debe estar entre 1 - 10")
    reason: str | None = Field(description="La razón debe ser una descripcióxn corta")
    type: str | None = Field(description="Whatsapp, Email, etc")

class UnifiedEvaluation(BaseModel):
    """Modelo para evaluación unificada (Método B)"""
    scoring: int = Field(description="Puntuación de riesgo de 1-10", ge=1, le=10)
    reason: str = Field(description="Razón de la evaluación en español (máximo 5 palabras)", examples=["Probable phishing bancario"])
    title: str = Field(description="Phishing, grooming, etc")

class OCRResponse(BaseModel):
    parsed_text: str = Field(description="Texto extraído de la imagen")
    is_error_response: bool = Field(description="Si hubo error en el procesamiento")
    error_message: str | None = Field(description="Mensaje de error si lo hay")
    processing_time: str | None = Field(description="Tiempo de procesamiento")

class EmailAlertRequest(BaseModel):
    scoring: int = Field(description="Puntuación de riesgo de 1-10", ge=1, le=10)
    reason: str = Field(description="Razón de la alerta")
    type: str = Field(description="Tipo de contenido: whatsapp, telegram, sms, email, web, etc.")
    recipient_email: str = Field(description="Email del destinatario donde se enviará la alerta")

class EmailAlertResponse(BaseModel):
    status: str = Field(description="Estado del envío: success o error")
    message: str = Field(description="Mensaje descriptivo del resultado")
    message_id: str | None = Field(default=None, description="ID del mensaje enviado (si es exitoso)")

class WhatsAppNotificationRequest(BaseModel):
    to_number: str = Field(description="Número de teléfono del destinatario (ej: 573001234567)")
    reason: str = Field(description="Razón de la alerta")

class WhatsAppNotificationResponse(BaseModel):
    status: str = Field(description="Estado del envío: success o error")
    message: str = Field(description="Mensaje descriptivo del resultado")
    message_id: str | None = Field(default=None, description="ID del mensaje enviado (si es exitoso)")

def get_image_hash(image_bytes: bytes) -> str:
    """Generate a hash for caching purposes."""
    return hashlib.md5(image_bytes).hexdigest()

@app.get("/health")
async def health_check():
    """Health check endpoint to monitor API status"""
    return {
        "status": "healthy",
        "active_requests": active_requests["count"],
        "cache_size": len(response_cache),
        "claude_semaphore_available": claude_semaphore._value,
        "textract_semaphore_available": textract_semaphore._value,
        "textract_client_initialized": textract_client is not None
    }

@app.get("/")
async def root():
    """Root endpoint"""
    return {
        "service": "Phishing Detection API",
        "status": "running",
        "endpoints": [
            "/health",
            "/evaluate",
            "/evaluate-phishing",
            "/evaluate-social-engineering",
            "/extract-text",
            "/send-alert-email",
            "/send-whatsapp-notification"
        ]
    }

@app.post("/evaluate-phishing")
async def evaluate_phishing(file: UploadFile) -> PhishingEvaluation:
    try:
        image_data = await file.read()
        
        # Check cache first
        image_hash = get_image_hash(image_data)
        cache_key = f"phishing_{image_hash}"
        if cache_key in response_cache:
            return response_cache[cache_key]

        # Optimize image before processing (resize, grayscale, JPEG conversion)
        optimized_image_data = await optimize_image_async(image_data)

        # Extract text asynchronously using AWS Textract
        extracted_text = await extract_text_with_textract(optimized_image_data)

        # Create message with extracted text (text-only model is cheaper than vision)
        message = HumanMessage(
            content=f"{EMAIL_PHISHING_PROMPT}\n\nTexto extraído de la imagen:\n{extracted_text}"
        )

        # Use semaphore to limit concurrent Claude API calls
        async with claude_semaphore:
            # Create a new model instance per request to avoid shared state issues
            model = get_model()
            structured_model = model.with_structured_output(PhishingEvaluation)
            
            # Add timeout to Claude API call
            response = await asyncio.wait_for(
                structured_model.ainvoke([message]),
                timeout=CLAUDE_TIMEOUT
            )
        
        # Cache the response
        response_cache[cache_key] = response
        
        return response
    
    except asyncio.TimeoutError:
        raise HTTPException(status_code=504, detail=f"Request timed out after {CLAUDE_TIMEOUT}s")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Internal error: {str(e)}")

@app.post("/evaluate-social-engineering")
async def evaluate_social_engineering(file: UploadFile) -> SocialEngineeringEvaluation:
    try:
        image_data = await file.read()

        image_base64 = base64.b64encode(image_data).decode('utf-8')

        # Determine image format from content type or filename
        if file.content_type and '/' in file.content_type:
            image_format = file.content_type.split('/')[1]
        else:
            # Fallback: try to determine from filename
            image_format = file.filename.split('.')[-1] if file.filename and '.' in file.filename else 'png'

        image_url = f"data:image/{image_format};base64,{image_base64}"

        message = HumanMessage(
            content=[
                {
                    "type": "text",
                    "text": SOCIAL_ENGINEERING_PROMPT,
                },
                {
                    "type": "image_url",
                    "image_url": {"url": image_url}
                }
            ]
        )

        # Use semaphore to limit concurrent Claude API calls
        async with claude_semaphore:
            # Create a new model instance per request to avoid shared state issues
            model = get_model()
            structured_model = model.with_structured_output(SocialEngineeringEvaluation)
            
            # Add timeout to Claude API call
            response = await asyncio.wait_for(
                structured_model.ainvoke([message]),
                timeout=CLAUDE_TIMEOUT
            )

        return response
    
    except asyncio.TimeoutError:
        raise HTTPException(status_code=504, detail=f"Request timed out after {CLAUDE_TIMEOUT}s")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Internal error: {str(e)}")

@app.post("/evaluate")
async def evaluate_unified(file: UploadFile) -> UnifiedEvaluation:
    try:
        image_data = await file.read()

        # Check cache first
        image_hash = get_image_hash(image_data)
        cache_key = f"unified_{image_hash}"
        if cache_key in response_cache:
            return response_cache[cache_key]

        # Optimize image before processing (resize, grayscale, JPEG conversion)
        optimized_image_data = await optimize_image_async(image_data)

        # Extract text asynchronously using AWS Textract
        extracted_text = await extract_text_with_textract(optimized_image_data)

        # Create message with extracted text (text-only model is cheaper than vision)
        system_message = SystemMessage(
            content=UNIFIED_EVALUATION_PROMPT
        )
        message = HumanMessage(
            content=f"Texto extraído de la imagen:\n{extracted_text}"
        )

        # Use semaphore to limit concurrent Claude API calls
        async with claude_semaphore:
            # Create a new model instance per request to avoid shared state issues
            model = get_model()
            structured_model = model.with_structured_output(UnifiedEvaluation)
            
            # Add timeout to Claude API call
            response = await asyncio.wait_for(
                structured_model.ainvoke([system_message, message]),
                timeout=CLAUDE_TIMEOUT
            )

        # Cache the response
        response_cache[cache_key] = response

        return response
    
    except asyncio.TimeoutError:
        raise HTTPException(status_code=504, detail=f"Request timed out after {CLAUDE_TIMEOUT}s")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Internal error: {str(e)}")

@app.post("/extract-text")
async def extract_text(file: UploadFile) -> OCRResponse:
    """
    Extrae texto de una imagen usando AWS Textract
    """
    try:
        start_time = time.time()
        # Leer imagen
        image_data = await file.read()
        
        # Check cache first
        image_hash = get_image_hash(image_data)
        cache_key = f"ocr_{image_hash}"
        if cache_key in response_cache:
            cached_result = response_cache[cache_key]
            processing_time_ms = int((time.time() - start_time) * 1000)
            return OCRResponse(
                parsed_text=cached_result,
                is_error_response=False,
                error_message=None,
                processing_time=str(processing_time_ms)
            )
        
        # Optimize image before processing (resize, grayscale, JPEG conversion)
        optimized_image_data = await optimize_image_async(image_data)
        
        # Extraer texto usando AWS Textract con imagen optimizada (native async)
        extracted_text = await extract_text_with_textract(optimized_image_data)
        
        end_time = time.time()
        processing_time_ms = int((end_time - start_time) * 1000)
        
        # Verificar si hubo error
        is_error = extracted_text.startswith("Error en Textract:")
        error_message = extracted_text if is_error else None
        parsed_text = extracted_text if not is_error else ""
        
        # Cache successful results
        if not is_error:
            response_cache[cache_key] = parsed_text
        
        return OCRResponse(
            parsed_text=parsed_text,
            is_error_response=is_error,
            error_message=error_message,
            processing_time=str(processing_time_ms)
        )
    
    except asyncio.TimeoutError:
        return OCRResponse(
            parsed_text="",
            is_error_response=True,
            error_message=f"Timeout: La extracción de texto excedió {TEXTRACT_TIMEOUT}s",
            processing_time=None
        )
    except Exception as e:
        return OCRResponse(
            parsed_text="",
            is_error_response=True,
            error_message=str(e),
            processing_time=None
        )

@app.post("/send-alert-email")
async def send_alert_email(request: EmailAlertRequest) -> EmailAlertResponse:
    """
    Envía un email de alerta de ingeniería social al padre de familia usando Resend.

    Args:
<<<<<<< Updated upstream
        request: Datos de la alerta incluyendo scoring, reason, type y recipient_email
=======
        request: Datos de la alerta incluyendo scoring, reason, type
>>>>>>> Stashed changes

    Returns:
        EmailAlertResponse con el estado del envío
    """
    try:
        # Preparar los datos para el servicio de email
        email_data = {
            "scoring": request.scoring,
            "reason": request.reason,
            "type": request.type
        }

        # Enviar el email usando el servicio
        result = send_phishing_alert(data=email_data, recipient_email=request.recipient_email)

        return EmailAlertResponse(
            status=result["status"],
            message=result["message"],
            message_id=result.get("message_id")
        )

    except Exception as e:
        return EmailAlertResponse(
            status="error",
            message=f"Error al enviar email: {str(e)}"
        )

@app.post("/send-whatsapp-notification")
async def send_whatsapp_notification_endpoint(request: WhatsAppNotificationRequest) -> WhatsAppNotificationResponse:
    """
    Envía una notificación de WhatsApp cuando se detecta ingeniería social.

    Args:
        request: Datos de la notificación (to_number, reason)

    Returns:
        WhatsAppNotificationResponse con el estado del envío
    """
    try:
        # Enviar la notificación usando el servicio
        result = send_whatsapp_notification(to_number=request.to_number, reason=request.reason)

        return WhatsAppNotificationResponse(
            status=result["status"],
            message=result["message"],
            message_id=result.get("message_id")
        )

    except Exception as e:
        return WhatsAppNotificationResponse(
            status="error",
            message=f"Error al enviar notificación de WhatsApp: {str(e)}"
        )
