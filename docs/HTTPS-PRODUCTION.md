# HTTPS in Production

This document covers TLS certificate options for the Briefcase stack (ASP.NET Core 10 API + Blazor WASM frontend, containerised, Linux-compatible).

---

## 1. Choosing a Certificate Provider

| Option | Cost | Auto-renewal | Wildcard | Best for |
|---|---|---|---|---|
| **Let's Encrypt** | Free | Yes (ACME) | Yes (DNS-01) | Self-hosted / VPS |
| **ZeroSSL** | Free tier | Yes (ACME) | Yes (DNS-01) | Alternative to LE |
| **Azure App Service Managed Cert** | Free | Yes | No | Azure App Service only |
| **Azure Front Door / CDN** | Paid tier | Yes | Yes | Global CDN + TLS offload |
| **AWS ACM** | Free | Yes | Yes | AWS deployments |
| **Paid CAs (DigiCert, Sectigo)** | Paid | Manual/API | Yes | EV certs, compliance |

**Recommended for this project:**

| Deployment target | Recommendation |
|---|---|
| Self-hosted VPS / bare metal (Linux) | **Caddy** (automatic Let's Encrypt) |
| Docker Compose on VPS | **Traefik** or **Caddy** (auto HTTPS) |
| Azure App Service | **Azure managed certificates** |
| Azure Container Apps | **Azure managed certificates** |
| Kubernetes | **cert-manager** + Let's Encrypt |

> **Short answer:** For a self-hosted Linux server, use **Caddy** as a reverse proxy.  
> It handles certificate issuance, renewal, and HTTPS redirect with zero configuration.

---

## 2. Caddy (Recommended for Self-Hosted)

Caddy is a modern web server written in Go. It obtains and renews Let's Encrypt certificates automatically using the ACME protocol.

### 2.1 Prerequisites

- A public domain name pointing at the server's IP (A/AAAA record)
- Ports **80** and **443** open in the firewall
- Docker and Docker Compose installed

### 2.2 Project layout

```
/srv/Briefcase/
├── docker-compose.yml
├── Caddyfile
└── caddy-data/          ← persisted certificate store (mount as volume)
```

### 2.3 `Caddyfile`

```caddyfile
# Replace example.com with your real domain.
# Caddy fetches a Let's Encrypt certificate automatically on first start.

api.example.com {
    reverse_proxy apiservice:8080
}

app.example.com {
    reverse_proxy webfrontend:8080
}
```

Caddy will:
1. Obtain a certificate from Let's Encrypt via HTTP-01 challenge on port 80
2. Serve HTTPS on port 443
3. Redirect all HTTP traffic to HTTPS automatically
4. Renew the certificate ~30 days before expiry

### 2.4 `docker-compose.yml`

```yaml
services:
  caddy:
    image: caddy:2-alpine
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
      - "443:443/udp"   # HTTP/3 (QUIC)
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile:ro
      - ./caddy-data:/data          # persists certificates — back this up!
      - ./caddy-config:/config
    networks:
      - internal

  apiservice:
    image: Briefcase/apiservice:latest
    environment:
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
    networks:
      - internal

  webfrontend:
    image: Briefcase/webfrontend:latest
    environment:
      ASPNETCORE_URLS: http://+:8080
    networks:
      - internal

networks:
  internal:
    driver: bridge
```

> **Important:** Set `ASPNETCORE_URLS` to `http://` (not `https://`) because TLS terminates at Caddy,  
> not at the ASP.NET Core process. Add `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so  
> `HttpContext.Request.Scheme` returns `https` and the API generates correct absolute URLs.

### 2.5 ASP.NET Core forwarded-headers configuration

Because TLS terminates at Caddy, ASP.NET Core receives plain HTTP. Add this to `Program.cs` to trust the `X-Forwarded-*` headers that Caddy sets:

```csharp
// Program.cs — production configuration
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

Or simply set the environment variable in the compose file (shown above). For reverse proxies on the same machine, both approaches are equivalent.

### 2.6 Wildcard certificate (optional)

If you want a single `*.example.com` certificate instead of one per subdomain, use the DNS-01 challenge. Caddy has first-party DNS provider modules:

```caddyfile
{
    acme_dns cloudflare {env.CF_API_TOKEN}
}

*.example.com {
    tls {
        dns cloudflare {env.CF_API_TOKEN}
    }
    @api host api.example.com
    handle @api {
        reverse_proxy apiservice:8080
    }
    @app host app.example.com
    handle @app {
        reverse_proxy webfrontend:8080
    }
}
```

Build or pull the image with the DNS module:

```bash
docker pull caddy:2-builder AS builder
# Or use the official xcaddy image with your DNS provider:
# docker pull ghcr.io/caddy/cloudflare:latest
```

---

## 3. Traefik (Docker-native alternative)

Traefik reads Docker labels to discover services and provisions certificates automatically.

### 3.1 `docker-compose.yml`

```yaml
services:
  traefik:
    image: traefik:v3
    restart: unless-stopped
    command:
      - "--providers.docker=true"
      - "--providers.docker.exposedbydefault=false"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--certificatesresolvers.le.acme.httpchallenge=true"
      - "--certificatesresolvers.le.acme.httpchallenge.entrypoint=web"
      - "--certificatesresolvers.le.acme.email=admin@example.com"
      - "--certificatesresolvers.le.acme.storage=/letsencrypt/acme.json"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./letsencrypt:/letsencrypt   # persists certificates
    networks:
      - internal

  apiservice:
    image: Briefcase/apiservice:latest
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.api.rule=Host(`api.example.com`)"
      - "traefik.http.routers.api.entrypoints=websecure"
      - "traefik.http.routers.api.tls.certresolver=le"
      - "traefik.http.services.api.loadbalancer.server.port=8080"
      # Redirect HTTP → HTTPS
      - "traefik.http.routers.api-http.rule=Host(`api.example.com`)"
      - "traefik.http.routers.api-http.entrypoints=web"
      - "traefik.http.routers.api-http.middlewares=redirect-to-https"
      - "traefik.http.middlewares.redirect-to-https.redirectscheme.scheme=https"
    environment:
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_FORWARDEDHEADERS_ENABLED: "true"
    networks:
      - internal

networks:
  internal:
    driver: bridge
```

> Set `acme.json` permissions to `600` before first run:  
> `chmod 600 ./letsencrypt/acme.json`

---

## 4. Nginx + Certbot (Traditional)

Use this if you already run Nginx or need fine-grained control.

### 4.1 Install

```bash
# Debian / Ubuntu
sudo apt update
sudo apt install -y nginx certbot python3-certbot-nginx
```

### 4.2 Initial certificate issuance

```bash
# Caddy and Traefik are zero-config, but Certbot needs a domain already resolving to the server.
sudo certbot --nginx -d api.example.com -d app.example.com \
  --non-interactive --agree-tos -m admin@example.com
```

Certbot will:
1. Spin up a temporary HTTP server on port 80
2. Complete the HTTP-01 ACME challenge with Let's Encrypt
3. Write the certificate to `/etc/letsencrypt/live/api.example.com/`
4. Modify the Nginx config to add the `ssl_certificate` directives

### 4.3 Nginx config (`/etc/nginx/sites-available/Briefcase`)

```nginx
upstream apiservice {
    server 127.0.0.1:5000;
}

upstream webfrontend {
    server 127.0.0.1:5001;
}

# Redirect HTTP → HTTPS
server {
    listen 80;
    server_name api.example.com app.example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name api.example.com;

    ssl_certificate     /etc/letsencrypt/live/api.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.example.com/privkey.pem;
    include             /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam         /etc/letsencrypt/ssl-dhparams.pem;

    # SignalR WebSocket support
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";

    # Forward real client IP and protocol
    proxy_set_header Host              $host;
    proxy_set_header X-Real-IP         $remote_addr;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;

    location / {
        proxy_pass http://apiservice;
    }
}

server {
    listen 443 ssl;
    server_name app.example.com;

    ssl_certificate     /etc/letsencrypt/live/api.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/api.example.com/privkey.pem;
    include             /etc/letsencrypt/options-ssl-nginx.conf;
    ssl_dhparam         /etc/letsencrypt/ssl-dhparams.pem;

    location / {
        proxy_pass http://webfrontend;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/Briefcase /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

### 4.4 Automatic renewal

Certbot installs a systemd timer automatically. Verify it:

```bash
sudo systemctl status certbot.timer
# Test the renewal process (dry run, no actual renewal):
sudo certbot renew --dry-run
```

---

## 5. LettuceEncrypt — ACME inside ASP.NET Core (No reverse proxy)

If you deploy the API directly without a reverse proxy, use `LettuceEncrypt` to obtain and manage certificates inside the process.

```bash
dotnet add package LettuceEncrypt
```

```csharp
// Program.cs
builder.Services.AddLettuceEncrypt(options =>
{
    options.AcceptTermsOfService = true;
    options.DomainNames = ["api.example.com"];
    options.EmailAddress = "admin@example.com";
});
```

Store the certificate persistently (required for container restarts):

```csharp
// Store in a directory (use a volume mount in containers)
builder.Services.AddLettuceEncrypt()
    .PersistDataToDirectory(new DirectoryInfo("/certs"), "YourCertificatePassword");
```

> **Caveat:** LettuceEncrypt requires ports 80 and 443 accessible on the host — no other process can listen on port 80 during the HTTP-01 challenge. A reverse proxy (Caddy/Nginx/Traefik) is generally easier to manage.

---

## 6. Cloud-Managed Certificates

### 6.1 Azure App Service

App Service issues free managed certificates for custom domains at no cost.

1. Add a custom domain: **App Service → Custom domains → Add custom domain**
2. Issue a certificate: **Certificates → Managed certificates → Add certificate**
3. Bind it: **Custom domains → Add binding** (select the managed cert)
4. Renewal is fully automatic

### 6.2 Azure Container Apps

Managed certificates are available for Container Apps:

```bash
az containerapp hostname add \
  --name Briefcase-api \
  --resource-group rg-Briefcase \
  --hostname api.example.com

az containerapp hostname bind \
  --name Briefcase-api \
  --resource-group rg-Briefcase \
  --hostname api.example.com \
  --environment Briefcase-env \
  --validation-method HTTP
```

The platform handles issuance and renewal automatically.

### 6.3 Azure Front Door (CDN + WAF + TLS)

Front Door manages certificates and adds a global CDN and WAF layer — useful when MAUI clients across different regions need low-latency API access:

```bash
az afd custom-domain create \
  --profile-name Briefcase-afd \
  --resource-group rg-Briefcase \
  --custom-domain-name api-domain \
  --host-name api.example.com \
  --minimum-tls-version TLS12 \
  --certificate-type ManagedCertificate
```

---

## 7. Let's Encrypt: How It Works

Understanding ACME (Automatic Certificate Management Environment) helps with debugging.

```
Client (Certbot/Caddy/Traefik)          Let's Encrypt CA
        │                                       │
        │── POST /acme/new-order ───────────────▶│
        │◀─ 201 order {authorizations} ─────────│
        │                                       │
        │── GET /acme/authz/{id} ───────────────▶│
        │◀─ 200 {challenges: [{type:"http-01"}]} │
        │                                       │
        │  Places token at:                     │
        │  http://yourdomain.com/.well-known/   │
        │         acme-challenge/{token}        │
        │                                       │
        │── POST /acme/chall/{id} (ready) ──────▶│
        │◀─ Let's Encrypt fetches the token ────│
        │◀─ 200 (valid) ────────────────────────│
        │                                       │
        │── POST /acme/finalize (CSR) ──────────▶│
        │◀─ 200 {certificate URL} ──────────────│
        │── GET /acme/cert/{id} ────────────────▶│
        │◀─ 200 fullchain.pem ──────────────────│
```

**Challenge types:**

| Type | How it works | Requires |
|---|---|---|
| `http-01` | LE fetches a token from `http://domain/.well-known/acme-challenge/` | Port 80 open |
| `dns-01` | LE checks a TXT record `_acme-challenge.domain` | DNS API access |
| `tls-alpn-01` | LE connects on port 443 via a special TLS handshake | Port 443 open |

**Rate limits (as of 2024):**

| Limit | Value |
|---|---|
| Certificates per registered domain / week | 50 |
| Duplicate certificates / week | 5 |
| Failed validation attempts / hour | 5 |
| Accounts per IP / 3 hours | 10 |

Use the **staging environment** during development to avoid hitting rate limits:

```bash
# Certbot staging
certbot --staging ...

# Caddy staging
{
    acme_ca https://acme-staging-v02.api.letsencrypt.org/directory
}
```

---

## 8. SignalR-Specific HTTPS Requirements

SignalR uses WebSocket (upgraded from HTTP). Ensure the proxy passes `Upgrade` headers:

**Caddy** handles this automatically.

**Nginx** — add to the `location /` block in the API server:

```nginx
proxy_http_version 1.1;
proxy_set_header Upgrade $http_upgrade;
proxy_set_header Connection $connection_upgrade;

map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}
```

**Traefik** — no extra configuration needed; WebSocket is supported by default.

---

## 9. TLS Security Hardening

Once certificates are in place, harden the TLS configuration:

### Minimum TLS version

**Caddy** — TLS 1.2+ is the default; TLS 1.0/1.1 are disabled by default.

**Nginx:**
```nginx
ssl_protocols TLSv1.2 TLSv1.3;
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:DHE-RSA-AES128-GCM-SHA256;
ssl_prefer_server_ciphers off;
```

### HSTS (HTTP Strict Transport Security)

Instruct browsers and MAUI WebViews to only connect over HTTPS:

**Caddy** (automatic, added in response headers):
```caddyfile
header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload"
```

**ASP.NET Core:**
```csharp
app.UseHsts(); // only call in production, not dev
```

Set in `appsettings.json` for fine-grained control:
```json
{
  "HttpsRedirection": {
    "HttpsPort": 443
  }
}
```

### OCSP Stapling

Reduces certificate validation latency for clients:

**Nginx:**
```nginx
ssl_stapling on;
ssl_stapling_verify on;
resolver 1.1.1.1 8.8.8.8 valid=300s;
```

**Caddy** — enabled automatically.

---

## 10. Verification Checklist

After deployment, verify the certificate configuration:

```bash
# Check certificate validity and chain
curl -vI https://api.example.com 2>&1 | grep -E "SSL|certificate|expire"

# Test TLS version and cipher suites (install testssl.sh or use online)
docker run --rm drwetter/testssl.sh api.example.com

# Check HTTPS redirect
curl -I http://api.example.com
# Expected: HTTP/1.1 301 Moved Permanently + Location: https://...

# Check HSTS header
curl -sI https://api.example.com | grep -i strict
# Expected: strict-transport-security: max-age=...

# Test WebSocket upgrade (SignalR endpoint)
wscat -c wss://api.example.com/messagehub
```

Online tools:
- https://www.ssllabs.com/ssltest/ — grade your TLS config (aim for A or A+)
- https://hstspreload.org/ — check / submit HSTS preload

---

## 11. Certificate Renewal

**Let's Encrypt certificates expire after 90 days.** All tools above renew automatically, but verify:

| Tool | Renewal mechanism | Verify |
|---|---|---|
| Caddy | Background goroutine, renews at 2/3 of lifetime | `journalctl -u caddy` |
| Traefik | Built-in ACME client, checks daily | `docker logs traefik` |
| Certbot | systemd timer `certbot.timer` | `systemctl status certbot.timer` |
| LettuceEncrypt | Background .NET hosted service | App logs |
| Azure managed | Automatic, no action required | Azure Portal |

**Always persist certificate storage outside containers:**

```yaml
# docker-compose.yml
volumes:
  - ./caddy-data:/data       # Caddy
  - ./letsencrypt:/letsencrypt  # Traefik / Certbot
  - ./certs:/certs           # LettuceEncrypt
```

---

## 12. Quick Decision Guide

```
Are you self-hosting on a Linux VPS or bare metal?
│
├── Yes
│   ├── Using Docker Compose? → Caddy (Caddyfile, section 2) ← simplest
│   ├── Heavily Docker-label driven? → Traefik (section 3)
│   └── Already have Nginx? → Nginx + Certbot (section 4)
│
└── No (cloud)
    ├── Azure App Service → Managed certificates (section 6.1)
    ├── Azure Container Apps → Managed certificates (section 6.2)
    ├── Azure + CDN/WAF needed → Azure Front Door (section 6.3)
    └── Kubernetes → cert-manager + Let's Encrypt ClusterIssuer
```
