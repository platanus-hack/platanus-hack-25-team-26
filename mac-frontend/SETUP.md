# Development Setup

## Prerequisites

- Node.js (v16 or later)
- macOS (this app is macOS-only)
- Apple Developer Account (for production builds)

## Installation

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd phishing-ai
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Set up environment variables** (for production builds only)

   Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

   Then edit `.env` and add your Apple Developer credentials:
   - `APPLE_ID` - Your Apple ID email
   - `APPLE_APP_SPECIFIC_PASSWORD` - Generate at [appleid.apple.com](https://appleid.apple.com)
   - `APPLE_TEAM_ID` - Found in your [Apple Developer account](https://developer.apple.com/account)

   **Note:** The `.env` file is gitignored and contains secrets. Never commit it!

## Development

```bash
# Run in development mode
npm run dev

# Run the backend server (for testing)
npm run backend
```

## Building for Production

**Prerequisites for building:**
- Valid "Developer ID Application" certificate installed in Keychain
- Environment variables configured in `.env`

```bash
# Build notarized production DMGs (x64 + arm64)
npm run build

# Build universal binary (single DMG for both architectures)
npm run build:universal

# Quick build without packaging (for testing)
npm run build:dir
```

See [BUILD.md](BUILD.md) for detailed build instructions.

## Project Structure

```
phishing-ai/
├── src/
│   ├── main.js              # Electron main process
│   ├── main/                # Main process modules
│   ├── preload/             # Preload scripts
│   └── renderer/            # Renderer process (overlay UI)
├── scripts/                 # Build scripts
├── entitlements.*.plist     # macOS entitlements
├── electron-builder.json    # Build configuration
├── .env.example             # Environment variables template
└── package.json
```

## Security Notes

**Never commit these files:**
- `.env` - Contains your Apple Developer credentials
- `dist/` - Build output

**Safe to commit:**
- `.env.example` - Template without actual credentials
- All other configuration files

## Permissions

This app requires the following macOS permissions:
- **Screen Recording** - To capture screenshots for phishing detection
- **Accessibility** (via `active-win`) - To detect the focused window

Users will be prompted to grant these permissions on first run.
