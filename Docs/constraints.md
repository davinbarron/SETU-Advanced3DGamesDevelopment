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
  - LOD0: `100%`
  - LOD1: `50%`
  - LOD2: `25%`
  - Culled: `5%`
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

Evidence:
- LOD Group inspector screenshot: `Docs/Screenshots/W5-B/lod_group_setup.png`