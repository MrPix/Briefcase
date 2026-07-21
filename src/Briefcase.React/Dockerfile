# ── Build the React (Vite) SPA ───────────────────────────────────────────────
FROM node:24-alpine AS build
ARG VERSION=0.0.0
ARG BUILD_NUMBER=0
WORKDIR /app

# Install dependencies first for better layer caching.
COPY src/Briefcase.React/package.json src/Briefcase.React/package-lock.json ./
RUN npm ci

# Build the production bundle. VITE_API_BASE_URL is left empty so the app talks
# to the API same-origin through the Caddy reverse proxy (no CORS in prod).
COPY src/Briefcase.React/ ./
ENV VITE_APP_VERSION=$VERSION
RUN npm run build

# ── Serve static files with Caddy ────────────────────────────────────────────
FROM caddy:alpine AS final
COPY --from=build /app/dist /usr/share/caddy
COPY src/Briefcase.React/Caddyfile /etc/caddy/Caddyfile
EXPOSE 80
