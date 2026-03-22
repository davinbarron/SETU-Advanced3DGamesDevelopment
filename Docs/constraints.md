# CA3 Constraints Baseline (Week 5 Lab B)

## 1) Scope and Scene

- Scene under test: `CA3_VerticleSlice` 
- Date captured: `2026-03-22`
- Unity version: `6000.3.7 (Unity 6.3)`
- Render pipeline: `URP`
- Platform/profile: `Development Build` 
- Objective: establish a measurable baseline for LOD + occlusion culling in the vertical slice scene.

## 2) LOD Group Setup (Key Prop)

- Key prop name: `FanBig01Motor01`
- LOD Group location (Hierarchy path): `Scene Assets/Fan/FanBig01Motor01`
- LOD levels configured: `LOD0 / LOD1 / LOD2`
- Transition percentages:
  - LOD0: `60%`
  - LOD1: `30%`
  - LOD2: `10%`
  - Culled: `10%`
- Notes on source meshes (manual/existing): `Source asset shipped as a single mesh; LOD1/LOD2 variants authored in Blender and exported in one FBX with LOD naming.`

### 2.1 LOD Asset Constraint + Mitigation

- Constraint identified: `FanBig01Motor01` was imported as a single mesh and did not include pre-authored LOD variants.
- Tooling note: Substance 3D Painter is used for texturing and does not generate simplified geometry LOD meshes.
- Chosen mitigation path: `Generated LOD1 and LOD2 in Blender using Decimate, then exported with LOD naming in one FBX for Unity import.`
- Output:
  - LOD1 source + triangle reduction target: `FanBig01Motor01_LOD1 (approx. decimate ratio 0.55)`
  - LOD2 source + triangle reduction target: `FanBig01Motor01_LOD2 (approx. decimate ratio 0.25)`
  - Export format/import path: `FBX -> Assets/_Game/Art/Models/FanBig01Motor01`
- Validation check: each LOD slot in Unity references a different renderer/mesh, transitions are visible in Scene view preview, and Unity auto-created the LOD Group from naming convention.
- Blender screenshots available at: `Docs/Screenshots/Blender/FanLOD`
- LOD triangle counts in Unity:
  - LOD0: `4162 tris`
  - LOD1: `2289 tris` (~55% of LOD0)
  - LOD2: `1040 tris` (~25% of LOD0)

Evidence:
- LOD Group inspector screenshot: `Docs/Screenshots/W5-B/lod_group_setup.png`

## 3) Occlusion Culling Bake

- Occlusion scene baked: `Yes`
- Static assignment approach:
  - `Occluder + Occludee`: wall/floor large opaque geometry
  - `Occludee`: fan and smaller props
  - Barrel (dissolve shader): not used as occluder
- Bake parameters:
  - Smallest Occluder: `2`
  - Smallest Hole: `0.25`
  - Backface Threshold: `100`
- Validation method: Game view stats captured from three fixed camera positions in Play Mode (paused immediately after settling).

Evidence:
- Occlusion bake settings screenshot: `Docs/Screenshots/W5-B/occlusion_settings.png`

## 4) Baseline Metrics (3 Camera Positions)

| Camera | Draw Calls (Batches) | SetPass Calls | Tris | Verts | FPS (Frame ms) | CPU Main ms | Render Thread ms | Notes |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| A | 82 | 45 | 48.7k | 52.5k | 62.5 (16.0) | 16.0 | 2.1 | Near camera / highest detail region |
| B | 87 | 47 | 46.5k | 50.5k | 62.1 (16.1) | 16.1 | 2.3 | Mid camera / mixed visibility |
| C | 77 | 44 | 32.2k | 36.2k | 81.9 (12.2) | 12.2 | 2.6 | Far camera / lower LOD |

Screenshot references:
- Camera A stats: `Docs/Screenshots/W5-B/Camera_A/stats.png`
- Camera B stats: `Docs/Screenshots/W5-B/Camera_B/stats.png`
- Camera C stats: `Docs/Screenshots/W5-B/Camera_C/stats.png`