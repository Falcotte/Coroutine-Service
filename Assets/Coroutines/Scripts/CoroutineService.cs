using System;
using System.Collections;
using System.Collections.Generic;
using AngryKoala.Services;
using UnityEngine;

namespace AngryKoala.Coroutines
{
    public sealed class CoroutineService : BaseService<ICoroutineService>, ICoroutineService
    {
        public int ActiveCoroutineCount => _trackedRoutines.Count;

        private readonly List<TrackedRoutine> _trackedRoutines = new();
        private readonly Dictionary<Coroutine, TrackedRoutine> _trackedRoutinesByCoroutine = new();

        private const string _delayedRoutineNamePrefix = "Delayed: ";
        private const string _nextFrameRoutineNamePrefix = "Next frame: ";

        private void OnDisable()
        {
            StopAll();
        }

        public Coroutine Run(IEnumerator routine, MonoBehaviour owner = null, string tag = null)
        {
            if (routine == null)
            {
                return null;
            }

            return RunInternal(routine, owner, tag, GetDisplayName(routine.GetType().Name));
        }

        public Coroutine RunDelayed(Action action, float delaySeconds, DelayTime delayTime = DelayTime.Scaled,
            MonoBehaviour owner = null, string tag = null)
        {
            if (action == null)
            {
                return null;
            }

            string routineName = _delayedRoutineNamePrefix + GetDisplayName(action.Method.Name);

            return RunInternal(DelayRoutine(action, delaySeconds, delayTime), owner, tag, routineName);
        }

        public Coroutine RunNextFrame(Action action, MonoBehaviour owner = null, string tag = null)
        {
            if (action == null)
            {
                return null;
            }

            string routineName = _nextFrameRoutineNamePrefix + GetDisplayName(action.Method.Name);

            return RunInternal(NextFrameRoutine(action), owner, tag, routineName);
        }

        public void Stop(Coroutine coroutine)
        {
            if (coroutine == null)
            {
                return;
            }

            if (_trackedRoutinesByCoroutine.TryGetValue(coroutine, out TrackedRoutine trackedRoutine))
            {
                StopInternal(trackedRoutine);
            }
        }

        public void StopAll()
        {
            StopAllCoroutines();

            for (int i = 0; i < _trackedRoutines.Count; i++)
            {
                _trackedRoutines[i].IsFinished = true;
            }

            _trackedRoutines.Clear();
            _trackedRoutinesByCoroutine.Clear();
        }

        public void StopAll(MonoBehaviour owner)
        {
            if (ReferenceEquals(owner, null))
            {
                return;
            }

            for (int i = _trackedRoutines.Count - 1; i >= 0; i--)
            {
                TrackedRoutine trackedRoutine = _trackedRoutines[i];

                if (ReferenceEquals(trackedRoutine.Owner, owner))
                {
                    StopInternal(trackedRoutine);
                }
            }
        }

        public void StopAll(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }

            for (int i = _trackedRoutines.Count - 1; i >= 0; i--)
            {
                TrackedRoutine trackedRoutine = _trackedRoutines[i];

                if (trackedRoutine.Tag == tag)
                {
                    StopInternal(trackedRoutine);
                }
            }
        }

        public void GetData(List<CoroutineData> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();

            float time = Time.time;
            float unscaledTime = Time.unscaledTime;

            for (int i = 0; i < _trackedRoutines.Count; i++)
            {
                TrackedRoutine trackedRoutine = _trackedRoutines[i];

                results.Add(new CoroutineData(trackedRoutine.Coroutine, trackedRoutine.Owner, trackedRoutine.Tag,
                    trackedRoutine.RoutineName, Mathf.Max(0f, time - trackedRoutine.StartedTime),
                    Mathf.Max(0f, unscaledTime - trackedRoutine.StartedUnscaledTime)));
            }
        }

        #region Internal Methods

        private Coroutine RunInternal(IEnumerator routine, MonoBehaviour owner, string tag, string routineName)
        {
            if (!isActiveAndEnabled)
            {
                Debug.LogWarning($"{nameof(CoroutineService)} is disabled, so '{routineName}' was not started", this);
                return null;
            }

            bool hasOwner = !ReferenceEquals(owner, null);

            if (hasOwner && !IsOwnerActive(owner))
            {
                Debug.LogWarning($"The owner of '{routineName}' is destroyed or inactive, so it was not started",
                    this);
                return null;
            }

            var trackedRoutine = new TrackedRoutine(this, routine, owner, hasOwner,
                string.IsNullOrEmpty(tag) ? null : tag, routineName);

            _trackedRoutines.Add(trackedRoutine);

            Coroutine coroutine = StartCoroutine(trackedRoutine);

            if (trackedRoutine.IsFinished)
            {
                return null;
            }

            trackedRoutine.Coroutine = coroutine;
            _trackedRoutinesByCoroutine[coroutine] = trackedRoutine;

            return coroutine;
        }

        private void StopInternal(TrackedRoutine trackedRoutine)
        {
            if (trackedRoutine.IsFinished)
            {
                return;
            }

            if (trackedRoutine.Coroutine != null)
            {
                StopCoroutine(trackedRoutine.Coroutine);
            }

            RemoveTrackedRoutine(trackedRoutine);
        }

        #endregion

        private void RemoveTrackedRoutine(TrackedRoutine trackedRoutine)
        {
            if (trackedRoutine.IsFinished)
            {
                return;
            }

            trackedRoutine.IsFinished = true;

            _trackedRoutines.Remove(trackedRoutine);

            if (trackedRoutine.Coroutine != null)
            {
                _trackedRoutinesByCoroutine.Remove(trackedRoutine.Coroutine);
            }
        }

        private void LogRoutineException(TrackedRoutine trackedRoutine, Exception exception)
        {
            string ownerName = trackedRoutine.Owner != null ? trackedRoutine.Owner.name : "none";
            string tag = trackedRoutine.Tag ?? "none";
            UnityEngine.Object context = trackedRoutine.Owner != null ? trackedRoutine.Owner : this;

            Debug.LogError(
                $"Coroutine '{trackedRoutine.RoutineName}' (owner: {ownerName}, tag: {tag}) threw and was ended",
                context);
            Debug.LogException(exception, context);
        }

        private void Update()
        {
            StopRoutinesOfInactiveOwners();
        }

        private void StopRoutinesOfInactiveOwners()
        {
            for (int i = _trackedRoutines.Count - 1; i >= 0; i--)
            {
                TrackedRoutine trackedRoutine = _trackedRoutines[i];

                if (trackedRoutine.HasOwner && !IsOwnerActive(trackedRoutine.Owner))
                {
                    StopInternal(trackedRoutine);
                }
            }
        }

        #region Utility

        private static bool IsOwnerActive(MonoBehaviour owner)
        {
            return owner != null && owner.gameObject.activeInHierarchy;
        }

        private static string GetDisplayName(string compilerName)
        {
            int nameEnd = compilerName.IndexOf('>');

            if (compilerName.StartsWith("<", StringComparison.Ordinal) && nameEnd > 1)
            {
                return compilerName.Substring(1, nameEnd - 1);
            }

            return compilerName;
        }

        private static IEnumerator DelayRoutine(Action action, float delaySeconds, DelayTime delayTime)
        {
            float remainingSeconds = delaySeconds;

            while (remainingSeconds > 0f)
            {
                yield return null;

                remainingSeconds -= delayTime == DelayTime.Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }

            action.Invoke();
        }

        private static IEnumerator NextFrameRoutine(Action action)
        {
            yield return null;

            action.Invoke();
        }

        private sealed class TrackedRoutine : IEnumerator
        {
            public object Current => _routine.Current;

            public Coroutine Coroutine { get; set; }
            public bool IsFinished { get; set; }

            public MonoBehaviour Owner { get; }
            public bool HasOwner { get; }
            public string Tag { get; }
            public string RoutineName { get; }

            public float StartedTime { get; }
            public float StartedUnscaledTime { get; }

            private readonly CoroutineService _service;
            private readonly IEnumerator _routine;

            public TrackedRoutine(CoroutineService service, IEnumerator routine, MonoBehaviour owner, bool hasOwner,
                string tag, string routineName)
            {
                _service = service;
                _routine = routine;

                Owner = owner;
                HasOwner = hasOwner;
                Tag = tag;
                RoutineName = routineName;

                StartedTime = Time.time;
                StartedUnscaledTime = Time.unscaledTime;
            }

            public bool MoveNext()
            {
                if (IsFinished)
                {
                    return false;
                }

                if (HasOwner && !IsOwnerActive(Owner))
                {
                    _service.RemoveTrackedRoutine(this);
                    return false;
                }

                bool hasNext = false;

                try
                {
                    hasNext = _routine.MoveNext();
                }
                catch (Exception exception)
                {
                    _service.LogRoutineException(this, exception);
                }

                if (!hasNext)
                {
                    _service.RemoveTrackedRoutine(this);
                    return false;
                }

                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        #endregion
    }
}
