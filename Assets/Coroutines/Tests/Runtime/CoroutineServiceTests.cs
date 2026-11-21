using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AngryKoala.Services;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace AngryKoala.Coroutines.Tests
{
    public sealed class CoroutineServiceTests
    {
        private readonly List<GameObject> _gameObjects = new();

        private CoroutineService _service;

        private int _stepCount;

        private const string _tag = "test-tag";
        private const string _otherTag = "other-tag";

        [SetUp]
        public void SetUp()
        {
            _service = Create<CoroutineService>("CoroutineService");
            _stepCount = 0;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                if (gameObject != null)
                {
                    Object.Destroy(gameObject);
                }
            }

            _gameObjects.Clear();

            yield return null;
        }

        [UnityTest]
        public IEnumerator Run_StepsRoutineEveryFrame()
        {
            _service.Run(CountFrames());

            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(GetActiveCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Run_RoutineThatCompletes_LeavesNoRecord()
        {
            _service.Run(WaitFrames(1));

            Assert.That(GetActiveCount(), Is.EqualTo(1));

            yield return null;
            yield return null;

            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Run_NullRoutine_ReturnsNull()
        {
            Assert.That(_service.Run(null), Is.Null);
            Assert.That(GetActiveCount(), Is.EqualTo(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Run_RoutineThatFinishesAtOnce_LeavesNoRecord()
        {
            Coroutine coroutine = _service.Run(FinishAtOnce());

            Assert.That(coroutine, Is.Null);
            Assert.That(_stepCount, Is.EqualTo(1));
            Assert.That(GetActiveCount(), Is.EqualTo(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RunDelayed_ZeroDelay_RunsActionAndLeavesNoRecord()
        {
            bool actionRan = false;

            _service.RunDelayed(() => actionRan = true, 0f);

            Assert.That(actionRan, Is.True);
            Assert.That(GetActiveCount(), Is.EqualTo(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RunDelayed_RunsActionAfterDelay()
        {
            bool actionRan = false;

            _service.RunDelayed(() => actionRan = true, 0.1f);

            yield return null;

            Assert.That(actionRan, Is.False);

            yield return new WaitForSeconds(0.3f);

            Assert.That(actionRan, Is.True);
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Stop_StopsRoutineAndRemovesRecord()
        {
            Coroutine coroutine = _service.Run(CountFrames());

            yield return null;

            _service.Stop(coroutine);
            int stepCountWhenStopped = _stepCount;

            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountWhenStopped));
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator StopAll_StopsEveryRoutine()
        {
            _service.Run(CountFrames());
            _service.Run(CountFrames());

            yield return null;

            _service.StopAll();
            int stepCountWhenStopped = _stepCount;

            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountWhenStopped));
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator StopAllWithOwner_StopsOnlyThatOwnersRoutines()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");
            PlayModeTestOwner otherOwner = Create<PlayModeTestOwner>("OtherOwner");

            RunOwned(CountFrames(), owner);
            RunOwned(WaitFrames(100), otherOwner);

            yield return null;

            _service.StopAll(owner);
            int stepCountWhenStopped = _stepCount;

            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountWhenStopped));
            Assert.That(GetActiveCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator StopAllWithTag_StopsOnlyThatTagsRoutines()
        {
            RunTagged(CountFrames(), _tag);
            RunTagged(WaitFrames(100), _otherTag);

            yield return null;

            _service.StopAll(_tag);
            int stepCountWhenStopped = _stepCount;

            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountWhenStopped));
            Assert.That(GetActiveCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator OwnerDestroyed_StopsItsRoutinesAndRemovesRecords()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");

            RunOwned(CountFrames(), owner);

            yield return null;

            Object.Destroy(owner.gameObject);

            yield return null;
            yield return null;

            int stepCountAfterDestroy = _stepCount;

            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountAfterDestroy));
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator OwnerGameObjectDeactivated_StopsItsRoutinesAndRemovesRecords()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");

            RunOwned(CountFrames(), owner);

            yield return null;

            owner.gameObject.SetActive(false);

            yield return null;
            yield return null;

            int stepCountAfterDeactivate = _stepCount;

            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountAfterDeactivate));
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator OwnerComponentDisabled_KeepsItsRoutinesRunning()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");

            RunOwned(CountFrames(), owner);

            yield return null;

            owner.enabled = false;
            int stepCountWhenDisabled = _stepCount;

            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.GreaterThan(stepCountWhenDisabled));
            Assert.That(GetActiveCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ServiceGameObjectDeactivated_ClearsRecords()
        {
            _service.Run(CountFrames());
            RunTagged(CountFrames(), _tag);

            yield return null;

            _service.gameObject.SetActive(false);

            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RoutineThatThrows_EndsAndLeavesNoRecord()
        {
            LogAssert.ignoreFailingMessages = true;

            _service.Run(ThrowOnSecondStep());

            yield return null;
            yield return null;

            LogAssert.ignoreFailingMessages = false;

            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator OwnerDestroyed_RemovesRecordOfRoutineThatIsWaiting()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");

            RunOwned(WaitSeconds(100f), owner);

            yield return null;

            Object.Destroy(owner.gameObject);

            yield return null;
            yield return null;

            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Run_OwnerInactive_DoesNotStart()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");
            owner.gameObject.SetActive(false);

            LogAssert.Expect(LogType.Warning, new Regex("destroyed or inactive"));

            Coroutine coroutine = RunOwned(CountFrames(), owner);

            Assert.That(coroutine, Is.Null);
            Assert.That(_stepCount, Is.EqualTo(0));
            Assert.That(GetActiveCount(), Is.EqualTo(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Run_ServiceDisabled_DoesNotStart()
        {
            _service.enabled = false;

            LogAssert.Expect(LogType.Warning, new Regex("is disabled"));

            Coroutine coroutine = _service.Run(CountFrames());

            Assert.That(coroutine, Is.Null);
            Assert.That(_stepCount, Is.EqualTo(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ServiceComponentDisabled_StopsRoutinesAndClearsRecords()
        {
            _service.Run(CountFrames());

            yield return null;

            _service.enabled = false;
            int stepCountWhenDisabled = _stepCount;

            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.EqualTo(stepCountWhenDisabled));
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Stop_FromInsideTheRoutine_RemovesRecord()
        {
            Coroutine coroutine = null;
            coroutine = _service.Run(StopSelfOnSecondStep(() => coroutine));

            yield return null;
            yield return null;
            yield return null;

            Assert.That(_stepCount, Is.EqualTo(1));
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Run_FromInsideARoutine_TracksBoth()
        {
            _service.Run(StartAnother());

            yield return null;

            Assert.That(GetActiveCount(), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RunDelayed_Unscaled_RunsWhileTimeScaleIsZero()
        {
            bool scaledActionRan = false;
            bool unscaledActionRan = false;

            float timeScale = Time.timeScale;
            Time.timeScale = 0f;

            try
            {
                _service.RunDelayed(() => scaledActionRan = true, 0.05f);
                _service.RunDelayed(() => unscaledActionRan = true, 0.05f, DelayTime.Unscaled);

                yield return new WaitForSecondsRealtime(0.3f);
            }
            finally
            {
                Time.timeScale = timeScale;
            }

            Assert.That(unscaledActionRan, Is.True);
            Assert.That(scaledActionRan, Is.False);
            Assert.That(GetActiveCount(), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RunDelayed_NullAction_ReturnsNull()
        {
            Assert.That(_service.RunDelayed(null, 1f), Is.Null);
            Assert.That(GetActiveCount(), Is.EqualTo(0));

            yield return null;
        }

        [UnityTest]
        public IEnumerator RunNextFrame_RunsActionOneFrameLater()
        {
            bool actionRan = false;

            _service.RunNextFrame(() => actionRan = true);

            Assert.That(actionRan, Is.False);
            Assert.That(GetActiveCount(), Is.EqualTo(1));

            yield return null;
            yield return null;

            Assert.That(actionRan, Is.True);
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RunDelayed_OwnerDestroyedBeforeDelayEnds_DoesNotRunAction()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");
            bool actionRan = false;

            _service.RunDelayed(() => actionRan = true, 0.1f, owner: owner);

            yield return null;

            Object.Destroy(owner.gameObject);

            yield return new WaitForSeconds(0.3f);

            Assert.That(actionRan, Is.False);
            Assert.That(GetActiveCount(), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator RoutineThatThrows_LogsOwnerTagAndRoutineName()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");

            _service.Run(ThrowOnSecondStep(), owner, _tag);

            LogAssert.Expect(LogType.Error, new Regex("ThrowOnSecondStep.*owner: Owner.*tag: test-tag"));
            LogAssert.Expect(LogType.Exception, new Regex("Test exception"));

            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetData_FillsListWithRunningRoutines()
        {
            PlayModeTestOwner owner = Create<PlayModeTestOwner>("Owner");
            var results = new List<CoroutineData> { default };

            Coroutine coroutine = _service.Run(CountFrames(), owner, _tag);

            yield return null;
            yield return null;

            _service.GetData(results);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Coroutine, Is.SameAs(coroutine));
            Assert.That(results[0].Owner, Is.SameAs(owner));
            Assert.That(results[0].Tag, Is.EqualTo(_tag));
            Assert.That(results[0].RoutineName, Is.EqualTo(nameof(CountFrames)));
            Assert.That(results[0].ElapsedUnscaledTime, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator GetData_NullList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _service.GetData(null));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Service_RegistersAndDeregistersWithServiceLocator()
        {
            Assert.That(ServiceLocator.TryGet(out ICoroutineService registeredService), Is.True);
            Assert.That(registeredService, Is.SameAs(_service));

            Object.Destroy(_service.gameObject);

            yield return null;

            Assert.That(ServiceLocator.IsRegistered<ICoroutineService>(), Is.False);
        }

        #region Utility

        private T Create<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            _gameObjects.Add(gameObject);

            return gameObject.AddComponent<T>();
        }

        private int GetActiveCount() => _service.ActiveCoroutineCount;

        private Coroutine RunOwned(IEnumerator routine, MonoBehaviour owner) => _service.Run(routine, owner);

        private Coroutine RunTagged(IEnumerator routine, string tag) => _service.Run(routine, tag: tag);

        private IEnumerator CountFrames()
        {
            while (true)
            {
                _stepCount++;
                yield return null;
            }
        }

        private IEnumerator WaitFrames(int frameCount)
        {
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
            }
        }

        private IEnumerator WaitSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator StopSelfOnSecondStep(Func<Coroutine> getCoroutine)
        {
            yield return null;

            _stepCount++;
            _service.Stop(getCoroutine.Invoke());

            yield return null;

            _stepCount++;
        }

        private IEnumerator StartAnother()
        {
            _service.Run(CountFrames());

            yield return WaitFrames(100);
        }

        private IEnumerator FinishAtOnce()
        {
            _stepCount++;
            yield break;
        }

        private IEnumerator ThrowOnSecondStep()
        {
            yield return null;

            throw new InvalidOperationException("Test exception");
        }

        #endregion
    }
}
