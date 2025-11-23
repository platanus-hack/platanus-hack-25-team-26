# Phishing AI Protection

Real-time phishing detection for macOS using AI-powered analysis.

## Quick Start

```bash
# Install dependencies
npm install

# Run in development mode
npm run dev
```

## Documentation

- **[SETUP.md](SETUP.md)** - Development setup and installation
- **[BUILD.md](BUILD.md)** - Production build instructions
- **[SECURITY_CHECKLIST.md](SECURITY_CHECKLIST.md)** - Security guidelines

## Building for Production

**Important:** Copy `.env.example` to `.env` and add your Apple Developer credentials before building.

```bash
npm run build
```

See [BUILD.md](BUILD.md) for detailed instructions.

## Security

⚠️ **Never commit `.env`** - It contains your Apple Developer credentials!

See [SECURITY_CHECKLIST.md](SECURITY_CHECKLIST.md) for details.
