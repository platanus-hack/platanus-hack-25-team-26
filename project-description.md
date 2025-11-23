# Kora 🛡️

**Protección inteligente para tus hijos en internet**

Kora es una aplicación para macOS y Windows, que monitorea en tiempo real la actividad en pantalla de tus hijos, detectando situaciones de riesgo como phishing, grooming y contenido inapropiado mediante inteligencia artificial.

## Demo

[![Kora Demo](https://img.youtube.com/vi/n5ttXELweg4/maxresdefault.jpg)](https://youtu.be/n5ttXELweg4)

[Ver demo en YouTube](https://youtu.be/n5ttXELweg4)

## Características

🔍 **Monitoreo en tiempo real** - Analiza continuamente lo que aparece en pantalla

🚨 **Alertas visuales** - Notificaciones instantáneas en el cursor cuando se detecta contenido sospechoso

📧 **Notificaciones a padres** - Alertas vía email o WhatsApp en casos de grooming u otras situaciones de alto riesgo

🤖 **Inteligencia artificial** - Detección automática de patrones de riesgo y contenido peligroso

## Inicio Rápido

```bash
# Instalar dependencias
npm install

# Ejecutar en modo desarrollo
npm run dev
```

## Compilar para Producción

**Importante:** Copia `.env.example` a `.env` y agrega tus credenciales de Apple Developer antes de compilar.

```bash
npm run build
```

## Seguridad

⚠️ **Nunca hagas commit de `.env`** - Contiene tus credenciales de Apple Developer.

Ver [SECURITY_CHECKLIST.md](SECURITY_CHECKLIST.md) para más detalles.

## Licencia

MIT

---

Desarrollado para el Platanus Hack 2025
