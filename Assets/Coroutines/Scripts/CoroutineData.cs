using UnityEngine;

namespace AngryKoala.Coroutines
{
    public readonly struct CoroutineData
    {
        public Coroutine Coroutine { get; }

        public MonoBehaviour Owner { get; }
        public string Tag { get; }

        public string RoutineName { get; }

        public float ElapsedTime { get; }
        public float ElapsedUnscaledTime { get; }

        public CoroutineData(Coroutine coroutine, MonoBehaviour owner, string tag, string routineName,
            float elapsedTime, float elapsedUnscaledTime)
        {
            Coroutine = coroutine;
            Owner = owner;
            Tag = tag;
            RoutineName = routineName;
            ElapsedTime = elapsedTime;
            ElapsedUnscaledTime = elapsedUnscaledTime;
        }
    }
}
