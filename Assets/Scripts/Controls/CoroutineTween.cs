using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Sample.Controls
{
    /// <summary>
    /// Base interface for tweeners.
    /// We use an interface instead of an abstract class as we want the tweens to be structs.
    /// </summary>
    interface ITweenValue
    {
        void TweenValue(float floatPercentage);

        bool ignoreTimeScale { get; }
        float duration { get; }

        bool ValidTarget();
    }

    /// <summary>
    /// Color tween class that receives the TweenValue callback and then sets the value on the target.
    /// </summary>
    struct ColorTween : ITweenValue
    {
        public enum ColorTweenMode
        {
            All,
            RGB,
            Alpha
        }

        public class ColorTweenCallback : UnityEvent<Color> { }

        ColorTweenCallback m_Target;

        public Color startColor { get; set; }
        public Color targetColor { get; set; }
        public ColorTweenMode tweenMode { get; set; }

        public float duration { get; set; }
        public bool ignoreTimeScale { get; set; }

        public void TweenValue(float floatPercentage)
        {
            if (!ValidTarget())
            {
                return;
            }

            var newColor = Color.Lerp(startColor, targetColor, floatPercentage);

            switch (tweenMode)
            {
                case ColorTweenMode.Alpha:
                {
                    newColor.r = startColor.r;
                    newColor.g = startColor.g;
                    newColor.b = startColor.b;
                    break;
                }
                case ColorTweenMode.RGB:
                {
                    newColor.a = startColor.a;
                    break;
                }
            }

            m_Target.Invoke(newColor);
        }

        public void AddOnChangedCallback(UnityAction<Color> callback)
        {
            m_Target ??= new ColorTweenCallback();

            m_Target.AddListener(callback);
        }

        public bool GetIgnoreTimescale()
        {
            return ignoreTimeScale;
        }

        public float GetDuration()
        {
            return duration;
        }

        public bool ValidTarget()
        {
            return m_Target != null;
        }
    }

    /// <summary>
    /// Float tween class that receives the TweenValue callback and then sets the value on the target.
    /// </summary>
    struct FloatTween : ITweenValue
    {
        public class FloatTweenCallback : UnityEvent<float> { }

        FloatTweenCallback m_Target;

        public float startValue { get; set; }
        public float targetValue { get; set; }

        public float duration { get; set; }
        public bool ignoreTimeScale { get; set; }

        public void TweenValue(float floatPercentage)
        {
            if (!ValidTarget())
            {
                return;
            }

            var newValue = Mathf.Lerp(startValue, targetValue, floatPercentage);
            m_Target.Invoke(newValue);
        }

        public void AddOnChangedCallback(UnityAction<float> callback)
        {
            m_Target ??= new FloatTweenCallback();

            m_Target.AddListener(callback);
        }

        public bool GetIgnoreTimescale()
        {
            return ignoreTimeScale;
        }

        public float GetDuration()
        {
            return duration;
        }

        public bool ValidTarget()
        {
            return m_Target != null;
        }
    }

    /// <summary>
    /// Tween runner that executes the given tween.
    /// The coroutine will live within the given behaviour container.
    /// </summary>
    class TweenRunner<T> where T : struct, ITweenValue
    {
        MonoBehaviour m_CoroutineContainer;
        IEnumerator m_Tween;

        /// <summary>
        /// Utility function for starting the tween.
        /// </summary>
        static IEnumerator Start(T tweenInfo)
        {
            if (!tweenInfo.ValidTarget())
            {
                yield break;
            }

            var elapsedTime = 0f;
            while (elapsedTime < tweenInfo.duration)
            {
                elapsedTime += tweenInfo.ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                var percentage = Mathf.Clamp01(elapsedTime / tweenInfo.duration);
                tweenInfo.TweenValue(percentage);
                yield return null;
            }

            tweenInfo.TweenValue(1f);
        }

        public void Init(MonoBehaviour coroutineContainer)
        {
            m_CoroutineContainer = coroutineContainer;
        }

        public void StartTween(T info)
        {
            if (m_CoroutineContainer == null)
            {
                Debug.LogWarning("Coroutine container not configured... did you forget to call Init?");
                return;
            }

            StopTween();

            if (!m_CoroutineContainer.gameObject.activeInHierarchy)
            {
                info.TweenValue(1.0f);
                return;
            }

            m_Tween = Start(info);
            m_CoroutineContainer.StartCoroutine(m_Tween);
        }

        public void StopTween()
        {
            if (m_Tween != null)
            {
                m_CoroutineContainer.StopCoroutine(m_Tween);
                m_Tween = null;
            }
        }
    }
}
