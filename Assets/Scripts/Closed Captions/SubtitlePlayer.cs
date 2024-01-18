using System;
using System.Collections;
using UnityEngine;

namespace Unity.Samples.Accessibility
{
    /// <summary>
    /// This class is used to play subtitles for the correct amount of time.
    /// It manages playback, pause, and stopping of subtitles.
    /// </summary>
    [AddComponentMenu("Accessibility/Subtitle Player")]
    class SubtitlePlayer : MonoBehaviour
    {
        public enum State
        {
            Stopped,
            Playing,
            Paused
        }

        Coroutine m_PlayCoroutine;
        int m_PlaybackTime = -1;
        int m_NextIndex = -1;
        State m_State = State.Stopped;

        public int time
        {
            get => m_PlaybackTime;
            set
            {
                if (m_PlaybackTime == value)
                    return;

                m_PlaybackTime = value;
                int nextIndex = -1;

                // Find the next item
                for (var i = 0; i < subtitle.items.Count; i++)
                {
                    var item = subtitle.items[i];

                    if (item.endTime.Milliseconds >= m_PlaybackTime)
                    {
                        nextIndex = i;
                        break;
                    }
                }

                m_NextIndex = nextIndex;
            }
        }

        public Subtitle subtitle;

        public State state
        {
            get => m_State;
            set
            {
                if (m_State == value)
                    return;

                m_State = value;
                stateChanged?.Invoke();
            }
        }

        public SubtitleItem currentItem
        {
            get => m_CurrentItem;
            private set
            {
                if (m_CurrentItem == value)
                    return;
                m_CurrentItem = value;
                currentItemChanged?.Invoke(m_CurrentItem);
            }
        }

        public event Action<SubtitleItem> currentItemChanged;
        public event Action stateChanged;
        SubtitleItem m_CurrentItem;

        void Awake()
        {
            Reset();
        }

        void Update()
        {
            if (state != State.Stopped)
            {
                if (m_PlayCoroutine == null)
                    StartPlaying();
                else
                {
                    if (state == State.Playing)
                        IncrementPlaybackTime();
                }
            }
        }

        public void Play(int timePosition = 0)
        {
            if (state != State.Stopped)
                return;
            state = State.Playing;
            time = timePosition;
        }

        public void Stop()
        {
            if (state == State.Stopped)
                return;
            StopCoroutine(m_PlayCoroutine);
            Reset();
        }

        void Reset()
        {
            state = State.Stopped;
            currentItem = null;
            m_PlayCoroutine = null;
            m_PlaybackTime = -1;
            m_NextIndex = -1;
        }

        void StartPlaying()
        {
            m_PlayCoroutine = StartCoroutine(PlaySubtitle());
        }

        void IncrementPlaybackTime()
        {
            m_PlaybackTime += (int) (Time.deltaTime * 1000);
        }

        bool IsInPlaybackRange(SubtitleItem item)
        {
            return item.startTime.Milliseconds <= m_PlaybackTime && item.endTime.Milliseconds >= m_PlaybackTime;
        }

        IEnumerator PlaySubtitle()
        {
            while (m_NextIndex >= 0 && m_NextIndex < subtitle.items.Count)
            {
                yield return PlayNextItem();
            }
            OnPlaybackFinished();
        }

        void OnPlaybackFinished()
        {
            state = State.Stopped;
            Reset();
        }

        IEnumerator PlayNextItem()
        {
            yield return new WaitUntil(() => IsInPlaybackRange(subtitle.items[m_NextIndex]));
            var item = subtitle.items[m_NextIndex];
            m_NextIndex++;
            currentItem = item;
            yield return new WaitUntil(() => !IsInPlaybackRange(item));
            currentItem = null;
        }
    }
}
