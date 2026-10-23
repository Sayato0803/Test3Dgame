# Unity 3D Animation Guidelines

## 1. Rigging & Retargeting
- For character models (e.g. Knight, Mage, King), configure `Animation Type: Humanoid` to enable cross-character animation retargeting and shared avatars.

## 2. Locomotion & Blend Trees
- Avoid sudden state swaps between Idle, Walk, and Run.
- Use **1D or 2D Blend Trees** driven by movement speed / velocity parameters (`Speed`, `Direction`).

## 3. Smooth State Machine Transitions
- For instant-response actions (Attack, Jump, Dash, Cast Spell):
  - Set `Has Exit Time = false`.
  - Set `Transition Duration` between `0.1s` and `0.2s` for seamless blending without animation clipping or freezing.
- Always use normalized transition offsets or appropriate interruption sources (`Current State` or `Next State`) for fluid gameplay combat.
