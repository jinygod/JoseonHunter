# Task 12 — portrait stabilization release handoff

## Delivered

- Added the reproducible Android development build entry point and PowerShell wrapper. The successful APK is `Builds/Android/JoseonHunter-development.apk` (101,971,466 bytes; SHA-256 `86a4a922b05e17e6ffaa5774cde009443070437f1b50ef5b62b159e08efe586a`).
- Added `PortraitStateValidationCapture`, which drives the production controller and presenters through Gameplay, level-up, appraisal, and resumed combat at all five required portrait sizes. It persists through play-mode domain reload and writes synchronous `Camera.Render` PNGs.
- Recorded 20 capture hashes, safe-area method, state gates, and visual review in `Docs/Verification/2026-07-31-portrait-stabilization-vertical-slice.md`.

## Validation and limitations

- Focused capture policy EditMode test passed after a red compile failure for the initially absent policy type.
- Final capture command must omit `-nographics`; with that option URP crashed in native `Camera.Render`. The graphics-device run exited 0 and produced all 20 PNGs.
- Full EditMode evidence is 527/527 passed. Full PlayMode evidence remains 182/261 passed with 79 pre-existing/unrelated failures recorded in the verification document.
- No Android device was attached; device capture and 30/50/100-enemy device performance remain blocked and are not represented as Editor data.
