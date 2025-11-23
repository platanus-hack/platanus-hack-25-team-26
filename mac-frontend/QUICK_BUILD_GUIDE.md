# Quick Build Guide

## ✅ Prerequisites Check

You have:
- ✅ Developer ID certificate installed (Pablo Villarroel - K4YA72P732)
- ✅ Environment variables configured in `.env`
- ✅ electron-builder installed and configured

## 🚀 Build Commands

### Production Build (Recommended)
```bash
npm run build
```
Creates a signed, notarized DMG for your Mac's architecture (~5-15 min)

### Universal Build (Intel + Apple Silicon)
```bash
npm run build:universal
```
Creates a universal DMG that works on all Macs (~10-30 min)

### Quick Test Build
```bash
npm run build:dir
```
Creates the app without DMG/notarization (~1-2 min)

## 📦 Build Output

After building, check `dist/` for:
- `Phishing AI Protection-1.0.0.dmg` - Ready to distribute!
- `Phishing AI Protection-1.0.0-mac.zip` - Alternative distribution format

## ⚠️ Before Building

**If you don't have an app icon**, edit `electron-builder.json` and remove:
```json
"icon": "assets/icon.icns",
```
From both the `mac` and `dmg` sections.

## 🧪 Test the Build

After building, verify notarization:
```bash
spctl -a -vvv -t install "dist/Phishing AI Protection-1.0.0.dmg"
```

Should show: `accepted` and `source=Notarized Developer ID`

## 💡 Common Issues

**Build fails with "No icon found"**
- Remove the `"icon"` lines from `electron-builder.json`

**Notarization times out**
- Wait longer (can take up to 15 minutes)
- Check your `.env` credentials are correct

**Build succeeds but app won't open**
- Run: `npm run build:dir` to test locally first
- Check Console.app for crash logs

## 📚 Full Documentation

See [BUILD.md](BUILD.md) for complete details.
