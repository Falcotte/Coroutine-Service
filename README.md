# Coroutine Service

A robust, centralized **Coroutine Service** for Unity that enables starting, tagging, monitoring, and managing coroutines globally — with full runtime introspection in the **Coroutine Inspector** editor window.

This service replaces ad-hoc `StartCoroutine()` calls with a powerful, organized system that tracks ownership, tags, elapsed time, and allows bulk stopping or filtering directly in the Unity Editor.

---

## ✨ Features

- **Centralized coroutine management**
- **Supports tags** and **owners** for structured control
- **Automatic cleanup** when owners are destroyed
- **Elapsed time tracking** (both scaled and unscaled)
- **Delayed execution helpers**
- **Pause-aware elapsed time in Editor**
- **Inspector window** for live coroutine inspection, sorting, and stopping
- Integrates seamlessly with the **Angry Koala Service Locator**

---

## 🧩 Architecture Overview

### Core Components

#### `ICoroutineService`

Defines the interface that all coroutine services must implement:

```
public interface ICoroutineService : IService
{
    Coroutine Run(IEnumerator routine);
    Coroutine Run(IEnumerator routine, string tag);
    Coroutine Run(MonoBehaviour owner, IEnumerator routine);
    Coroutine Run(MonoBehaviour owner, IEnumerator routine, string tag);

    Coroutine RunDelayed(Action action, float delaySeconds);
    Coroutine RunDelayed(Action action, float delaySeconds, string tag);
    Coroutine RunDelayed(MonoBehaviour owner, Action action, float delaySeconds);
    Coroutine RunDelayed(MonoBehaviour owner, Action action, float delaySeconds, string tag);

    void Stop(Coroutine coroutine);
    void StopAll();
    void StopAll(MonoBehaviour owner);
    void StopAll(string tag);

    IReadOnlyList<CoroutineData> GetData();
    IReadOnlyList<CoroutineData> GetData(MonoBehaviour owner);
    IReadOnlyList<CoroutineData> GetData(string tag);

    int GetActiveCoroutineCount();
}
```

This provides a unified API for launching and managing coroutines from anywhere in your project.

#### `CoroutineService`

Implements the runtime logic of the service and integrates with Unity’s coroutine system.
Supports:

* Running coroutines globally or per-owner

* Tag-based grouping and filtering

* Safe stopping of individual, tagged, or owner-based coroutines

* Runtime introspection and elapsed time tracking

All active coroutines are stored with rich metadata (`CoroutineData`) for debugging, inspection, and analytics.

#### `CoroutineData`

Holds metadata about a running coroutine:

```
public sealed class CoroutineData
{
    public Coroutine Coroutine;
    public MonoBehaviour Owner;
    public string Tag;
    public string RoutineTypeName;

    public float StartedTime;
    public float StartedRealtime;
    public float ElapsedTime;
    public float ElapsedRealtime;
}
```

When running inside the Editor, it also tracks **pause duration** so elapsed realtime stays accurate across editor pauses.

#### `CoroutineInspectorEditorWindow`

A custom Unity Editor window for **visualizing and managing coroutines** live during Play Mode.

Open via:

```Menu → Angry Koala → Coroutines → Coroutine Inspector```

Features:

* Lists all active coroutines with columns for:

  * Owner

  * Tag

  * Routine Type

  * Elapsed Time / Realtime

* Search and filter by tag or routine name

* Sort by any column

* Stop individual or grouped coroutines

* Ping the owner object

* Auto-refresh (configurable)

---

## 🧠 Usage Example

### 1. Starting Coroutines

```
using AngryKoala.Coroutines;
using UnityEngine;

public class ExampleUsage : MonoBehaviour
{
    private void Start()
    {
        // Start a simple routine
        ServiceLocator.Get<ICoroutineService>().Run(MyRoutine());

        // Start with owner tracking
        ServiceLocator.Get<ICoroutineService>().Run(this, OwnerRoutine());

        // Start with a tag
        ServiceLocator.Get<ICoroutineService>().Run(this, TaggedRoutine(), "Gameplay");
    }

    private IEnumerator MyRoutine()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Routine completed!");
    }

    private IEnumerator OwnerRoutine()
    {
        while (true)
        {
            yield return null;
        }
    }

    private IEnumerator TaggedRoutine()
    {
        yield return new WaitForSeconds(3f);
    }
}
```

### 2. Stopping Coroutines

```
private void OnDisable()
{
    var service = ServiceLocator.Get<ICoroutineService>();

    // Stop all coroutines owned by this MonoBehaviour
    service.StopAll(this);

    // Or stop all coroutines with a given tag
    service.StopAll("Gameplay");
}
```

### 3. Using Delayed Actions

```
service.RunDelayed(() => Debug.Log("Executed after 2s"), 2f, "UI");
```

---

## 🧱 Extending the System

You can extend the service for advanced scheduling or visualization:

* Add **custom columns** to the inspector for your coroutine metadata.

* Integrate with profiling tools to measure coroutine load.

* Build **editor utilities** that use `ICoroutineService.GetData()` for runtime analytics or visual debugging.

---

## 🪲 Debugging Tips

* **Coroutines not visible?** Ensure Play Mode is active and the service is initialized (added to the scene).

* **Tag filtering not working?** Tags are case-insensitive but must be non-empty.

* **Elapsed time mismatch?** Check that your game time scale hasn’t changed — the inspector shows both scaled and realtime values.

* **Stop buttons not responding?** Verify `ICoroutineService` is registered with the Service Locator.