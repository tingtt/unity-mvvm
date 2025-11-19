# PRD: unity-mvvm

## Overview / Problem Statement

Unity's standard approach has these common problems:

- Most code changes require launching Unity to review
- Unity does not show diffs, making reviews take a very long time
- Folders are organized by scenes or script types, making it hard to understand dependencies

This project fixes these problems by using ideas from web frontend development like MVVM (React) and Next.js.

## Goals

- Define MVVM paradigm for Unity
  - Build systems centered on C# scripts that do not depend on UI elements (GameObjects built in scenes and prefabs)
- Reduce attaching C# scripts to GameObjects
  - Only attach scripts for:
    - ViewModels in MVVM pattern
    - Scripts for detailed UI animations and visual effects

## Non-Goals

- Declarative UI
  - Optimizations like React work well for web frontends, but for game applications we keep the imperative paradigm from real-time OS to avoid hiding performance optimizations

<!-- ## User Stories -->

## Proposed Solution / Feature Description

TODO: Write this as we build and test.

<!-- ## Mockup -->

## High-Level Tech Consideration

See [DesignDoc](./DesignDoc.md)

## Open Questions / Risks

- How to handle or prevent Git LFS rebase issues (out of scope but needs a solution)
