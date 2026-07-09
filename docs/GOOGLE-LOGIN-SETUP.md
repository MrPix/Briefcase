# Configure Login with Google

This guide explains how to enable **Sign in with Google** (Google OAuth 2.0 / OIDC) for the
Briefcase API. Once configured, users can authenticate with their Google account from the
Web, MAUI, and desktop clients — no password required.

The API already ships with Google's endpoints pre-filled in
[src/Briefcase.ApiService/appsettings.json](../src/Briefcase.ApiService/appsettings.json).
You only need to supply a **Client ID** and **Client Secret** obtained from Google, plus register
the correct redirect URI.

---

## 1. How the flow works

Briefcase uses the OAuth 2.0 **Authorization Code flow with PKCE**:

1. The client opens `GET /api/auth/oauth/Google` on the API.
2. The API redirects the browser to Google's consent screen.
3. After the user approves, Google redirects back to the API callback:
   `GET /api/auth/oauth/Google/callback`.
4. The API exchanges the authorization code for tokens, reads the user's profile, creates or links
   the account, and issues Briefcase JWT access/refresh tokens.

The **redirect URI you must register with Google is the API callback**, not the client app:

```
https://<your-api-host>/api/auth/oauth/Google/callback
```

For local development with .NET Aspire this is typically:

```
https://localhost:7xxx/api/auth/oauth/Google/callback
```

> Use the actual HTTPS URL the API service listens on (check the Aspire dashboard or
> `launchSettings.json`). The path is always `/api/auth/oauth/Google/callback`.

---

## 2. Create Google OAuth credentials

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create (or select) a project.
3. Open **APIs & Services → OAuth consent screen**:
   - Choose **External** user type (unless you use Google Workspace internal).
   - Fill in the app name, support email, and developer contact.
   - Add the scopes `openid`, `email`, and `profile`.
   - While the app is in **Testing** mode, add your Google account under **Test users**.
4. Open **APIs & Services → Credentials → Create Credentials → OAuth client ID**:
   - **Application type:** *Web application*.
   - **Authorized redirect URIs:** add every API callback URL you use, for example:
     - `https://localhost:7xxx/api/auth/oauth/Google/callback` (local dev)
     - `https://your-domain.com/api/auth/oauth/Google/callback` (production)
5. Click **Create** and copy the generated **Client ID** and **Client Secret**.

---

## 3. Configure the API

The API reads Google settings from the `OAuth:Google` configuration section. The endpoints and
scopes already have sensible defaults, so you normally only set the `ClientId` and `ClientSecret`.

### Option A — User Secrets (recommended for local dev)

Never commit real credentials. Store them with the .NET Secret Manager:

```bash
cd src/Briefcase.ApiService
dotnet user-secrets init
dotnet user-secrets set "OAuth:Google:ClientId" "<your-client-id>"
dotnet user-secrets set "OAuth:Google:ClientSecret" "<your-client-secret>"
```

### Option B — Environment variables (recommended for containers/production)

Configuration keys map to environment variables by replacing `:` with `__`:

```bash
OAuth__Google__ClientId=<your-client-id>
OAuth__Google__ClientSecret=<your-client-secret>
```

In production, prefer a secret store (e.g. Azure Key Vault) over plain environment variables.

### Option C — appsettings (not for secrets)

You may fill non-secret defaults in
[src/Briefcase.ApiService/appsettings.json](../src/Briefcase.ApiService/appsettings.json),
but do **not** commit the `ClientSecret` there:

```json
{
  "OAuth": {
    "Google": {
      "ClientId": "",
      "ClientSecret": "",
      "AuthorizationEndpoint": "https://accounts.google.com/o/oauth2/v2/auth",
      "TokenEndpoint": "https://oauth2.googleapis.com/token",
      "UserInfoEndpoint": "https://www.googleapis.com/oauth2/v3/userinfo",
      "Scopes": "openid email profile"
    }
  }
}
```

---

## 4. Allow client redirect URIs (multi-app / cross-origin clients)

After a successful login the API can redirect back to a client app (e.g. the MAUI app or a Web app
hosted on a different origin) with the tokens in the URL fragment. Any client redirect URI whose
host differs from the API host must be allowlisted in `OAuth:AllowedClientRedirectUris`:

```json
{
  "OAuth": {
    "AllowedClientRedirectUris": [
      "https://your-web-app.com/oauth-callback",
      "briefcase://oauth-callback"
    ]
  }
}
```

Client redirect URIs that share the same host as the API are allowed automatically, so a single
co-hosted Web app usually needs no extra entries here.

---

## 5. Verify

1. Start the stack:
   ```bash
   dotnet run --project src/Briefcase.AppHost
   ```
2. Open the Web client and go to the **Sign In** page.
3. Click **Google**. You should be redirected to Google's consent screen.
4. After approving, you are returned to the app and signed in.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `Unsupported OAuth provider: Google` | Provider name misspelled in the request path | Use exactly `Google` (matched case-insensitively). |
| `OAuth:Google:ClientId is not configured.` | Client ID/Secret missing | Set `OAuth:Google:ClientId` and `OAuth:Google:ClientSecret`. |
| Google shows **redirect_uri_mismatch** | Registered redirect URI doesn't match the API callback | Register `https://<api-host>/api/auth/oauth/Google/callback` exactly (scheme, host, port, path). |
| `client_redirect_uri is not allowed.` | Cross-origin client redirect not allowlisted | Add it to `OAuth:AllowedClientRedirectUris`. |
| `access_denied` on consent screen | Account not a test user while app is in Testing | Add the account under **OAuth consent screen → Test users**, or publish the app. |
| `Failed to exchange authorization code.` | Wrong Client Secret or clock skew | Re-check the secret; ensure the server clock is correct. |

---

## Related

- OAuth service implementation: [src/Briefcase.ApiService/Services/OAuthService.cs](../src/Briefcase.ApiService/Services/OAuthService.cs)
- Auth endpoints: [src/Briefcase.ApiService/Controllers/AuthController.cs](../src/Briefcase.ApiService/Controllers/AuthController.cs)
- Architecture overview: [docs/ARCHITECTURE.md](ARCHITECTURE.md)
