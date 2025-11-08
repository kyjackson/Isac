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
   - ~~Use **ASP.NET Core** for the backend API.~~ **[REVISED]** No custom backend; phone app orchestrates cloud APIs.

2. **Supported Platforms**
   - Galaxy Watch6 Classic (Wear OS).
   - Samsung Galaxy S23 Ultra (for companion app).
   - Cloud APIs: OpenAI Realtime API, Cartesia TTS API.

3. **Design Constraints**
   - No significant Kotlin/Java code.
   - Do not require Android Studio for normal development.
   - Prefer shared libraries and clear separation of concerns.
   - Optimize for maintainability, testability, and minimal coupling.

4. **Security & Privacy (High Level)**
   - All communication with cloud APIs over HTTPS.
   - User-specific voice profiles managed via Cartesia API.
   - Provide mechanisms to delete voice data via Cartesia API.

5. **Core Concept (Revised Architecture)**
   - ISAC is a voice-driven assistant:
     - User speaks into the watch.
     - Audio is sent to phone via Bluetooth.
     - **Phone orchestrates cloud APIs:**
       1. **OpenAI Realtime API:** Accepts audio, performs ASR + LLM reasoning, returns text response.
       2. **Cartesia TTS API:** Converts text to speech using user's cloned voice.
     - Response audio is sent back to watch and played.
   - A custom watch face presents an ISAC-style HUD and deep-links into the ISAC app.

---

## Project Overview (Revised)

Solution projects:

1. `Isac.Core.Shared` (DTOs, API client abstractions)
2. ~~`Isac.Core.Api`~~ **[REMOVED]** No custom backend needed.
3. `Isac.Wear` (Watch app)
4. `Isac.Watchface` (Watch face)
5. `Isac.Mobile` (Phone companion app - now acts as orchestrator)
6. `Isac.Tests`

### Project Dependency Graph

- `Isac.Core.Shared`
  - No project dependencies.
- ~~`Isac.Core.Api`~~ **[REMOVED]**
- `Isac.Wear`
  - Depends on: `Isac.Core.Shared`
- `Isac.Watchface`
  - Depends on: `Isac.Core.Shared`
- `Isac.Mobile`
  - Depends on: `Isac.Core.Shared`
  - **New:** Integrates OpenAI Realtime SDK and Cartesia TTS SDK.
- `Isac.Tests`
  - Depends on: `Isac.Core.Shared`

---

## 1. Isac.Core.Shared

**Type:** .NET 10 Class Library  
**Purpose:** Shared contracts, clients, configuration primitives, and utilities.

### Responsibilities

- Define request/response DTOs for watch-to-phone communication.
- Provide abstractions for OpenAI Realtime and Cartesia API clients.
- Centralize configuration keys, API endpoints, and constants.

### Key Components

Status:
- [x] ~~`IsacApiOptions` (`BaseUrl`)~~ **[DEPRECATED]** No custom backend.
- [x] **[NEW]** `OpenAIRealtimeOptions` (API key, model selection, voice)
- [x] **[NEW]** `CartesiaOptions` (API key, voice ID, model)
- [x] DTOs: `WatchAudioMessage`, `PhoneAudioResponse` for Bluetooth audio transfer.
- [x] DTOs: `CartesiaVoiceEnrollRequest`, `CartesiaVoiceEnrollResponse` for Cartesia enrollment.
- [x] **[NEW]** Interface `IRealtimeClient` (OpenAI Realtime WebSocket wrapper)
- [x] **[NEW]** Interface `ICartesiaTTSClient` (Cartesia TTS API wrapper)
- [x] **[NEW]** DI extensions `AddOpenAIRealtimeClient`, `AddCartesiaTTSClient`.

### Implementation Notes

- All code must be platform neutral (no direct Android/iOS APIs).
- Provide extension methods to register API clients in DI containers. ✅ Implemented.
- Client implementations added in `Isac.Mobile` project (WebSocket/HTTP logic). ✅ Implemented.

---

## 2. ~~Isac.Core.Api~~ [REMOVED]

**Status:** No longer part of the architecture. Phone app calls cloud APIs directly.

---

## 3. Isac.Wear

**Type:** .NET for Android Wear OS Application  
**Purpose:** Main interactive ISAC app on the watch.

### Required Features (MVP) Status
- [x] Main `Activity` with simple UI (hold-to-speak and status).
- [x] Runtime `RECORD_AUDIO` permission handling.
- [x] Press-and-hold recording interaction.
- [x] Audio capture via `AudioRecord` (16kHz mono PCM16).
- [ ] **[REVISED]** Send audio to phone via **Wear OS Data Layer API** (Bluetooth).
- [ ] **[REVISED]** Receive response audio from phone and play via `AudioTrack`.
- [x] Display minimal status UI (Listening/Sending/Playing/Idle).
- [ ] **[REVISED]** Settings screen removed (no API base URL needed; phone handles all cloud comms).

---

## 4. Isac.Watchface

**Type:** .NET for Android Wear OS Application (Watch Face)

### Required Features (MVP) Status
- [x] Simple digital face showing time and date.
- [x] ISAC indicator element.
- [x] Tap anywhere to launch `Isac.Wear.MainActivity` (with intent).
- [x] Ambient mode (screen off/on simulation; minute updates in ambient).
- [x] Battery-efficient (second updates active, minute in ambient).

---

## 5. Isac.Mobile (Revised Role: Orchestrator)

**Type:** .NET MAUI App  
**Purpose:** Companion app that orchestrates cloud APIs and communicates with watch.

### Required Features (MVP) Status
- [x] Shell navigation (Home / Status, Enrollment, Settings).
- [ ] **[NEW]** Bluetooth communication with watch (Wear OS companion API or Data Layer).
- [x] **[NEW]** OpenAI Realtime API integration:
  - WebSocket client for audio input/text output. ✅ Implemented
  - Handle audio streaming from watch → Realtime API. ⏳ Pending Bluetooth
  - Parse text response from LLM. ✅ Implemented
- [x] **[NEW]** Cartesia TTS API integration:
  - Send text response + voice ID → receive cloned audio. ✅ Implemented
  - Stream audio back to watch. ⏳ Pending Bluetooth
- [x] **[REVISED]** Voice enrollment UI:
  - Record user's voice (multiple samples). ✅ File picker placeholder
  - Upload to Cartesia API to create custom voice. ✅ Implemented
  - Store voice ID in app preferences. ✅ Implemented
- [x] **[NEW]** Settings page:
  - Enter OpenAI API key. ✅ Implemented
  - Enter Cartesia API key. ✅ Implemented
  - Display enrolled voice ID. ✅ Implemented
- [ ] **[REMOVED]** ~~Test Connection button (Ping)~~ No custom backend to ping.

---

## 6. Isac.Tests

### Required Tests (Initial) Status
- [ ] **[REVISED]** `RealtimeAPI_ReturnsTextResponse` (mock WebSocket test)
- [ ] **[NEW]** `CartesiaTTS_ReturnsAudio` (mock HTTP test)
- [x] Serialization tests for DTOs
- [ ] **[NEW]** Bluetooth message serialization tests

---

## Development Order (Recommended) Progress

1. **Shared library + API client abstractions** ✅ **COMPLETE**
   - [x] Implement OpenAI Realtime client abstraction in `Isac.Core.Shared`.
   - [x] Implement Cartesia TTS client abstraction in `Isac.Core.Shared`.
   - [x] Add DTOs for Bluetooth audio transfer (watch ↔ phone).
   - [x] Add configuration options for OpenAI and Cartesia.
   - [x] Add DI extension methods.
2. Mobile App (Isac.Mobile) - **Core orchestrator** ⏳ **IN PROGRESS**
   - [x] Integrate OpenAI Realtime SDK (WebSocket client). ✅
   - [x] Integrate Cartesia TTS SDK (HTTP client). ✅
   - [ ] Implement Bluetooth communication with watch. ⏳ NEXT
   - [x] Build voice enrollment flow (record → upload to Cartesia). ✅
   - [ ] Implement end-to-end flow: audio in → Realtime → Cartesia → audio out. ⏳ Pending Bluetooth
3. Wear App (Isac.Wear)
   - [ ] Update to send audio to phone via Bluetooth (Data Layer API).
   - [ ] Update to receive audio from phone and play.
   - [ ] Remove Settings screen (not needed).
4. Watch Face (Isac.Watchface)
   - [x] Already complete (no changes needed).
5. Testing
   - [ ] Write integration tests for OpenAI + Cartesia clients.
   - [ ] Test watch ↔ phone Bluetooth communication.

---

## System Flow (Final Architecture - Option B)

```
┌──────────────┐                 ┌─────────────────┐
│ Galaxy Watch │  Bluetooth      │ Galaxy S23 Ultra│
│   (Wear OS)  │ ◄────audio─────►│   (MAUI App)    │
└──────────────┘                 └─────────────────┘
      │                                   │
      │ 1. User speaks                    │ 2. Receives audio
      │ 2. Records audio                  │
      │ 3. Sends to phone ────────────────┘
      │                                   │
      │                                   ▼
      │                          ┌─────────────────────┐
      │                          │ OpenAI Realtime API │
      │                          │  (ASR + LLM)        │
      │                          │  audio → text       │
      │                          └─────────────────────┘
      │                                   │
      │                                   │ 3. Text response
      │                                   ▼
      │                          ┌─────────────────────┐
      │                          │   Cartesia TTS API  │
      │                          │  (Voice Cloning)    │
      │                          │  text → audio       │
      │                          └─────────────────────┘
      │                                   │
      │ 5. Plays response audio           │ 4. Cloned audio
      └──────────────────audio────────────┘
```

---

## Coding Style & Guidelines

- Use `async`/`await` for all I/O operations.
- Centralize API configuration in `Isac.Core.Shared`.
- Use WebSocket for OpenAI Realtime (persistent connection).
- Use HTTP/REST for Cartesia TTS (stateless calls).
- Keep platform-specific code isolated (Bluetooth in MAUI, AudioRecord/AudioTrack in Wear).
- Fail fast and log meaningful errors (especially for WebSocket disconnects and API failures).

---

## Cost Estimates (100 users, 10 queries/day each)

| Component | Cost/month |
|-----------|------------|
| OpenAI Realtime API (audio in/out + LLM reasoning) | ~$75-150 |
| Cartesia TTS (voice cloning) | ~$45-90 |
| **Total** | **~$120-240** |

*Much cheaper than self-hosting ASR/LLM/TTS infrastructure (~$350-1700/month).*

---

This document should be used by automated coding assistants and human contributors to implement and maintain a consistent, well-structured ISAC system across all projects in the solution.
