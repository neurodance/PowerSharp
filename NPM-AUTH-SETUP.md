# NPM Authentication Setup Guide

This guide helps you set up npm authentication for the PowerSharp project, especially after npm's recent security changes (September 2025).

## Background

npm recently strengthened security with these changes:
- **Classic tokens deprecated** - All legacy classic tokens are being revoked
- **Granular tokens required** - New tokens have scoped permissions and shorter lifetimes (7-90 days)
- **TOTP 2FA disabled** - New setups must use WebAuthn/passkeys

Reference: [npm Security Changes](https://github.blog/changelog/2025-09-29-strengthening-npm-security-important-changes-to-authentication-and-token-management/)

## Do You Need Authentication?

**For public packages ONLY:** No authentication needed! The script should work without any npm login.

**For private packages or publishing:** You need to authenticate.

## Setup Options

### Option 1: Interactive Login (Local Development)

Simple one-time login:

```powershell
npm login
```

Follow the prompts. This is the easiest for local development.

### Option 2: Granular Access Token (CI/CD & Automation)

For automated scripts and CI/CD pipelines:

1. **Generate a token:**
   - Go to [npmjs.com](https://www.npmjs.com/)
   - Settings → Access Tokens → Generate New Token
   - Choose "Granular Access Token"
   - Set appropriate permissions (read-only for installing, read-write for publishing)
   - Note: Maximum lifetime is 90 days

2. **Use the token:**

   **PowerShell:**
   ```powershell
   $env:NPM_TOKEN = "npm_yourtokenhere"
   npm config set //registry.npmjs.org/:_authToken $env:NPM_TOKEN
   ```

   **Linux/Mac:**
   ```bash
   export NPM_TOKEN="npm_yourtokenhere"
   npm config set //registry.npmjs.org/:_authToken $NPM_TOKEN
   ```

3. **Or create `.npmrc` file:**
   
   In your home directory or project root:
   ```
   //registry.npmjs.org/:_authToken=${NPM_TOKEN}
   ```

### Option 3: Trusted Publishers (Best for GitHub Actions)

For GitHub Actions workflows, use OIDC-based authentication (no tokens needed):

```yaml
- name: Setup Node
  uses: actions/setup-node@v4
  with:
    node-version: '18'
    registry-url: 'https://registry.npmjs.org'
```

See: [npm Trusted Publishers](https://docs.npmjs.com/trusted-publishers)

## Troubleshooting

### Error: "npm ERR! code E401" or "npm ERR! need auth"

**Solution:** Run `npm login` or set up a granular access token.

### Script Hangs Waiting for Input

**Cause:** npm is prompting for authentication interactively.

**Solution:** 
1. Run `npm login` before running the script
2. Or set up a token as shown above
3. The updated script now uses `--no-audit` flag to avoid some auth prompts

### Token Expired

**Cause:** Granular tokens expire (max 90 days).

**Solution:** Generate a new token and update your configuration.

## Updated Script Changes

The `CREATE-DESIGNER-PROJECT.ps1` script now includes:

1. **Non-interactive mode** - Sets environment variables to avoid prompts
2. **Auth check** - Warns if npm is not authenticated (but continues for public packages)
3. **Better error messages** - Provides guidance when authentication is needed
4. **Additional flags** - Uses `--no-audit` and `--legacy-peer-deps` to minimize auth requirements

## Commands Reference

```powershell
# Check if you're logged in
npm whoami

# View current npm configuration
npm config list

# Check npm registry
npm config get registry

# Login interactively
npm login

# Logout
npm logout

# Set token from environment variable
npm config set //registry.npmjs.org/:_authToken $env:NPM_TOKEN

# Clear auth token
npm config delete //registry.npmjs.org/:_authToken
```

## For This Project

The Adaptive Cards designer uses **public npm packages**, so authentication should NOT be required. If you're seeing authentication prompts:

1. Make sure you're using the updated script version
2. Check if there are any private packages in `package.json`
3. Try clearing npm cache: `npm cache clean --force`
4. Verify your npm registry: `npm config get registry` (should be https://registry.npmjs.org/)

## Additional Resources

- [npm Docs: Creating and viewing access tokens](https://docs.npmjs.com/creating-and-viewing-access-tokens)
- [npm Docs: Using private packages](https://docs.npmjs.com/using-private-packages-in-a-ci-cd-workflow)
- [GitHub Blog: npm Security Changes](https://github.blog/changelog/2025-09-29-strengthening-npm-security-important-changes-to-authentication-and-token-management/)
