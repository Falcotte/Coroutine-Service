# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [2.0.0] - 2026-11-21

### Added

- `RunNextFrame(Action, owner, tag)`, which calls an action on the next frame.
- `ICoroutineService.ActiveCoroutineCount`, a property that returns the number of tracked coroutines.
- `DelayTime` enum (`Scaled` and `Unscaled`) for `RunDelayed`.
- `CoroutineData.RoutineName`, `ElapsedTime` and `ElapsedUnscaledTime`.
- Play Mode tests in `Tests/Runtime`.
- Assembly definitions `AngryKoala.Coroutines`, `AngryKoala.Coroutines.Editor` and `AngryKoala.Coroutines.Tests.Runtime`.
- `package.json` for distribution as a UPM package from `Assets/Coroutines`.

### Changed

- `Run` is now `Run(IEnumerator routine, MonoBehaviour owner = null, string tag = null)`. One signature replaces the four overloads.
- `RunDelayed` is now `RunDelayed(Action action, float delaySeconds, DelayTime delayTime = DelayTime.Scaled, MonoBehaviour owner = null, string tag = null)`. One signature replaces the four overloads.
- `GetActiveCoroutineCount()` is now the `ActiveCoroutineCount` property.
- `GetData()` and its owner and tag overloads are now `GetData(List<CoroutineData> results)`, which fills the caller's list.
- `CoroutineData` is a `readonly struct`. It has `RoutineName`, `ElapsedTime` and `ElapsedUnscaledTime`.
- Services are resolved through the Services package, not a copy under `Assets/Services`.

### Removed

- `[DefaultExecutionOrder(-1002)]` on `CoroutineService`. Callers resolve the service in `Start` or later.
- `CoroutineData.RoutineTypeName`, `StartedTime`, `StartedRealtime`, `ElapsedRealtime` and `TotalPauseDuration`.
- The copy of the Services module under `Assets/Services`.

### Fixed

- Coroutines of a destroyed or deactivated owner kept running. They now end with the owner.
- Records stayed in the service after its GameObject was deactivated. Deactivating the service now stops everything and clears its records.
- Exceptions were logged without context. They now include the owner, tag and routine name.
- A nested routine (`yield return Inner();`) that threw was logged by Unity alone and left its record behind. The service now steps nested routines itself.
- The Coroutine Inspector kept showing an empty table after the service was destroyed. It now looks for the service again.
- Reopening the Coroutine Inspector could draw with missing textures.
- The Coroutine Inspector resolved the service in a way that logged an error under Services 2.0 when it was missing. It now uses `TryGet`.

## Upgrading from 1.x

- Change `Run(owner, routine[, tag])` calls to `Run(routine, owner, tag)`. Owner and tag are optional.
- Change `RunDelayed(owner, action, delay[, tag])` calls to `RunDelayed(action, delaySeconds, delayTime, owner, tag)`. Pass `DelayTime.Scaled` to keep the old behaviour.
- Replace `GetActiveCoroutineCount()` with the `ActiveCoroutineCount` property.
- Replace `GetData()`, `GetData(owner)` and `GetData(tag)` with `GetData(list)`, then filter the list yourself if you need one owner or tag.
- Replace reads of `RoutineTypeName`, `StartedTime`, `StartedRealtime`, `ElapsedRealtime` and `TotalPauseDuration` with `RoutineName`, `ElapsedTime` and `ElapsedUnscaledTime`.
- Remove the `Assets/Services` copy from the project and list `com.angrykoala.services` 2.0.0 in `manifest.json`.
- Resolve the service in `Start` or later, not in `Awake`. The `DefaultExecutionOrder` attribute is gone.
