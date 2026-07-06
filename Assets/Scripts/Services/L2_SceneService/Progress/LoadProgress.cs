using RedDust.Core;
using RedDust.Core.Events;
using UnityEngine;

namespace RedDust.GameScene
{
    /// <summary>
    /// Publishes LoadingProgressEvent. Supports weighted composite progress from
    /// multiple parallel loads (scene + asset labels).
    /// </summary>
    public class LoadProgress
    {
        private readonly EventHub _eventHub;
        private float[] _trackValues;
        private int _completedTracks;

        public LoadProgress(EventHub eventHub)
        {
            _eventHub = eventHub;
        }

        /// <summary>Publish a simple single-track progress update.</summary>
        public void Publish(string phase, float progress)
        {
            _eventHub?.Get<SceneProgressEvent>()?.Raise(new SLoadingProgress(phase, Mathf.Clamp01(progress)));
        }

        public void Clear()
        {
            _trackValues = null;
            _completedTracks = 0;
        }

        /// <summary>Start tracking N parallel loads (equal weight each).</summary>
        public void BeginComposite(int trackCount)
        {
            _trackValues = new float[trackCount];
            _completedTracks = 0;
        }

        /// <summary>Update a single track. Tracks are indexed 0..(trackCount-1).</summary>
        public void UpdateTrack(int track, float value)
        {
            if (_trackValues == null || track < 0 || track >= _trackValues.Length) return;
            _trackValues[track] = Mathf.Clamp01(value);
            if (value >= 1f)
                _completedTracks++;
        }

        /// <summary>Equal-weight average of all tracks.</summary>
        public float TotalProgress
        {
            get
            {
                if (_trackValues == null || _trackValues.Length == 0) return 0f;
                float sum = 0f;
                foreach (var v in _trackValues) sum += v;
                return sum / _trackValues.Length;
            }
        }

        public int TotalTracks => _trackValues?.Length ?? 0;
        public int CompletedTracks => _completedTracks;
        public bool AllTracksComplete => _completedTracks >= TotalTracks;
    }
}
