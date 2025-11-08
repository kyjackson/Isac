# ISAC Solution Architecture & Implementation Plan

This document describes the architecture, responsibilities, relationships, and implementation tasks for all projects in the **ISAC** solution.

It is written to guide an AI coding assistant (e.g. GitHub Copilot / Copilot Agent) to work consistently across the entire codebase.

---

## Global Principles

1. **Primary Language & Stack**
   - All application logic is written in **C#**.
   - Use **.NET 10** (preview) where applicable.
   - Use **.NET for Android** for Wear OS projects.
   - Use **.NET MAUI** for the mobile companion app.
   - Use **ASP.NET Core** for the backend API.

2. **Supported Platforms**
   - Galaxy Watch6 Classic (Wear OS).
   - Android phone (for companion app).
   - Backend hosted environment (Kestrel + reverse proxy / cloud app service).

3. **Design Constraints**
   - No significant Kotlin/Java code.
   - Do not require Android Studio for normal development.
   - Prefer shared libraries and clear separation of concerns.
   - Optimize for maintainability, testability, and minimal coupling.

4. **Security & Privacy (High Level)**
   - All communication with backend over HTTPS.
   - All user-specific data and voice profiles must be scoped per user.
   - Provide mechanisms to delete voice data and profiles.

5. **Core Concept**
   - ISAC is a voice-driven assistant:
     - User speaks into the watch.
     - Audio is sent to backend.
     - Backend performs ASR → LLM → TTS.
     - Response audio is returned to the watch (ideally using a voice cloned from the user’s own voice).
   - A custom watch face presents an ISAC-style HUD and deep-links into the ISAC app.

---

## Project Overview

Solution projects:

1. `Isac.Core.Shared`
2. `Isac.Core.Api`
3. `Isac.Wear`
4. `Isac.Watchface`
5. `Isac.Mobile`
6. `Isac.Tests`

### Project Dependency Graph

- `Isac.Core.Shared`
  - No project dependencies.
- `Isac.Core.Api`
  - Depends on: `Isac.Core.Shared`
- `Isac.Wear`
  - Depends on: `Isac.Core.Shared`
- `Isac.Watchface`
  - Depends on: `Isac.Core.Shared`
- `Isac.Mobile`
  - Depends on: `Isac.Core.Shared`
- `Isac.Tests`
  - Depends on: `Isac.Core.Shared`, `Isac.Core.Api`

No project should reference another app project (e.g. `Isac.Wear` must not reference `Isac.Mobile`).

---

## 1. Isac.Core.Shared

**Type:** .NET 10 Class Library  
**Purpose:** Shared contracts, clients, configuration primitives, and utilities.

### Responsibilities

- Define request/response DTOs used across projects.
- Provide a typed HTTP client for communicating with `Isac.Core.Api`.
- Centralize configuration keys, route names, and constants.
- Provide shared abstractions that are platform-agnostic.

### Key Components (to implement)

- `Isac.Core.Shared.Configuration`
  - `IsacApiOptions`
    - `BaseUrl` (string)
- `Isac.Core.Shared.Transport`
  - DTOs:
    - `QueryRequest`
      - `UserId` (string)
      - `DeviceId` (string)
      - `AudioFormat` (string, e.g. `"audio/wav"`)
      - `Payload` (byte[] or stream in specific usages)
    - `QueryResponse`
      - `AudioFormat` (string)
      - `AudioBytes` (byte[])
      - (Optionally) `Text` (string, for debugging)
    - `VoiceEnrollRequest`
      - `UserId` (string)
      - `Clips` (collection of `VoiceSampleDescriptor`)
    - `VoiceEnrollResponse`
      - `VoiceProfileId` (string)
    - `VoiceDeleteRequest`
      - `UserId` (string)
- `Isac.Core.Shared.Client`
  - `IIsacClient`
    - `Task<QueryResponse> SendQueryAsync(QueryRequest request, CancellationToken ct = default);`
    - `Task<VoiceEnrollResponse> EnrollVoiceAsync(VoiceEnrollRequest request, CancellationToken ct = default);`
    - `Task DeleteVoiceAsync(string userId, CancellationToken ct = default);`
    - `Task<bool> PingAsync(CancellationToken ct = default);`
  - `IsacHttpClient` (default implementation)
    - Uses `HttpClient` (injected or provided).
    - Serializes/deserializes DTOs using `System.Text.Json`.
    - Handles base URL, error handling, and basic retries.

### Implementation Notes

- All code must be platform neutral (no direct Android/iOS APIs).
- Provide extension methods to register `IIsacClient` in DI containers.

---

## 2. Isac.Core.Api

**Type:** ASP.NET Core Web API (.NET 10)  
**Purpose:** Central “ISAC Core” backend.

### Responsibilities

- Expose HTTP endpoints for:
  - Health/ping.
  - Handling voice queries.
  - Voice enrollment and deletion.
- Integrate with:
  - Automatic Speech Recognition (ASR).
  - Large Language Model (LLM).
  - Text-to-Speech (TTS) with voice cloning.
- Enforce per-user isolation and basic authentication (to be implemented).

### Required Endpoints (initial)

1. `GET /api/v1/ping`
   - Returns simple status JSON.
2. `POST /api/v1/query`
   - Accepts audio input.
   - For initial implementation:
     - Accept input as `multipart/form-data` with:
       - `userId`
       - `deviceId`
       - `audio` (file)
     - Returns:
       - For MVP: static or generated WAV as placeholder.
3. `POST /api/v1/voice/enroll`
   - Accepts multiple audio samples.
   - MVP: store metadata only, return dummy `VoiceProfileId`.
4. `DELETE /api/v1/voice`
   - Removes stored voice profile and related data (MVP can be a stub).

### Implementation Tasks

- [ ] Create minimal hosting setup (`Program.cs`) using top-level statements.
- [ ] Register controllers or minimal APIs for the routes above.
- [ ] Use DTOs from `Isac.Core.Shared`.
- [ ] Implement a simple in-memory store for:
  - Registered users.
  - Voice profiles (stub implementation).
- [ ] Implement `Ping` endpoint to be used by clients for connectivity checks.
- [ ] Add structured logging for all API calls.
- [ ] Ensure CORS configuration is suitable for mobile/watch clients.

### Future Tasks (placeholders)

- [ ] Integrate real ASR.
- [ ] Integrate real LLM.
- [ ] Integrate real TTS with voice cloning.
- [ ] Add authentication/authorization (e.g. tokens per user/device).
- [ ] Persist data in a database instead of in-memory.

---

## 3. Isac.Wear

**Type:** .NET for Android Wear OS Application  
**Purpose:** Main interactive ISAC app on the watch.

### Responsibilities

- Provide a simple UI to:
  - Start and stop voice capture.
  - Send recorded audio to `Isac.Core.Api`.
  - Play back response audio.
- Use `Isac.Core.Shared` for DTOs and API calls.
- Handle permissions (audio, network).
- Optimize for short, low-friction interactions.

### Required Features (MVP)

- [ ] Single main `Activity` (e.g. `MainActivity`).
- [ ] Request `RECORD_AUDIO` permission at runtime.
- [ ] Large central control:
  - Press-and-hold: start recording.
  - Release: stop recording, send to backend.
- [ ] Record audio using `AudioRecord`:
  - Mono, 16kHz or 24kHz, PCM 16-bit.
- [ ] Package audio and send to `/api/v1/query` via `IsacHttpClient`.
- [ ] Receive reply `AudioBytes` and play using `MediaPlayer` or `AudioTrack`.
- [ ] Display minimal status UI:
  - “Listening…”
  - “Sending…”
  - “Playing response…”
  - Error messages for connectivity issues.

### Implementation Notes

- Use only C# and .NET for Android bindings for audio and networking APIs.
- No heavy logic or model inference on-device; delegate to backend.
- Use configuration (e.g. embedded settings or simple local storage) for API base URL.
- Design with Wear OS screen constraints in mind (circular UI).

---

## 4. Isac.Watchface

**Type:** .NET for Android Wear OS Application (Watch Face)  
**Purpose:** Custom ISAC-style watch face that integrates with `Isac.Wear`.

### Responsibilities

- Render a watch face visually inspired by ISAC HUD:
  - Time, date, and status indicators.
- Support tap actions:
  - On a defined region (e.g. center ring/logo), launch `Isac.Wear` directly.
- Optionally display complication data in the future.

### Required Features (MVP)

- [ ] Implement a watch face using `CanvasWatchFaceService` (or equivalent) in C#.
- [ ] Draw a simple digital face:
  - Time (hours/minutes, 24h or 12h).
  - Date.
  - One ISAC indicator element.
- [ ] Handle tap events:
  - On tap in the designated region:
    - Start `Isac.Wear.MainActivity` with an intent.
- [ ] Support ambient mode (reduced rendering).
- [ ] Ensure battery-efficient drawing; no continuous heavy work.

### Implementation Notes

- The watch face must not execute heavy network or LLM logic.
- It may read lightweight state (later) via shared preferences or simple APIs if needed.
- Reuse shared constants/theme from `Isac.Core.Shared` where appropriate.

---

## 5. Isac.Mobile

**Type:** .NET MAUI App  
**Purpose:** Companion/control app for phone and desktop platforms.

### Responsibilities

- Manage user configuration:
  - API base URL.
  - User identity / auth tokens (once implemented).
- Handle voice enrollment flows:
  - Guide the user through recording training samples.
  - Upload enrollment samples to `Isac.Core.Api`.
- Provide debugging/advanced controls:
  - Connectivity tests to backend.
  - View logs or recent interactions (future).

### Required Features (MVP)

- [ ] MAUI Shell-based app structure with a simple navigation:
  - Home / Status page.
  - Voice Enrollment page.
  - Settings page.
- [ ] Integrate `IsacHttpClient` from `Isac.Core.Shared`.
- [ ] Implement “Test Connection” button:
  - Calls `/api/v1/ping` and shows result.
- [ ] Implement basic voice enrollment UI:
  - Record multiple clips using MAUI/Android APIs.
  - Display progress and send to `/api/v1/voice/enroll`.

### Implementation Notes

- Keep UI simple and functional.
- All network calls should use the same DTOs and client as other projects.
- No direct dependency on watch projects.

---

## 6. Isac.Tests

**Type:** Test Project (e.g. xUnit)  
**Purpose:** Automated tests for shared logic and backend.

### Responsibilities

- Validate DTO serialization/deserialization.
- Validate `IsacHttpClient` behavior against a test server or mocked handlers.
- Validate `Isac.Core.Api` endpoints using in-memory test host.

### Required Tests (Initial)

- [ ] `PingEndpoint_ReturnsSuccess`.
- [ ] `QueryEndpoint_ReturnsAudio_ForValidInput` (using stubbed implementation).
- [ ] `EnrollVoice_StoresProfileStub`.
- [ ] Serialization tests for all DTOs in `Isac.Core.Shared`.
- [ ] Basic error handling tests for `IsacHttpClient`.

---

## Development Order (Recommended)

1. **Backend and Shared**
   - Implement `Isac.Core.Shared` DTOs and `IsacHttpClient`.
   - Implement `Isac.Core.Api` with `/api/v1/ping` and a stub `/api/v1/query`.
   - Add basic tests in `Isac.Tests`.

2. **Wear App (Isac.Wear)**
   - Implement press-to-record, send to `/api/v1/query`, and play static/stub response.
   - Confirm end-to-end flow: watch → API → watch.

3. **Watch Face (Isac.Watchface)**
   - Implement basic watch face drawing.
   - Implement tap-to-launch `Isac.Wear`.

4. **Mobile App (Isac.Mobile)**
   - Implement settings and ping test.
   - Implement basic voice enrollment UI wired to `/api/v1/voice/enroll`.

5. **Iterative Enhancements**
   - Replace stub responses with real ASR + LLM + TTS integrations.
   - Add authentication, persistence, and richer UX.

---

## Coding Style & Guidelines

- Use `async`/`await` for all I/O operations.
- Centralize HTTP configuration in `Isac.Core.Shared`.
- Avoid duplicating endpoint URLs or DTOs across projects.
- Fail fast and log meaningful errors in `Isac.Core.Api`.
- Keep platform-specific code isolated to:
  - `Isac.Wear` (Wear OS specifics)
  - `Isac.Watchface` (watch face rendering)
  - `Isac.Mobile` (MAUI specifics)

---

This document should be used by automated coding assistants and human contributors to implement and maintain a consistent, well-structured ISAC system across all projects in the solution.
