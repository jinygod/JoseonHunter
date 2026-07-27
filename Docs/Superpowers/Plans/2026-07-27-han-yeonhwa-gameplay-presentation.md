# Han Yeonhwa Gameplay Presentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the approved slower combat pace and uncluttered health presentation while preparing the playable slice for Han Yeonhwa's later art replacement.

**Architecture:** Keep the current rapid playable controller and change only the authoritative movement values and health-bar ownership. The player retains a world-space bar, normal enemies create no bar, and the boss exposes its health through one top-screen UI bar.

**Tech Stack:** Unity 6000.5.5f1, C# 9, URP 2D, Unity MCP.

## Global Constraints

- Player speed is exactly `2.4`.
- Normal enemy speed interpolates from `0.775` to `1.325`.
- Test boss speed is exactly `1.125`.
- Attack interval and experience values do not change.
- Normal enemies create no health-bar GameObjects.
- The player keeps the world-space health bar.
- The Fallen General uses one named boss bar at the top of the UI.
- Testing remains a focused Play-mode smoke check per the user's speed preference.
- Unrelated user and Unity-generated working-tree changes remain unstaged.

---

### Task 1: Slow Actors And Move Boss Health Into The HUD

**Files:**
- Modify: `Assets/JoseonHunter/Scripts/Runtime/Gameplay/FirstPlayableController.cs`

**Interfaces:**
- `EnemyState.HealthFill` is non-null only for the player-owned health bar path and is removed from enemy state.
- `OnGUI()` reads the active boss state and renders its normalized health.

- [ ] Change reset movement speed from `4.8f` to `2.4f`.
- [ ] Change normal enemy speed interpolation to `Mathf.Lerp(0.775f, 1.325f, elapsed / TestDuration)`.
- [ ] Change boss speed from `2.25f` to `1.125f`.
- [ ] Stop calling `CreateHealthBar` from normal enemy, boss, and treasure creation.
- [ ] Remove enemy health-fill updates while preserving the player's bar update.
- [ ] In `OnGUI`, find the live boss and draw a dark top panel, boss name, background bar, red normalized fill, and integer current/maximum health.
- [ ] Enter Play mode, verify the serialized/runtime values, confirm normal enemies have no `Health Bar` child, confirm the player does, spawn a boss through the existing path, and confirm the boss HUD data path executes without a gameplay exception.
- [ ] Commit with `feat: tune pace and boss health presentation`.

---

## Completion Gate

- Player, normal enemies, and boss use the exact approved movement speeds.
- Only the player has a world-space health bar.
- Boss health appears at the top of the screen while the boss is alive.
- Unity reports zero new gameplay errors in the focused smoke run.
