# Tycoon Metropolis VR — Art Direction & Spatial Audio Style Guide
**Document Version:** 1.0.0  
**Target:** Meta Quest Standalone VR / Mixed Reality  
**Status:** Approved AAA Production Blueprint (Phase 8 Gate)

---

## 1. Visual Art Direction: Executive Tabletop Strategy-Diorama

### 1.1 Core Aesthetic Pillars
1. **Executive High-Roller Luxury:** The board presents an upscale boardroom diorama with brushed gold trims, dark polished marble foundations, and subtle emerald/champagne emissive accents.
2. **Tactile Diorama Miniature Scale:** The city sits atop an anchored physical table at 1:1 scale (0.50m board half-extent), creating the feel of an intricate living architectural model with animated courtyard pedestrians, moving micro-traffic, and executive sky-scrapers.
3. **High-Contrast Readability in Passthrough & VR:** Property boundaries and card text maintain high luminance contrast against both virtual skyboxes and real-world MR room passthrough.

### 1.2 Approved Color & Material Palette

| Token Name | Hex Code | Material Properties | Application |
| :--- | :--- | :--- | :--- |
| **Executive Gold Trim** | `#D9B347` | Metallic: 0.95, Smoothness: 0.88 | Outer table bevel, trophy inlays, gold coin pickups |
| **Dark Obsidian Base** | `#141A22` | Metallic: 0.05, Smoothness: 0.92 | Table plinth, plaque base, HUD backing console |
| **Table Felt Neutral** | `#0D1117` | Metallic: 0.00, Smoothness: 0.25 | Center board felt, diorama roadway surface |
| **Active Turn Emerald** | `#00FF88` | Emissive HDR: +1.2, Smoothness: 0.95 | Nameplate active strip, valid action button hover |
| **Cash / Payout Green** | `#3FFF73` | Emissive HDR: +0.8 | Cash balance readout, positive rent gains |
| **Warning / Debt Red** | `#FF3344` | Emissive HDR: +0.6 | Bankruptcy alert, debt deductions, penalty tiles |

### 1.3 Lighting & Atmospheric Temperatures
- **Tabletop Key Light:** 5200K (Daylight Neutral), directional angle (55° pitch, -30° yaw) with soft contact shadows (shadow distance 2.5m on mobile).
- **Executive Night Mode:** 3200K (Warm Incandescent), 0.07 lux city skyscraper window glows, with MR passthrough opacity reduced to 0.22 for enhanced focus.
- **Minigame Spotlights:** 4500K focused conical rim light highlighting physical dice trays, briefcase dials, and valve wheels.

---

## 2. Spatial Audio Architecture & Sound Bank Design

### 2.1 Audio Bus Structure & Routing

```
                     [Master AudioMixer]
                              |
      +-----------------------+-----------------------+
      |                       |                       |
[UI & HUD Prompts]    [Tabletop SFX]          [Living Ambience]
- 2D Stereo           - 3D Spatial (0.85)     - 3D Quad Ambience
- Zero latency        - Custom rolloff 0.3-3m - Muted in Passthrough
- Priority: High      - Max voices: 6         - Priority: Low
```

### 2.2 Event-to-Cue Sound Bank Matrix

| Event Name | Acoustic Description | Frequency / Formant Profile | Max Voices / Debounce |
| :--- | :--- | :--- | :--- |
| **`DiceRattle`** | Hollow wooden tumbler rattle | Multi-impulse noise + 180Hz body resonance | 1 voice / 60ms debounce |
| **`DiceImpact`** | Crisp felt bounce | 140Hz decaying fundamental + soft felt tap | 2 voices / 40ms debounce |
| **`TokenStep`** | Solid tactile miniature chess thud | 220Hz wood-on-felt transient (35ms duration) | 1 voice / 50ms debounce |
| **`CashTransfer`** | Double harmonic metallic chime | 880Hz + 1760Hz bell strike with exponential decay | 2 voices / 50ms debounce |
| **`PropertyDeedStamp`**| Executive wax seal / paper stamp | 450Hz low snap + paper slide | 1 voice / 100ms debounce |
| **`BuildingConstruction`**| Hydraulic click & stone settling | 320Hz rising harmonic sweep + locking click | 1 voice / 100ms debounce |
| **`CelebrationBurst`** | Distant fireworks pop & shimmer | 65Hz launch thud + high resonant shimmer | 4 voices / 30ms debounce |
| **`PenaltyDebt`** | Heavy damped bronze gong | 110Hz minor resonant dissonance | 1 voice / 200ms debounce |

### 2.3 Acoustic Synthesis Implementation Rules
- All synthesized SFX employ full ADSR (Attack, Decay, Sustain, Release) envelopes.
- Harsh, single-frequency raw sine or square waves are strictly banned from normal gameplay.
- Fast successive events (such as flying coin cascades or rapid token walking) enforce a minimum 40ms debounce interval to prevent audio clipping or amplifier distortion.

---

## 3. Animation Game Feel & Performance Budgets

### 3.1 Animation Timings & Durations
- **Button Poke / Selection:** 0.12s elastic pop (scale 1.0 -> 0.92 -> 1.05 -> 1.0).
- **Flying Cash Stream:** 0.55s parabolic arc with gentle spin (max 8 coin primitives).
- **Celebration Fireworks:** 1.35s gravity-affected particle sparks with linear fade.
- **Table Auto-Rotation (Pass & Play):** 0.85s smooth quintic ease-in-out curve.

### 3.2 Cancellation and Safety Guarantees
- Every visual animation coroutine checks `matchSessionGeneration` per frame.
- Calling `ResetMatch()` or changing scenes executes `ClearActiveTransientVisuals()`, instantly freeing and destroying all active animated sparks, coins, or temporary effect primitives.
