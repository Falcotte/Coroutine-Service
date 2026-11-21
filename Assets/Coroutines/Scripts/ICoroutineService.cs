using System;
using System.Collections;
using System.Collections.Generic;
using AngryKoala.Services;
using UnityEngine;

namespace AngryKoala.Coroutines
{
    public interface ICoroutineService : IService
    {
        int ActiveCoroutineCount { get; }

        /// <summary>
        /// Runs the routine on the service. With an owner, the routine ends when the owner is destroyed or its
        /// GameObject becomes inactive. Returns null when the routine finished before this call returned.
        /// </summary>
        Coroutine Run(IEnumerator routine, MonoBehaviour owner = null, string tag = null);

        /// <summary>
        /// Calls the action after the delay. A delay of zero or less calls it before this method returns.
        /// </summary>
        Coroutine RunDelayed(Action action, float delaySeconds, DelayTime delayTime = DelayTime.Scaled,
            MonoBehaviour owner = null, string tag = null);

        Coroutine RunNextFrame(Action action, MonoBehaviour owner = null, string tag = null);

        void Stop(Coroutine coroutine);

        void StopAll();
        void StopAll(MonoBehaviour owner);
        void StopAll(string tag);

        /// <summary>
        /// Clears the list and fills it with a snapshot of every running coroutine. Meant for tools.
        /// </summary>
        void GetData(List<CoroutineData> results);
    }
}
