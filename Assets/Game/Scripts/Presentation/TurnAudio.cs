using System.Collections.Generic;
using UnityEngine;

namespace LanternDepths.Presentation
{
    public sealed class TurnAudio : MonoBehaviour
    {
        private AudioSource source;
        private readonly Dictionary<EventKind, AudioClip> clips = new Dictionary<EventKind, AudioClip>();
        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            foreach (var kind in new[] { EventKind.Damaged, EventKind.PickedUp, EventKind.Used, EventKind.FloorChanged, EventKind.LevelUp, EventKind.Victory, EventKind.Telegraph })
            {
                int rate = 22050; var samples = new float[rate / 7]; float hz = kind == EventKind.Damaged ? 180 : kind == EventKind.Telegraph ? 100 : 400 + (int)kind * 60;
                for (int i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * hz * i / rate) * (1f - (float)i / samples.Length) * 0.18f;
                var clip = AudioClip.Create(kind.ToString(), samples.Length, 1, rate, false); clip.SetData(samples, 0); clips.Add(kind, clip);
            }
        }
        public void Play(GameEvent e, int volume)
        {
            if (volume > 0 && clips.TryGetValue(e.Kind, out var clip)) { source.pitch = e.Kind == EventKind.Damaged && e.ActorId == 0 ? 0.7f : 1f; source.PlayOneShot(clip, volume / 100f); }
        }
        private void OnDestroy() { foreach (var clip in clips.Values) Destroy(clip); }
    }
}
