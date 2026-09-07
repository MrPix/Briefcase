# Briefcase Mobile (React Native + Expo)

Cross-platform (Android / iOS) client for the Briefcase API, built with **Expo** and
**expo-router**. It talks to the same ASP.NET Core backend as the web app
(`Briefcase.React`) and the MAUI app.

## Feature scope (MVP)

- **Auth** — email/password, Google (system browser), and "add this device with a code".
- **Clipboard** — list, create text/URL notes, realtime sync via SignalR, copy / pin / delete.
- **Files** — upload from camera, photo library, or documents; image previews; download & share.

> E2EE messages created on other clients are shown as locked placeholders — decryption is a
> planned follow-up (needs a native crypto module for PBKDF2/AES-GCM parity).

## Prerequisites

- Node.js 20+ and npm.
- The Briefcase API running (via Aspire or `dotnet run` on `Briefcase.ApiService`).
- Android Studio emulator, Xcode simulator, or a physical device with **Expo Go** /
  a development build.

## Configure the API URL

React Native cannot use relative URLs, so the app needs an **absolute** API base URL.
Set it via an environment variable before starting Expo (defaults to `http://10.0.2.2:5218`):

| Target             | `EXPO_PUBLIC_API_BASE_URL`            |
| ------------------ | ------------------------------------- |
| Android emulator   | `http://10.0.2.2:<apiPort>`           |
| iOS simulator      | `http://localhost:<apiPort>`          |
| Physical device    | `http://<your-LAN-IP>:<apiPort>`      |

```powershell
$env:EXPO_PUBLIC_API_BASE_URL = "http://10.0.2.2:5218"
npm run start
```

Find `<apiPort>` from the API's launch profile / Aspire dashboard. During development the
API is plain HTTP; debug app builds and Expo Go permit cleartext to the LAN host. A release
build over HTTP would need `expo-build-properties` (Android) — production should use HTTPS.

## Run

```powershell
cd src/Briefcase.Mobile
npm install
npm run android   # or: npm run ios
```

## Google sign-in

The app opens the API's OAuth flow in the system browser and receives the tokens back via the
`briefcase://login` deep link. That redirect URI is allowlisted for development in
`Briefcase.ApiService/appsettings.Development.json` under `OAuth:AllowedClientRedirectUris`.

Custom-scheme deep links resolve most reliably in a **development build**
(`npx expo run:android` / `npx expo run:ios`). In Expo Go the redirect URI contains a dynamic
LAN address, so a dev build is recommended for testing Google login end to end. Email/password
and login-by-code work in Expo Go without extra setup.

## Project layout

```
app/                 expo-router routes
  _layout.tsx        root stack + providers (Auth, SafeArea, StatusBar)
  index.tsx          auth-based redirect
  login.tsx          email/password + Google + login-by-code
  signup.tsx         account creation
  (app)/_layout.tsx  auth guard + bottom tabs
  (app)/clipboard.tsx
  (app)/files.tsx
src/
  lib/               config, apiClient, media helpers
  auth/              AuthContext, tokenStorage (SecureStore), deviceInfo
  services/          messages, devices (login-code)
  realtime/          SignalR hub + message stream
  types/             API DTO types
  ui/                theme tokens + shared components
```
