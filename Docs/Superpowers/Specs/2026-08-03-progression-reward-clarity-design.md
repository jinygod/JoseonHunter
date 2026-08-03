# Progression, Reward, and Pickup Clarity Design

## Goal

Make level-ups feel paced and consequential instead of continuous and automatic. The appraisal must reveal the rolled value before naming its grade, support upgrades must be common and legible, weapon choices must feel rarer, queued levels must briefly return the player to combat, and important combat pickups and Thunder Crash Bomb feedback must remain readable at the distant portrait camera.

## Confirmed causes

- `FirstPlayableController` initializes experience at 8 and then uses `7 + level * 4` instead of the domain `ExperienceCurve`. This reaches level 22 at roughly 1,100 normal-enemy experience.
- `UpgradeSelector` always inserts one owned-weapon upgrade and one unowned weapon before filling the third card. A normal three-card choice therefore contains at least two weapons whenever both pools are available.
- `NotifyUpgradePresentationClosed` opens the next pending level immediately. A magnet can therefore chain several paused selection and reward screens without a playable interval.
- `RewardRevealPresenter` applies reward intensity to the final alpha of the entire root. A support reward ends at only 70% opacity, including its panel and text. Support rewards also pass only the raw delta, such as `+0.7`.
- Thunder Crash Bomb maps its expanding damage radius to opaque concentric detonation frames, which produces the large geometric disk seen in the report.
- The checked-in experience flame and yeopjeon sprites are readable at source resolution, but runtime scales of `0.30` and `0.18` make them too small for the current camera.

## Appraisal verdict sequence

- Keep the existing fast-then-slow numeric count-up.
- While the value is moving, show `추가옵션 감정 중` and keep the grade seal hidden.
- After the number reaches its final value, hold it for a short beat, then reveal the grade (`일반`, `고급`, or `최대`) with the existing restrained scale pulse.
- Only after the verdict appears does the presenter enter its readable/confirmable state.
- Read-only weapon details continue to show the already-known grade immediately.

The timeline will expose an explicit grade-reveal time so presentation tests can prove that value lock precedes grade disclosure.

## Experience and queued level-ups

- `ExperienceCurve` becomes the single source of truth for every level and uses `8 + 6L + L²`, where `L` is the current level. Representative thresholds are 15, 24, 35, 48, 63, 80, 99, 120, 143, and 168.
- `FirstPlayableController` uses the curve both at reset and after every level-up. The formula is integer-only and guarded against overflow.
- The first earned level still opens immediately.
- When more levels are pending, closing a reward returns to gameplay and starts a 1.0-second combat grace period. The next pending choice opens after the grace period. Earned experience and pending levels are never discarded.
- The grace timer resets with the run and is evaluated only while gameplay is running.

At approximately 1,100 normal-enemy experience, the expected level falls from about 22 to about 12. This keeps a three-minute vertical slice active without making the opening feel empty.

## Upgrade offer economy

- While support upgrades are eligible, every three-card choice contains at least one support option.
- While at least two distinct supports are eligible, choices normally contain two supports.
- A choice contains at most one weapon option unless there are not enough support options to fill three cards.
- Weapon opportunities use deterministic pacing from the player level: every fourth level guarantees one weapon option; other levels have a seeded 25% weapon chance.
- A weapon slot first prefers an owned-weapon upgrade, then a new weapon, alternating the preference by level so both strengthening and discovery remain possible.
- If the preferred categories are exhausted, the selector fills from all remaining eligible upgrades without duplicates and preserves replacement rules.
- Existing evolution eligibility remains unchanged.

`UpgradeSelector` receives the current player level as an explicit input. A compatibility overload retains the old signature for existing callers and tests.

## Support reward presentation

- The support/evolution reward uses an opaque hanji panel with a dark ink border, dark text, and a dark-brown confirm button with light text.
- Reward intensity controls the entrance scale pulse, not final panel opacity. The final root alpha is always 1.
- The support glyph is retained but reduced so the name and effect remain dominant.
- Support reward summaries are complete Korean descriptions:
  - `최대 체력 +20`
  - `이동 속도 +12%`
  - `경험치 획득 범위 +0.7`
- No new font or image asset is required.

## Thunder Crash Bomb feedback

- Standard blast presentation uses the existing transparent lightning-current frames instead of the opaque concentric detonation disk.
- The damage radius and exact pixel-contact rules remain unchanged; only presentation-frame selection changes.
- The confirmed-hit flash also uses a lightning-current frame with a short pale-blue flash.
- The visual continues to scale to the gameplay radius, but most pixels remain transparent, so the effect reads as expanding electricity rather than a solid geometric plate.
- No PixelLab generation is required because an appropriate checked-in frame set already exists.

## Pickup readability

- Experience flame base scale changes from `0.30` to `0.48`, preserving its attraction pulse, trail, attraction radius, and 0.42 collection distance.
- Yeopjeon base scale changes from `0.18` to `0.34` and gains a small scale pulse using the existing object; no extra renderer or allocation is added.
- The magnet scale remains separate and unchanged unless its fallback sprite is used.
- Pickup collision and balance values do not change.

## Performance and safety

- No new textures, materials per pickup, particle systems, managers, or per-frame collections are introduced.
- The pending-level grace period adds one float and one branch to the existing gameplay tick.
- Pickup animation changes only transform scale on existing objects.
- Thunder uses already-loaded presentation sprites and the existing pooled transient visual path.
- Serialized scenes, prefabs, render settings, and user-owned dirty assets are outside this change.

## Verification

- EditMode tests cover the scalable experience curve, deterministic offer composition, weapon rarity/pity behavior, and grade-after-value timing.
- PlayMode tests cover the appraisal text/seal sequence, opaque support reward presentation and complete Korean summary, queued-level combat grace, and enlarged pickup scales.
- Weapon mechanic tests verify Thunder still uses the same gameplay states and damage contacts while selecting the transparent current-frame range for blast presentation.
- Run the focused tests red then green, the full EditMode suite, the full PlayMode suite, and the Android development build.
- Review the final diff and stage only task-owned files. Preserve all existing unrelated modifications.

## Acceptance criteria

- A player cannot see the affix grade before the rolled number has settled.
- Roughly 1,100 normal-enemy experience produces about level 12, not level 22.
- Eligible support upgrades appear in every choice; weapon cards are capped at one except when supports cannot fill the choice.
- Pending magnet levels include a visible playable interval between reward screens.
- The support reward panel is opaque and its effect states the affected stat and unit.
- Thunder Crash Bomb no longer displays the large concentric disk.
- Experience flames and yeopjeon are visibly larger without changing pickup balance.
