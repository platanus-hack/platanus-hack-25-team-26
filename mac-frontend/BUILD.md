# Building a Notarized Production Release

This guide explains how to create a notarized production build of Phishing AI Protection for macOS.

## Prerequisites

### 1. Apple Developer Account
You need an active Apple Developer Program membership ($99/year).

### 2. Developer Certificate
You need a valid "Developer ID Application" certificate installed in your macOS Keychain.

**To check if you have the certificate:**
```bash
security find-identity -v -p codesigning
```

You should see a line like:
```
1) XXXXXXXXXX "Developer ID Application: Your Name (TEAM_ID)"
```

**If you don't have it:**
1. Go to [Apple Developer Certificates](https://developer.apple.com/account/resources/certificates/list)
2. Click the "+" button
3. Select "Developer ID Application"
4. Follow the steps to create and download the certificate
5. Double-click the downloaded `.cer` file to install it in Keychain Access

### 3. App-Specific Password
You should already have this in your `.env` file. If not:
1. Go to [appleid.apple.com](https://appleid.apple.com)
2. Sign in with your Apple ID
3. Go to "Sign-In and Security" > "App-Specific Passwords"
4. Click "+" to generate a new password
5. Add it to `.env` as `APPLE_APP_SPECIFIC_PASSWORD`

### 4. Environment Variables
Your `.env` file should contain:
```bash
APPLE_ID=your.email@example.com
APPLE_APP_SPECIFIC_PASSWORD=xxxx-xxxx-xxxx-xxxx
APPLE_TEAM_ID=XXXXXXXXXX
```

**To find your Team ID:**
```bash
security find-certificate -c "Developer ID Application" -p | openssl x509 -text | grep "OU="
```

Or check your [Apple Developer Account](https://developer.apple.com/account) under "Membership Details".

## App Icon (Optional but Recommended)

The build configuration expects an icon at `assets/icon.icns`. If you don't have one:

**Option 1: Create a temporary icon**
```bash
mkdir -p assets
# Create a simple PNG (at least 1024x1024)
# Then convert to icns using online tools or:
# iconutil -c icns icon.iconset
```

**Option 2: Skip the icon**
Edit [electron-builder.json:20](electron-builder.json#L20) and remove the `"icon"` line.

## Building

### Build for your current architecture (faster)
```bash
npm run build
```

This creates a signed and notarized DMG for your Mac's architecture (Intel x64 or Apple Silicon arm64).

### Build Universal Binary (Intel + Apple Silicon)
```bash
npm run build:universal
```

This creates a single DMG that works on both Intel and Apple Silicon Macs. **This takes significantly longer** as it builds and signs both architectures.

### Build directory only (for testing, no DMG)
```bash
npm run build:dir
```

This creates the `.app` bundle without packaging it into a DMG. Useful for testing the build without waiting for notarization.

## Build Process

When you run the build command, here's what happens:

1. **Packaging** - electron-builder packages your app with all dependencies
2. **Code Signing** - The app is signed with your Developer ID certificate
3. **Notarization** - The signed app is uploaded to Apple for notarization
   - This can take 2-15 minutes depending on Apple's servers
   - You'll see progress in the terminal
4. **Stapling** - The notarization ticket is stapled to the app
5. **DMG Creation** - A disk image is created with the notarized app

The final DMG will be in the `dist/` directory.

## Build Output

After a successful build, you'll find in `dist/`:
- `Phishing AI Protection-1.0.0.dmg` - The installable DMG
- `Phishing AI Protection-1.0.0-mac.zip` - Zip archive of the app
- `mac/` - The built .app bundle

## Troubleshooting

### "No identity found"
You don't have the Developer ID certificate installed. See Prerequisites section.

### "Notarization failed"
Check that your `.env` file has the correct Apple ID, app-specific password, and Team ID.

### "Screen recording permission not working"
The entitlements are already configured. Users will need to grant permission in:
**System Settings > Privacy & Security > Screen Recording**

### Build is very slow
- Use `npm run build` instead of `npm run build:universal` if you only need one architecture
- Notarization can take 5-15 minutes - this is normal
- Subsequent builds are faster (cached dependencies)

### Testing the notarization
After building, test that the DMG is properly notarized:
```bash
spctl -a -vvv -t install "dist/Phishing AI Protection-1.0.0.dmg"
```

Should output: `accepted` and `source=Notarized Developer ID`

## Distribution

Once built, you can:
1. **Direct Distribution**: Upload the DMG to your website or GitHub releases
2. **Email/Cloud**: Share via email, Dropbox, Google Drive, etc.

Users can simply download and install the DMG - no warnings from macOS Gatekeeper!

## Version Updates

To update the version number:
1. Edit `version` in [package.json:3](package.json#L3)
2. Rebuild: `npm run build`

The version appears in:
- DMG filename
- App bundle version
- About dialog (if you add one)

## Resources

- [electron-builder Documentation](https://www.electron.build/)
- [Apple Notarization Guide](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)
- [Electron Code Signing](https://www.electron.build/code-signing)
