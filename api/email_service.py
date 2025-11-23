import requests
from pathlib import Path
from typing import Optional


# Email configuration
SENDER_EMAIL = "onboarding@resend.dev"  # Puede que tu endpoint no lo necesite, pero lo dejo por si acaso
RECIPIENT_EMAIL = "esteban@damascuss.io"  # Default recipient

# API Endpoints
EMAIL_API_ENDPOINT = "https://api-notmeta.damascuss.io/notmeta/kora/email/"
WHATSAPP_API_ENDPOINT = "https://api-notmeta.damascuss.io/notmeta/kora/notify/"

def get_email_template(data: dict, logo_base64: Optional[str] = None) -> str:
    """
    Genera el template HTML para el email de alerta de ingeniería social (control parental).
    
    Args:
        data: Diccionario con los datos de la alerta
        logo_base64: Logo en base64 para embed en el HTML (opcional)
    """
    reason = data.get("reason", "")
    tipo = data.get("type", "")
    scoring = data.get("scoring", 5)

    # Siempre es ingeniería social
    risk_level = "INGENIERÍA SOCIAL DETECTADA"

    # Color basado en el scoring
    if scoring >= 7:
        risk_color = "#dc2626"  # Rojo - Peligro
    elif scoring >= 4:
        risk_color = "#f59e0b"  # Amarillo - Advertencia
    else:
        risk_color = "#10b981"  # Verde - Bajo riesgo

    # Leer el template HTML desde el archivo externo
    template_path = Path(__file__).parent / "email_template.html"
    with open(template_path, 'r', encoding='utf-8') as f:
        html_template = f.read()

    # Determinar la fuente de la imagen del logo
    if logo_base64:
        logo_src = f"data:image/png;base64,{logo_base64}"
    else:
        # Fallback a cid si no hay logo base64 (para compatibilidad)
        logo_src = "cid:logo"

    # Reemplazar los placeholders con los valores dinámicos
    html_content = html_template.format(
        risk_color=risk_color,
        risk_level=risk_level,
        tipo=tipo or 'desconocido',
        reason=reason if reason else 'No se proporcionó una razón específica'
    )

    # Reemplazar cid:logo con el data URI si tenemos el logo
    if logo_base64:
        html_content = html_content.replace('src="cid:logo"', f'src="{logo_src}"')

    return html_content


def send_phishing_alert(data: dict, recipient_email: str):
    """
    Envía un email de alerta cuando se detecta ingeniería social usando tu endpoint.
    Args:
        data: Diccionario con los datos devueltos por el API (scoring, reason, type)
        recipient_email: Email del destinatario que recibirá la alerta
    """
    try:
        # Generar contenido HTML del email
        html_content = get_email_template(data)

        # Preparar el payload para tu endpoint
        payload = {
            "html": html_content,
            "email": recipient_email,
            "subject": "⚠️ ALERTA: Ingeniería Social Detectada - Control Parental"
        }

        # Enviar POST request a tu endpoint
        response = requests.post(
            EMAIL_API_ENDPOINT,
            json=payload,
            timeout=10  # 10 segundos de timeout
        )

        # Verificar si la respuesta fue exitosa
        response.raise_for_status()

        return {
            "status": "success",
            "message_id": response.json().get('id') if response.json() else None,
            "message": f"Alert email sent successfully to {recipient_email}"
        }
    except requests.exceptions.Timeout:
        return {
            "status": "error",
            "message": "Request timeout: El servidor tardó demasiado en responder"
        }
    except requests.exceptions.RequestException as e:
        return {
            "status": "error",
            "message": f"Failed to send email via API: {str(e)}"
        }

    except Exception as e:
        return {
            "status": "error",
            "message": f"Failed to send email: {str(e)}"
        }


def send_whatsapp_notification(to_number: str, reason: str):
    """
    Envía una notificación de WhatsApp cuando se detecta ingeniería social.
    Args:
        to_number: Número de teléfono del destinatario (ej: "573001234567")
        reason: Razón de la alerta
    """
    try:
        # Preparar el payload para el endpoint de WhatsApp
        payload = {
            "to_number": to_number,
            "reason": reason
        }

        # Enviar POST request al endpoint de WhatsApp
        response = requests.post(
            WHATSAPP_API_ENDPOINT,
            json=payload,
            headers={
                "Content-Type": "application/json",
            },
            timeout=10  # 10 segundos de timeout
        )

        # Verificar si la respuesta fue exitosa
        response.raise_for_status()

        return {
            "status": "success",
            "message_id": response.json().get('id') if response.json() else None,
            "message": f"WhatsApp notification sent successfully to {to_number}"
        }
    except requests.exceptions.Timeout:
        return {
            "status": "error",
            "message": "Request timeout: El servidor tardó demasiado en responder"
        }
    except requests.exceptions.RequestException as e:
        return {
            "status": "error",
            "message": f"Failed to send WhatsApp notification via API: {str(e)}"
        }
    except Exception as e:
        return {
            "status": "error",
            "message": f"Failed to send WhatsApp notification: {str(e)}"
        }
