# Camera — Eval Prompts

## Prompt 1: "Camera jitters when following the player at high speed"
**Tests:** Core follow knowledge, SmoothDamp vs Lerp understanding
**Assertions:**
- Should verify camera is in LateUpdate (not Update)
- Should check SmoothDamp usage (not Lerp)
- Should explain WHY Lerp causes jitter (frame-rate dependent percentage-of-remaining)
- Should check smoothTime value (too low = jitter, too high = lag)
- Should NOT suggest modifying player movement

## Prompt 2: "Add parallax for a new underground cave area"
**Tests:** Parallax system knowledge, layer configuration
**Assertions:**
- Should define layers with parallax factors (0.1–1.0 range)
- Should set appropriate Z positions for sorting
- Should ensure layers are wide enough to cover camera bounds
- Should reference existing ParallaxBackgroundManager API
- Should consider infinite scrolling for seamless backgrounds

## Prompt 3: "Boss room camera should zoom in and lock to the arena"
**Tests:** Boss room pattern, bounds system knowledge
**Assertions:**
- Should use BossRoomTrigger pattern
- Should lock camera bounds to room dimensions
- Should adjust orthographic size for zoom
- Should use smooth transition (not snap)
- Should reference enemy-behavior agent for boss trigger coordination

## Prompt 4: "Camera shows black edges when player reaches map boundaries"
**Tests:** Bounds clamping knowledge, aspect ratio awareness
**Assertions:**
- Should check bounds clamping accounts for camera half-width/half-height
- Should verify bounds min/max are set correctly
- Should test at different aspect ratios (16:9, 21:9, 4:3)
- Should use camera.orthographicSize and camera.aspect for calculation
- Should suggest dead zones or background fill as alternatives

## Prompt 5: "Add screen shake when the boss does a ground slam"
**Tests:** Camera shake implementation, cross-agent coordination
**Assertions:**
- Should use the existing Shake(duration, magnitude) pattern
- Should decay magnitude over duration for natural feel
- Should coordinate with enemy-behavior (boss attack event triggers shake)
- Should coordinate with vfx (ScreenFlash may accompany shake)
- Should NOT modify boss attack code (file bead for enemy-behavior)
