# Next Week Plan

Updated: `2026-05-22`

## Main Focus

Next week, the main goal is to move more of the game from runtime-created objects to scene or prefab-driven setup. This should make the project easier to maintain, test, and extend.

At the same time, I also want to start a new `3D` shooting practice mode.

## Priority Order

### Low Risk

- Scene-ify `rimrushGameBootstrap`, `Camera`, `Audio`, `Presenter`, and the main root objects.

### Low-Medium Risk

- Move menu background, `HUD`, buttons, and other static `UI` into scenes or prefabs.

### Medium Risk

- Change the court, hoop, and ball visuals into prebuilt objects controlled by scripts.

### Medium-High Risk

- Change player visual objects, animation containers, and spawn points to prefab or scene-reference driven setup.

### High Risk

- Refactor `rimrushPlayerObject`, `rimrushBallObject`, and `rimrushBasketObject` so they drive existing objects instead of creating their own.

### Highest Risk

- Clean up old runtime creation paths and keep only the dynamic parts that are still necessary.

## New Task

- Build an initial `3D` shooting practice prototype.
- First target: a separate scene with basic shooting, ball flight, and score detection working.

## Weekly Goal

- Finish the first round of scene/prefab migration for the core match setup.
- Make clear progress on the object-responsibility refactor.
- Get a playable first version of the `3D` shooting practice mode running.
