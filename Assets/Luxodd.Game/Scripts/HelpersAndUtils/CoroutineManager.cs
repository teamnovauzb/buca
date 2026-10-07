using System.Collections;
using UnityEngine;

namespace Luxodd.Game.Scripts.HelpersAndUtils
{
    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager _instanceInner;

        private static CoroutineManager _instance
        {
            get
            {
                if (_instanceInner == null)
                {
                    _instanceInner = Object.FindAnyObjectByType<CoroutineManager>();
                    if (_instanceInner == null)
                        throw new System.InvalidOperationException("Place the saved CoroutineManager in the scene before starting Luxodd services.");
                }
                return _instanceInner;
            }
        }
        
        private void Awake()
        {
            _instanceInner = this;
        }

        public static Coroutine StartCoroutineMethod(IEnumerator routine)
        {
            return _instance.StartCoroutine(routine); 
        }

        public static void StopCoroutineMethod(Coroutine routine)
        {
            _instance.StopCoroutine(routine);
        }

        public static void DelayedAction(float seconds, System.Action action)
        {
            _instance.StartCoroutine(DelayedActionInner(seconds, action) );
        }

        public static void NextFrameAction(int ftameCount, System.Action action)
        {
            _instance.StartCoroutine( _instance.NextFrameActionInner(ftameCount, action) );
        }

        private static IEnumerator DelayedActionInner(float delay, System.Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }

        private IEnumerator NextFrameActionInner(int frameCount, System.Action action)
        {
            while (frameCount > 0)
            {
                yield return new WaitForEndOfFrame();
                frameCount--;
            }
            
            action?.Invoke();
        }
    }
}
