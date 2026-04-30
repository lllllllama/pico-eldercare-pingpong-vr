# VRTableTennis Reuse Notes

## Reuse / migration status
- Target resources for migration: ball, paddle, table, net prefabs/models/materials/textures/audio.
- In this repository execution environment, direct remote fetch of `kushal-goenka/VRTableTennis` was blocked, so binary asset migration could not be completed in-commit.
- To keep delivery usable and demonstrable, this revision provides **assetized local fallback pack** under `_Project` (prefabs + materials) and uses it as first-class resources instead of runtime temporary objects.

## What is migrated now
- Gameplay structure and layout conventions inspired by VRTableTennis:
  - fixed table-centered training lane
  - periodic serve loop
  - right-hand paddle follow + velocity-based return
- Ball rigidbody defaults and collider strategy are configured for stable demo interaction.

## What is not migrated yet
- Original VRTableTennis meshes/textures/audio binaries are not included yet.
- Legacy XR rig dependent scripts from external projects are intentionally excluded to preserve current PICO XR setup.

## Why not directly use full VRTableTennis scene
- Current project already contains working PICO/XR setup and scene bootstrap.
- Pulling in a full foreign scene risks breaking controller bindings, camera rig assumptions, or project-level settings.
- This demo therefore composes objects into current scene via Editor tool, keeping existing XR Origin and Main Camera intact.

## Current PICO demo resource usage
- Editor tool creates/updates reusable assets in:
  - `Assets/_Project/Prefabs/PingPong`
  - `Assets/_Project/Materials/PingPong`
- Scene instances are built from these assets, not from transient runtime-only objects.
