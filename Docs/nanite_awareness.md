# Week 6-A: Nanite Awareness

## Core Concept

Nanite is UE5's virtualised geometry system. The triangles are rendered at pixel precision maintaining LOD0 visual fidelity while eliminating triangle processing cost. Single high-poly asset import; engine auto-builds hierarchical cluster structure for on-demand streaming.

## Nanite vs. Traditional LOD

| Aspect | Traditional LOD (Unity W5) | Nanite (UE5 W6) |
|--------|---------------------------|-----------------|
| Source | Manual decimation (Blender 55%/25%) | Single high-poly asset |
| Visual Quality | Degrades at distance (discrete LOD steps) | Maintained at all distances (pixel-precise) |
| Authoring | Multi-variant required; manual testing | Single import; engine builds hierarchy |
| Performance | 3 discrete LOD levels | Dynamic cluster streaming per-pixel |

## Baseline Metrics (Big Fan Asset)

| Metric | Nanite | Traditional Quads | Improvement |
|--------|--------|------------------|-------------|
| Frame Time | 64.50 ms | 129.87 ms | **50% faster** |
| Primitives | 9,306 | 21,800 | **57% fewer** |
| Overdraw | 1x (green) | 2-4x (blue) | **minimal** |

Overdraw visualisation: Nanite surface at 1x green; traditional quads show dense blue interior overdraw. Cluster structure shows hierarchical grouping—fine detail at silhouettes, coarser interior. Demonstrates intelligent asset reorganization into fetch-efficient hierarchy.

## Nanite Settings (Baseline)

- **Enable Nanite Support**: Enabled
- **Keep Triangle Percent**: 100% (preserve full detail)
- **Normal Precision**: Auto (8-bit)
- **Minimum Residency**: Minimal (32 KB streaming)
- **Lerp UVs**: Enabled (seam blending)

## Best Fit & Limitations

**Ideal**: Static high-poly props (rocks, buildings, machinery), hero assets, complex detail-rich geometry.

**Poor Fit**: Animated meshes, transparent materials, simple geometry, deformable content.

**Key Constraints**:
- Static geometry only (animated requires traditional LOD)
- Opaque rendering only (transparents process full geometry)
- Conservative rasterisation (sub-pixel features may disappear at distance)
- Requires high-quality source input (poor asset = poor result)

## Takeaway

Nanite delivers LOD reduction benefits without visual loss. Visual fidelity remains LOD0-equivalent at all distances. Superior to manual LOD workflow for static assets, though both remain valuable in modern engines for different use cases.

## Evidence

Outputs in `Docs/Screenshots/W6-A/`:
- Overview/: Nanite Settings, Lit view, multi-mode visualization
- Nanite_Visualiser/: Overdraw comparison (Nanite vs. Quad), Triangles view, Clusters view
- Stats/: Frame metrics with Nanite enabled vs. disabled


