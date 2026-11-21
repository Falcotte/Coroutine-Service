# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Workflow

This is a Unity project. Building, running, and testing all happen inside the Unity Editor (6000.6.4f1). The `com.unity.pipeline` package is installed, so an agent can drive the open editor with `unity command` (see the global editor automation rules). Open `Coroutine Service.sln` in your IDE for code editing with full IntelliSense support.

Work happens on the `development` branch. `main` is the release branch.

To run the editor inspection tool: **Angry Koala > Coroutines > Coroutine Inspector** (Play Mode only).

Tests live in `Assets/Coroutines/Tests/Runtime` (Play Mode only). There is no Edit Mode test assembly, because coroutines need Play Mode.

## Distribution

The module is a UPM package. Its root is `Assets/Coroutines`, which holds `package.json` (`com.angrykoala.coroutines`, version 2.0.0). Consumers install it from git with `?path=/Assets/Coroutines#<tag>`. It depends on `com.angrykoala.services` 2.0.0, which the project must also list in its own `Packages/manifest.json`, because Unity does not resolve git dependencies between packages.

## Architecture

The module lives entirely under `Assets/Coroutines/`. Its namespace is `AngryKoala.Coroutines`. The Services module (`AngryKoala.Services`) is an external package, not part of this repo.

### Assemblies

- `AngryKoala.Coroutines`: runtime code in `Scripts/`.
- `AngryKoala.Coroutines.Editor`: `Scripts/Editor/`, Editor only.
- `AngryKoala.Coroutines.Tests.Runtime`: `Tests/Runtime/`, Play Mode tests.

### Public API

**`ICoroutineService`** (extends `IService`): `Run(routine, owner, tag)`, `RunDelayed(action, delaySeconds, delayTime, owner, tag)`, `RunNextFrame(action, owner, tag)`, `Stop(coroutine)`, `StopAll()`, `StopAll(owner)`, `StopAll(tag)`, `GetData(List<CoroutineData>)`, and the `ActiveCoroutineCount` property. Owner and tag are optional. `Run` and the delayed starts return null when nothing is tracked, for example when the routine finishes at once or the owner is inactive.

**`CoroutineData`** (`readonly struct`): `Coroutine`, `Owner`, `Tag`, `RoutineName`, `ElapsedTime` (scaled) and `ElapsedUnscaledTime`.

**`DelayTime`**: `Scaled` or `Unscaled`, for `RunDelayed`.

**`CoroutineService`** (`sealed`, extends `BaseService<ICoroutineService>`): the implementation. It is a MonoBehaviour that registers itself in `Awake` through `BaseService`.

### Tracking

`CoroutineService` keeps one list of `TrackedRoutine` and a dictionary from `Coroutine` to `TrackedRoutine`. A `TrackedRoutine` is a private `IEnumerator` that wraps the caller's routine, so the service sees each step. It also steps nested routines (`yield return Inner();`) itself, on a small stack, and never hands an `IEnumerator` to Unity, so the owner check and the exception handling cover nested steps too. Every start method goes through `RunInternal`, which adds the record and then calls `StartCoroutine`.

A record is removed when its routine finishes, throws, or is stopped. Stopping goes through `StopInternal`, which calls `StopCoroutine` and then `RemoveTrackedRoutine`.

### Owner lifetime

The service ends a routine whose owner is gone in two places:

- `TrackedRoutine.MoveNext` checks the owner before each step. A routine whose owner is inactive is removed there.
- `Update` calls `StopRoutinesOfInactiveOwners`, which stops any record whose owner is destroyed or whose GameObject is inactive.

"Inactive" means `owner.gameObject.activeInHierarchy` is false. Disabling only the component does not end the owner's routines.

### Service lifecycle

- `OnDisable` calls `StopAll()`, so disabling or destroying the service stops everything and clears its records.
- `RunInternal` refuses to start when the service is not `isActiveAndEnabled`, and logs a warning.
- `RunInternal` refuses to start when the owner is destroyed or inactive, and logs a warning.

### Delayed and next-frame calls

`RunDelayed` and `RunNextFrame` wrap the action in a small routine, `DelayRoutine` or `NextFrameRoutine`. `DelayRoutine` counts down with `Time.deltaTime` or `Time.unscaledDeltaTime`, depending on `DelayTime`. A delay of zero or less calls the action before the method returns.

Routine names are derived from the caller's type or method name. Delayed and next-frame names are prefixed `Delayed: ` and `Next frame: `.

### Errors

An exception from a routine is caught in `TrackedRoutine.MoveNext`. `LogRoutineException` logs the owner name, tag and routine name, and the routine then ends. The other coroutines keep running.

### Editor Tool

**`CoroutineInspectorEditorWindow`** (`Scripts/Editor/`): an EditorWindow that lists running coroutines in Play Mode. It resolves the service with `ServiceLocator.TryGet`, so a missing service does not log an error. It reads records through `GetData`. Auto-refresh runs every 0.05 seconds and is on by default. Search, filter, sort and toggle settings are saved to EditorPrefs.

### Key Files

| File | Purpose |
|---|---|
| `Assets/Coroutines/Scripts/ICoroutineService.cs` | Public API interface |
| `Assets/Coroutines/Scripts/CoroutineService.cs` | Implementation, `TrackedRoutine`, owner checks, delayed calls |
| `Assets/Coroutines/Scripts/CoroutineData.cs` | Readonly struct, one snapshot per running coroutine |
| `Assets/Coroutines/Scripts/DelayTime.cs` | Scaled or unscaled delay |
| `Assets/Coroutines/Scripts/Editor/CoroutineInspectorEditorWindow.cs` | Play Mode inspector window |
| `Assets/Coroutines/Tests/Runtime/CoroutineServiceTests.cs` | Play Mode tests |
| `Assets/Coroutines/Tests/Runtime/PlayModeTestOwner.cs` | Test owner MonoBehaviour |
| `Assets/Coroutines/package.json` | UPM manifest |
