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

Status:
- [x] `IsacApiOptions` (`BaseUrl`)
- [x] DTOs: `QueryRequest`, `QueryResponse`, `VoiceEnrollRequest`, `VoiceEnrollResponse`, `VoiceDeleteRequest`, `VoiceSampleDescriptor`
- [x] Interface `IIsacClient`
- [x] Implementation `IsacHttpClient`
- [x] DI extension `AddIsacClient`
- [ ] Additional shared constants / route name centralization (not yet added)

### Implementation Notes

- All code must be platform neutral (no direct Android/iOS APIs).
- Provide extension methods to register `IIsacClient` in DI containers. (Implemented)

---

## 2. Isac.Core.Api

**Type:** ASP.NET Core Web API (.NET 10)  
**Purpose:** Central “ISAC Core” backend.

### Required Endpoints (initial)

1. `GET /api/v1/ping` (Implemented) ✅
2. `POST /api/v1/query` (Stub implemented: returns silence WAV) ✅
3. `POST /api/v1/voice/enroll` (Stub implemented) ✅
4. `DELETE /api/v1/voice` (Stub implemented) ✅

### Implementation Tasks

- [x] Create minimal hosting setup (`Program.cs`) using top-level statements.
- [x] Register minimal APIs for the routes above. (Controllers not used.)
- [x] Use DTOs from `Isac.Core.Shared`.
- [ ] Implement a simple in-memory store for:
  - Registered users (NOT DONE)
  - Voice profiles (DONE) → partially complete overall.
- [x] Implement `Ping` endpoint to be used by clients for connectivity checks.
- [ ] Add structured logging for all API calls (basic logging service added, per-endpoint structured logging still TODO).
- [x] Ensure CORS configuration is suitable for mobile/watch clients ( permissive policy added ).

### Future Tasks (placeholders)

- [ ] Integrate real ASR.
- [ ] Integrate real LLM.
- [ ] Integrate real TTS with voice cloning.
- [ ] Add authentication/authorization (e.g. tokens per user/device).
- [ ] Persist data in a database instead of in-memory.

---

## 3. Isac.Wear

(MVP not yet implemented)

### Required Features (MVP) Status
- [ ] Main `Activity` with UI logic.
- [ ] Runtime `RECORD_AUDIO` permission handling.
- [ ] Press-and-hold recording interaction.
- [ ] Audio capture via `AudioRecord` (16k/24kHz mono PCM16).
- [ ] Send audio to `/api/v1/query` via `IsacHttpClient`.
- [ ] Play response audio.
- [ ] Status UI states.

---

## 4. Isac.Watchface

(MVP not yet implemented)

### Required Features (MVP) Status
- [ ] Watch face using `CanvasWatchFaceService` (or equivalent binding).
- [ ] Draw time/date + indicator.
- [ ] Tap region launches `Isac.Wear.MainActivity`.
- [ ] Ambient mode support.
- [ ] Battery-efficient rendering.

---

## 5. Isac.Mobile

(MVP implemented to basic level)

### Required Features (MVP) Status
- [x] Shell navigation (Home / Status, Enrollment, Settings).
- [x] Integrate `IsacHttpClient` (basic usage in pages; runtime BaseUrl from Settings/Home).
- [x] Test Connection button (Ping) on Home.
- [x] Voice enrollment UI & upload flow (basic via FilePicker; recording to be added later).

---

## 6. Isac.Tests

### Required Tests (Initial) Status
- [x] `PingEndpoint_ReturnsSuccess` (integration-style TestServer ping test)
- [ ] `QueryEndpoint_ReturnsAudio_ForValidInput`
- [ ] `EnrollVoice_StoresProfileStub`
- [x] Serialization tests for DTOs
- [x] Basic error handling tests for `IsacHttpClient` (network failure case)

---

## Development Order (Recommended) Progress

1. Backend and Shared
   - [x] Implement `Isac.Core.Shared` DTOs and `IsacHttpClient`.
   - [x] Implement `Isac.Core.Api` with `/api/v1/ping` and stub `/api/v1/query`.
   - [x] Add basic tests in `Isac.Tests` (partial set; more remaining).
2. Wear App (Isac.Wear)
   - [ ] Not started.
3. Watch Face (Isac.Watchface)
   - [ ] Not started.
4. Mobile App (Isac.Mobile)
   - [x] Implemented Shell, Home (Ping), Settings (BaseUrl), and Enrollment (basic file-based flow). Recording UI pending.
5. Iterative Enhancements
   - [ ] Not started.

---

## Coding Style & Guidelines

- Use `async`/`await` for all I/O operations. (Applied where implemented.)
- Centralize HTTP configuration in `Isac.Core.Shared`. (In place.)
- Avoid duplicating endpoint URLs or DTOs across projects. (Maintained.)
- Fail fast and log meaningful errors in `Isac.Core.Api`. (Logging enhancement pending.)
- Keep platform-specific code isolated. (Current code respects this.)

---

This document should be used by automated coding assistants and human contributors to implement and maintain a consistent, well-structured ISAC system across all projects in the solution.
