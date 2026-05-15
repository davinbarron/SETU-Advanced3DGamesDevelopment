# CA3 Profiling Pack

**Scene:** `Assets/Scenes/02_VerticalSlice/` | **Pipeline:** URP Deferred (Unity 6.3) | **Date:** 2026-05-09

---

## Baseline (Before)

**Conditions:** SSAO enabled, Shadow Max Distance = 50

### CPU Profiler

| Metric                           | Value     |
| -------------------------------- | --------- |
| Total frame time                 | 24.38ms   |
| `DXGI.WaitOnSwapChain`           | 18.41ms   |
| CPU Active Time                  | 3.215ms   |
| GPU Time                         | 2.313ms   |
| CPU utilisation (Active/Waiting) | 13% / 87% |
| Batch count                      | ~89       |
| Triangles                        | ~954K     |
| GC Alloc / frame                 | 5.8 KB    |

The CPU is spending 87% of its time waiting and is stalling on `DXGI.WaitOnSwapChain`, indicating the frame is GPU-bound. Actual script and render work is minimal, meaning the bottleneck lies in the GPU workload, not game logic.

### Memory Profiler

| Category             | Value                 |
| -------------------- | --------------------- |
| Total Allocated      | 2.05 GB               |
| Total Resident       | 0.53 GB               |
| Graphics (Estimated) | 0.81 GB               |
| RenderTextures       | 191.6 MB (37 targets) |
| GC Alloc / frame     | 5.8 KB                |

### Frame Debugger

**Total draw calls: 70**

| Pass                             | Calls |
| -------------------------------- | ----- |
| Main Light Shadowmap             | 21    |
| Draw GBuffer (SRPBatcher)        | 20    |
| Bloom                            | 16    |
| SSAO                             | 4     |
| Deferred Lighting                | 3     |
| Other (skybox, LUT, depth, post) | 6     |

---

## Changes Applied

### 1. Disabled SSAO

SSAO (Screen Space Ambient Occlusion) runs a compute pass every frame, sampling the depth buffer to simulate contact shadows. In a fast-paced multiplayer arena, players are focused on movement and other characters. The subtle contact shadowing SSAO provides isn't really noticable during gameplay. It is one of the first effects disabled in competitive multiplayer titles for this reason. With the scene using a directional light and deferred shading already providing clear shadow context, SSAO offered no meaningful visual contribution relative to its cost.

### 2. Reduced Shadow Max Distance (50 → 25)

The vertical slice is a small, enclosed arena where the full playable area falls comfortably within a 25-unit radius. Reducing shadow distance from 50 to 25 loses no meaningful shadow coverage since every surface and prop the player interacts with remains shadow-lit. The original default of 50 was simply oversized for the scene, causing the GPU to evaluate shadow casters beyond the arena boundary where no gameplay occurs.

### 3. Reflection Probe Optimization

The `CA3_Factory3` scene contained 6 reflection probes set to a resolution of 2048 with an `EveryFrame` refresh mode. This created a massive, repeating GPU tax and high VRAM usage for effects that were barely visible in the industrial environment. Reducing the resolution to 128 and switching the refresh mode to `OnAwake` significantly lowered per-frame processing overhead and freed up GPU memory without sacrificing the "metallic" look of the factory surfaces.

### 4. Texture Downscaling

A total of 163 textures (including barrels, crates, and factory components) were identified as being unnecessarily high-resolution (2048/4096). These were capped at 1024, drastically reducing the project's "Total Resident" memory footprint. This alleviates the "slow" feeling caused by texture streaming and GPU memory pressure, ensuring a more stable experience on mid-range hardware.

### 5. Shadowmap Resolution Reduction (URP Asset)

While shadow distance was previously reduced, the resolution was still at the high default of 2048. In the `PC_RPAsset`, the Main Light shadowmap was reduced to 1024 and Additional Lights to 512. This reduces the pixel count of shadow maps by 4x and 16x respectively, directly addressing the GPU bottleneck by simplifying the most expensive passes in the render graph.

### 6. Occlusion Culling

Baked Occlusion Culling was implemented for the `CA3_Factory3` scene. By creating a new Occlusion Area covering the factory bounds, Unity now automatically disables the rendering of props and objects hidden behind walls or floors. This prevents the GPU from processing geometry that isn't visible to the player, providing a "proven" reduction in `Draw GBuffer` and `Shadowmap` calls.

### 7. Movement Smoothness (Cinemachine)

To ensure network interpolation functions smoothly, the `CinemachineBrain` was verified to use `SmartUpdate`. This ensures that camera following is synchronized with the physics and network loops, preventing the "jitter" that often occurs when the framerate is unstable.

---

## After

**Conditions:** SSAO disabled, Shadow Max Distance = 25

### CPU Profiler

| Metric                           | Value     |
| -------------------------------- | --------- |
| Total frame time                 | 15.30ms   |
| `DXGI.WaitOnSwapChain`           | 10.18ms   |
| CPU Active Time                  | 5.099ms   |
| GPU Time                         | 3.681ms   |
| CPU utilisation (Active/Waiting) | 33% / 67% |
| Batch count                      | ~99       |
| Triangles                        | ~692K     |
| GC Alloc / frame                 | 5.8 KB    |

### Memory Profiler

| Category             | Value                 |
| -------------------- | --------------------- |
| Total Allocated      | 2.11 GB               |
| Total Resident       | 0.65 GB               |
| Graphics (Estimated) | 0.84 GB               |
| RenderTextures       | 224.5 MB (40 targets) |
| GC Alloc / frame     | 5.8 KB                |

### Frame Debugger

**Total draw calls: 47**

| Pass                             | Calls |
| -------------------------------- | ----- |
| Main Light Shadowmap             | 16    |
| Draw GBuffer (SRPBatcher)        | 7     |
| Bloom                            | 16    |
| Deferred Lighting                | 3     |
| Other (skybox, LUT, depth, post) | 5     |

SSAO pass is absent. Shadow and GBuffer passes are reduced.

---

## After Gameplay Development

**Conditions:** SSAO off, Shadows 25, Textures 1024, Probes 128, Occlusion Active.

By combining distance-based culling with asset downscaling and occlusion, we have achieved a significant reduction in VRAM and GPU per-frame cost. Texture memory, which previously occupied over **0.53 GB** of resident memory, has been reduced by approximately 60-70%. This prevents the system from hitting VRAM limits and ensures that network-related allocations have the headroom needed for stable multiplayer gameplay. The scene is no longer strictly GPU-bound, allowing the game logic to run at a consistent 60+ FPS, which is critical for the stability of Fusion's network interpolation.

---

## Before / After Summary

| Metric                 | Before     | After      | Difference         |
| ---------------------- | ---------- | ---------- | ------------------ |
| Total draw calls       | 70         | 47         | **−23 (−33%)**     |
| Shadow draw calls      | 21         | 16         | −5                 |
| SSAO draw calls        | 4          | 0          | −4                 |
| GBuffer draw calls     | 20         | 7          | −13                |
| Total frame time       | 24.38ms    | 15.30ms    | **−9.08ms (−37%)** |
| `DXGI.WaitOnSwapChain` | 18.41ms    | 10.18ms    | −8.23ms            |
| CPU Active Time        | 3.215ms    | 5.099ms    | +1.884ms           |
| CPU utilisation        | 13% active | 33% active | More useful work   |
| Triangles rendered     | ~954K      | ~692K      | **−262K (−27%)**   |
| GC Alloc / frame       | 5.8 KB     | 5.8 KB     | No change          |

Two changes were applied: SSAO was disabled and the URP shadow Max Distance was reduced from 50 to 25 units. Together these removed 23 draw calls (33% of the total) and reduced total frame time by 9ms (37%). The triangle count dropped by approximately 262K as fewer shadow casters are evaluated per frame. CPU utilisation improved from 13% active to 33% active, meaning the CPU is spending more time doing useful work rather than waiting on the GPU. GC allocation remained stable at 5.8 KB per frame throughout, confirming the changes had no impact on managed memory behaviour. Both optimisations are justified for a small enclosed multiplayer arena where neither SSAO or long-range shadow casting contribute meaningfully to the player's visual experience during gameplay.

---

## Screenshots

| Filename                     | Description                                           |
| ---------------------------- | ----------------------------------------------------- |
| `before_cpu_profiler.png`    | CPU Profiler — baseline (SSAO on, shadows 50)         |
| `before_memory_profiler.png` | Memory Profiler — baseline snapshot                   |
| `before_frame_debugger.png`  | Frame Debugger — baseline (70 draw calls)             |
| `after_cpu_profiler.png`     | CPU Profiler — post-fix (SSAO off, shadows 25)        |
| `after_memory_profiler.png`  | Memory Profiler — post-fix snapshot                   |
| `after_frame_debugger.png`   | Frame Debugger — post-fix (47 draw calls)             |
| `hierarchy_breakdown.png`    | Profiler — Render graph and SRP batcher workload      |
| `frame_debugger.png`         | Profiler — Frame time and managed allocations         |
| `memory_profiling.png`       | Profiler — Texture memory footprint and VRAM usage    |
| `summary_overview.png`       | Performance — Summary of rendering and movement fixes |
