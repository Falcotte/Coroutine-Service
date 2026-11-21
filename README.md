# Angry Koala Coroutines

A coroutine service for Unity. Systems start coroutines through one service, optionally tagged and tied to an owner `MonoBehaviour`. The service keeps a record of each running coroutine, stops the ones whose owner goes away, and can stop them in bulk by owner or tag. The Coroutine Inspector shows the running coroutines live in Play Mode. The module depends on the Services module to find the service.

**Use it for**

- Timing that the game's rules depend on, for example a cooldown or a delayed spawn.
- Sequences that must be stopped by owner or tag, for example everything an enemy started when it dies.
- Seeing which coroutines are running, and how long they have run, while the game is in Play Mode.

**Do not use it for**

- Motion for show, for example a fade or a bounce. Use a tween (DOTween) for that. A tween is not a coroutine, and the Coroutine Service is not built to run many of them.

## Install

Add the package to the project's `Packages/manifest.json`. The Services module must be listed too:

```json
"com.angrykoala.coroutines": "https://github.com/Falcotte/Coroutine-Service.git?path=/Assets/Coroutines#v2.0.0",
"com.angrykoala.services": "https://github.com/Falcotte/Service-Locator.git?path=/Assets/Services#v2.0.0"
```

Unity does not resolve git dependencies between packages. Each AngryKoala module must be listed in the project's own `manifest.json`.

Minimum Unity version: 6000.3 (from `package.json`). Developed and tested on Unity 6000.6.

## Public interface

```csharp
public interface ICoroutineService : IService
{
    // The number of coroutines the service is tracking.
    int ActiveCoroutineCount { get; }

    // Runs the routine. With an owner, it ends when the owner is destroyed or its GameObject is deactivated. Returns null if the routine finished at once or was not started.
    Coroutine Run(IEnumerator routine, MonoBehaviour owner = null, string tag = null);

    // Calls the action after the delay, in scaled or unscaled seconds. A delay of zero or less calls it before the method returns.
    Coroutine RunDelayed(Action action, float delaySeconds, DelayTime delayTime = DelayTime.Scaled, MonoBehaviour owner = null, string tag = null);

    // Calls the action on the next frame.
    Coroutine RunNextFrame(Action action, MonoBehaviour owner = null, string tag = null);

    // Stops one coroutine and removes its record.
    void Stop(Coroutine coroutine);

    // Stops every coroutine and removes every record.
    void StopAll();

    // Stops every coroutine started with the owner.
    void StopAll(MonoBehaviour owner);

    // Stops every coroutine started with the tag. Null or empty tags do nothing.
    void StopAll(string tag);

    // Clears the list and fills it with a snapshot of every running coroutine. Meant for tools.
    void GetData(List<CoroutineData> results);
}

public readonly struct CoroutineData
{
    // The Unity coroutine handle.
    public Coroutine Coroutine { get; }

    // The owner passed to Run, or null.
    public MonoBehaviour Owner { get; }

    // The tag passed to Run, or null.
    public string Tag { get; }

    // The name of the routine: the iterator method, or the method that declares the action.
    public string RoutineName { get; }

    // Seconds since the routine started, in scaled game time.
    public float ElapsedTime { get; }

    // Seconds since the routine started, in unscaled time.
    public float ElapsedUnscaledTime { get; }
}

public enum DelayTime
{
    // Counts with Time.deltaTime. Stops while Time.timeScale is 0.
    Scaled,

    // Counts with Time.unscaledDeltaTime. Keeps counting while Time.timeScale is 0.
    Unscaled
}
```

## Example

A system that starts a spawn timer when it is enabled and stops it when the enemy dies. The owner is the enemy, so the timer also ends if the enemy is destroyed.

```csharp
using System.Collections;
using AngryKoala.Coroutines;
using AngryKoala.Services;
using UnityEngine;

public sealed class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float _spawnInterval = 2f;

    private ICoroutineService _coroutineService;

    private void Start()
    {
        _coroutineService = ServiceLocator.Get<ICoroutineService>();

        _coroutineService.Run(SpawnLoop(), this, "Spawning");
        _coroutineService.RunDelayed(StartBossIntro, 3f, DelayTime.Unscaled, this, "Intro");
    }

    private void OnDisable()
    {
        _coroutineService?.StopAll(this);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_spawnInterval);
            Spawn();
        }
    }

    private void Spawn()
    {
    }

    private void StartBossIntro()
    {
    }
}
```

## Rules

- An owner's coroutines end when the owner is destroyed or its GameObject becomes inactive in the hierarchy. Disabling only the component does not end them.
- `Run`, `RunDelayed` and `RunNextFrame` with a destroyed or inactive owner do not start, and log a warning.
- Disabling or destroying the service stops every coroutine and clears its records. A disabled service refuses `Run` and the other start methods, and logs a warning.
- A routine that finishes before `Run` returns gives back null, and leaves no record.
- `RunDelayed` with a delay of zero or less calls the action before it returns.
- `DelayTime.Unscaled` keeps counting while `Time.timeScale` is 0.
- A routine that throws is logged with its owner, tag and routine name, and then ends. The other coroutines keep running.
- A routine that yields another `IEnumerator` (`yield return Inner();`) is stepped by the service, so the owner rule and the exception rule cover the nested routine too.
- `Stop` does not resume a routine that is waiting on the stopped coroutine with `yield return`. That is Unity's behaviour.
- Stopping a routine does not run its `finally` blocks. That is Unity's behaviour too.
- `GetData` fills the list you pass in. It is meant for tools, not for gameplay code.
- Start order: the service registers itself in `Awake`. Callers resolve it in `Start` or later, with `ServiceLocator.Get<ICoroutineService>()`. The service has no `DefaultExecutionOrder`.

## What it needs

- Unity 6000.3 or later.
- The Services module, `com.angrykoala.services` 2.0.0, listed in the project's `manifest.json`.
- Tests need `com.unity.test-framework`.

## Coroutine Inspector

Open it from **Angry Koala > Coroutines > Coroutine Inspector**. It works in Play Mode only.

The window lists each running coroutine with its owner, tag, routine name, and scaled and unscaled elapsed time. It has:

- A search box for routine name, tag and owner name, and a tag filter.
- Sorting by any column.
- Per-row **Stop** and **Ping Owner** buttons.
- **Stop All** and **Stop All With Tag** buttons.
- Refresh and an Auto-refresh toggle. Auto-refresh is on by default. The search, filter, sort and toggle settings are remembered in EditorPrefs.

## Tests

The module has Play Mode tests in `Assets/Coroutines/Tests/Runtime`. There is no Edit Mode test assembly, because coroutines run only in Play Mode. Run them from the Test Runner, in Play Mode.

## Status

Tested: 35 Play Mode tests pass on Unity 6000.6.4f1.

Not tested: Unity 6000.3, and player builds. Neither has been verified.
