# CA2 Branch Strategy

## Overview

The repository uses a trunk-based approach with short-lived feature branches created per CA milestone. `main` is kept in a working state at all times — experimental work and feature development happens on dedicated branches before being merged back.

## Branch Structure

| Branch | Purpose | Created | Merged |
|---|---|---|---|
| `main` | Stable working state throughout the module | Week 1 (19 Jan) | N/A |
| `ca2/networking-countdown-timer` | Networked game state: countdown timer, phase system, game-over state (Option C) | Week 9 (23 Mar) | 08 cb7b8 (1 Apr) |
| `ca2/networking-pickup` | Score orbs with authority-correct pickup/respawn, player scoring (Option B) | Week 9 (28 Mar) | a240048 (1 Apr) |
| `ca2/networking-voting` | Rematch vote system, refined UI, final testing and documentation | Week 10 (1–5 Apr) | Pending merge before ca2-submit |

## Commit Convention

All commits follow a consistent prefix convention:

| Prefix | Usage |
|---|---|
| `feat:` | New feature or capability added |
| `fix:` | Bug or error corrected |
| `perf:` | Performance improvement |
| `docs:` | Documentation, markdown files, screenshots |
| `net:` | Networking-specific implementation |
| `chore:` | Housekeeping — gitignore, project settings, package updates |

This convention is documented here and applied consistently from Week 1 onward so that the commit graph is readable without needing to open individual diffs.

## Milestone Tags

| Tag | Week (actual calendar date) | Description |
|---|---|---|
| `baseline` | Week 1 | Repository created, project opens cleanly |
| `ca1-submit` | Week 4 | CA1 submitted, shader and lighting work complete |
| `ca3-start` | Week 9 | Constraints baseline and LOD profiling committed |
| `ca2-baseline` | Week 9 | Two-client Fusion session connecting, spawning, and KCC movement working |
| `ca2-submit` | Week 10 | Final CA2 submission state — features complete, tests passed, documentation committed |

## Workflow

1. Each networked feature (countdown timer, pickup orbs, rematch voting) was developed on a dedicated `ca2/networking-*` branch to keep iterations isolated
2. After testing and refining a feature, its branch was merged into `main` with a descriptive commit message like `merge: ca2 pickup feature`
3. Subsequent features were created from `main` and merged after their own testing cycle
4. `main` remained in a working state at all times — all feature branches started fresh and only merged back once tested and stable
5. The final `ca2-submit` tag will be applied on `main` after the last feature branch (`ca2/networking-voting`) is merged
6. `Library/`, `Temp/`, and `obj/` are excluded via `.gitignore` confirmed in Week 1

## Lessons Learned

I made consistent use of feature branches throughout the module from `feat/pbr-map-correctness` in Week 1 through `ca2/networking-voting` in Week 10 created a maintainable history where each lab session and feature work had its own branch isolated from `main`. This meant that when major features like CA1 rendering, LOD profiling, and Fusion networking were integrated, `main` remained stable.

I used a commit message convention (`feat:`, `net:`, `docs:`, etc.) consistently from the start which made the feature branches self-documenting. When reviewing a merge commit like `a240048 merge: ca2 pickup feature`, it was immediately clear what that branch accomplished without diving into individual diffs. Branches with descriptive names (`lab/w8-b-rpc`, `ca2/networking-pickup`) provided context at a glance.

It helps not only myself but for others who look at my history can easily tell the work I did and when I did it.

I will be continuing this pattern into CA3.
