# Security Checklist ✅

## Files That Should NEVER Be Committed

- ✅ `.env` - **PROTECTED** (gitignored)
  - Contains your Apple ID credentials
  - Contains app-specific password
  - Contains Team ID

- ✅ `dist/` - **PROTECTED** (gitignored)
  - Build output
  - May contain signed binaries with your certificate

- ✅ `*.dmg`, `*.zip` - **PROTECTED** (gitignored)
  - Packaged applications

## Files That Should Be Committed

- ✅ `.env.example` - Template for environment variables (no secrets)
- ✅ `.gitignore` - Specifies what to exclude
- ✅ `electron-builder.json` - Build configuration (no secrets)
- ✅ `entitlements.*.plist` - App permissions
- ✅ `scripts/notarize.js` - Notarization script (reads from env vars)
- ✅ All documentation files

## Before Pushing to Git

Run this command to verify no secrets are being committed:

```bash
# Check what files are staged
git status

# Ensure .env is NOT in the list
# If it is, run: git reset .env
```

## If You Accidentally Commit .env

1. **Remove it from git history immediately:**
   ```bash
   git rm --cached .env
   git commit -m "Remove .env from tracking"
   git push
   ```

2. **Rotate your credentials:**
   - Generate a new app-specific password at [appleid.apple.com](https://appleid.apple.com)
   - Update your `.env` file with the new password

## Verification

Current status:
```bash
# .env is properly gitignored ✅
$ git check-ignore -v .env
.gitignore:11:.env	.env

# .env is not tracked ✅
$ git ls-files .env
(no output - good!)
```

## For Team Members

When setting up the project:

1. Clone the repository
2. Copy `.env.example` to `.env`
3. Fill in your own Apple Developer credentials
4. Never commit `.env`

Each team member should use their own Apple Developer credentials.
