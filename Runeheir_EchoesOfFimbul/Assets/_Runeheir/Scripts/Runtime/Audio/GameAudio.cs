using System.Collections.Generic;
using System.Threading;
using Runeheir.Combat;
using Runeheir.Field;
using Runeheir.Player;
using Runeheir.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Runeheir.Audio
{
    /// <summary>
    /// The game's sound: procedural music chosen from the map (and a boss track when a boss has you in its sights),
    /// crossfaded, plus a pool of 3D voices for <see cref="Sfx"/>. Lives across scenes; created on the first scene load.
    /// Music is composed on a worker thread the first time a mood is needed. Batch-mode runs (tests, servers) stay silent.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const int Voices = 20;
        private const float Crossfade = 2.5f;
        private const string MusicKey = "rh_volume_music";
        private const string SfxKey = "rh_volume_sfx";

        private readonly Dictionary<Sfx, AudioClip> _sfx = new Dictionary<Sfx, AudioClip>();
        private readonly Dictionary<Sfx, float> _lastPlayed = new Dictionary<Sfx, float>();
        private readonly Dictionary<MusicMood, AudioClip> _music = new Dictionary<MusicMood, AudioClip>();
        private readonly Dictionary<MusicMood, float[]> _composed = new Dictionary<MusicMood, float[]>();
        private readonly HashSet<MusicMood> _composing = new HashSet<MusicMood>();

        private AudioSource[] _musicSources;
        private AudioSource[] _voices;
        private int _activeMusic;
        private int _nextVoice;
        private MusicMood? _mood;
        private float _nextMoodCheck;
        private float _nextListenerCheck;

        public static GameAudio Instance { get; private set; }

        /// <summary>0..1, saved between sessions.</summary>
        public static float MusicVolume { get; private set; } = 0.55f;

        public static float SfxVolume { get; private set; } = 0.8f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Instance != null || Application.isBatchMode)
            {
                return;
            }

            var host = new GameObject("GameAudio");
            DontDestroyOnLoad(host);
            Instance = host.AddComponent<GameAudio>();
        }

        // ---------------------------------------------------------------- API
        /// <summary>Plays a sound effect, at a point in the world (3D) or in the listener's ears (UI).</summary>
        public static void Play(Sfx sfx, Vector3? at = null, float volume = 1f, float pitch = 1f)
        {
            if (Instance != null)
            {
                Instance.PlayInternal(sfx, at, volume, pitch);
            }
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxKey, SfxVolume);
        }

        /// <summary>The music for a map: its theme decides (towns, meadows, forests, the cold coasts, dungeons, arenas).</summary>
        public static MusicMood MoodFor(MapDefinition map)
        {
            if (map == null)
            {
                return MusicMood.Title;
            }

            switch (map.Theme)
            {
                case MapTheme.Town: return MusicMood.Town;
                case MapTheme.Meadow: return MusicMood.Meadow;
                case MapTheme.BirchForest: return MusicMood.Forest;
                case MapTheme.Crypt:
                case MapTheme.IceCavern: return MusicMood.Dungeon;
                case MapTheme.Arena: return MusicMood.Arena;
                case MapTheme.Castle: return MusicMood.Arena;
                case MapTheme.Desert: return MusicMood.Desert;
                case MapTheme.Volcano: return MusicMood.Volcano;
                case MapTheme.Jungle: return MusicMood.Jungle;
                case MapTheme.Coast: return MusicMood.Coast;
                case MapTheme.SandTomb: return MusicMood.Dungeon;
                default: return MusicMood.Cold;
            }
        }

        // ---------------------------------------------------------------- lifecycle
        private void Awake()
        {
            MusicVolume = PlayerPrefs.GetFloat(MusicKey, MusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxKey, SfxVolume);
            _musicSources = new AudioSource[2];
            for (int i = 0; i < 2; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.loop = true;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.volume = 0f;
                _musicSources[i] = source;
            }

            _voices = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                var voice = new GameObject("Voice" + i).AddComponent<AudioSource>();
                voice.transform.SetParent(transform, false);
                voice.playOnAwake = false;
                voice.rolloffMode = AudioRolloffMode.Linear;
                voice.minDistance = 6f;
                voice.maxDistance = 48f;
                voice.dopplerLevel = 0f;
                _voices[i] = voice;
            }

            // Title and town are wanted first; compose them right away.
            Compose(MusicMood.Title);
            Compose(MusicMood.Town);
        }

        private void Update()
        {
            FollowListener();
            AdoptComposed();

            if (Time.unscaledTime >= _nextMoodCheck)
            {
                _nextMoodCheck = Time.unscaledTime + 0.75f;
                var wanted = WantedMood();
                if (wanted != _mood)
                {
                    _mood = wanted;
                    Compose(wanted);
                    StartTrack(wanted);
                }
            }

            // Crossfade: the active source rises to the music volume, the other falls silent.
            float step = Time.unscaledDeltaTime / Crossfade;
            for (int i = 0; i < 2; i++)
            {
                var source = _musicSources[i];
                float target = i == _activeMusic && source.clip != null ? MusicVolume * 0.7f : 0f;
                source.volume = Mathf.MoveTowards(source.volume, target, step);
                if (i != _activeMusic && source.volume <= 0f && source.isPlaying)
                {
                    source.Stop();
                }
            }

            // A mood whose music wasn't ready yet starts the moment it is.
            if (_mood.HasValue && _musicSources[_activeMusic].clip == null && _music.ContainsKey(_mood.Value))
            {
                StartTrack(_mood.Value);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ---------------------------------------------------------------- music
        private MusicMood WantedMood()
        {
            if (SceneManager.GetActiveScene().name.Contains("Login") || FieldContext.Map == null)
            {
                return MusicMood.Title;
            }

            var player = PlayerCharacter.Local;
            if (player != null && !player.IsDead && BossEngaged(player))
            {
                return MusicMood.Boss;
            }

            return MoodFor(FieldContext.Map);
        }

        private static bool BossEngaged(PlayerCharacter player)
        {
            foreach (var monster in FindObjectsByType<Monster>(FindObjectsSortMode.None))
            {
                if (monster.Definition != null && monster.Definition.IsBoss && !monster.IsDead && monster.AggroTarget != null
                    && Vector3.Distance(monster.Position, player.Position) < 35f)
                {
                    return true;
                }
            }

            return false;
        }

        private void StartTrack(MusicMood mood)
        {
            if (!_music.TryGetValue(mood, out var clip))
            {
                return; // still composing; Update starts it when ready
            }

            var current = _musicSources[_activeMusic];
            if (current.clip == clip && current.isPlaying)
            {
                return;
            }

            _activeMusic = 1 - _activeMusic;
            var next = _musicSources[_activeMusic];
            next.clip = clip;
            next.volume = 0f;
            next.Play();
        }

        private void Compose(MusicMood mood)
        {
            if (_music.ContainsKey(mood) || _composing.Contains(mood))
            {
                return;
            }

            _composing.Add(mood);
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var samples = MusicComposer.Compose(mood, 17);
                lock (_composed)
                {
                    _composed[mood] = samples;
                }
            });
        }

        private void AdoptComposed()
        {
            KeyValuePair<MusicMood, float[]>[] ready;
            lock (_composed)
            {
                if (_composed.Count == 0)
                {
                    return;
                }

                ready = new KeyValuePair<MusicMood, float[]>[_composed.Count];
                ((ICollection<KeyValuePair<MusicMood, float[]>>)_composed).CopyTo(ready, 0);
                _composed.Clear();
            }

            foreach (var pair in ready)
            {
                var clip = AudioClip.Create("Music_" + pair.Key, pair.Value.Length, 1, MusicComposer.Rate, false);
                clip.SetData(pair.Value, 0);
                _music[pair.Key] = clip;
                _composing.Remove(pair.Key);
            }
        }

        // ---------------------------------------------------------------- effects
        private void PlayInternal(Sfx sfx, Vector3? at, float volume, float pitch)
        {
            if (SfxVolume <= 0f)
            {
                return;
            }

            // The same effect twice in a frame or two (a multi-hit skill) is one sound, a little louder.
            if (_lastPlayed.TryGetValue(sfx, out float last) && Time.unscaledTime - last < 0.035f)
            {
                return;
            }

            _lastPlayed[sfx] = Time.unscaledTime;
            if (!_sfx.TryGetValue(sfx, out var clip))
            {
                var samples = SfxBank.Build(sfx);
                clip = AudioClip.Create("Sfx_" + sfx, samples.Length, 1, SfxBank.Rate, false);
                clip.SetData(samples, 0);
                _sfx[sfx] = clip;
            }

            var voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % Voices;
            voice.Stop();
            voice.clip = clip;
            voice.spatialBlend = at.HasValue ? 0.75f : 0f;
            voice.transform.position = at ?? transform.position;
            voice.volume = Mathf.Clamp01(volume) * SfxVolume;
            voice.pitch = pitch * Random.Range(0.96f, 1.04f);
            voice.Play();
        }

        private void FollowListener()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                transform.position = camera.transform.position;
            }

            if (Time.unscaledTime < _nextListenerCheck)
            {
                return;
            }

            _nextListenerCheck = Time.unscaledTime + 2f;
            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            var own = GetComponent<AudioListener>();
            if (listeners.Length == 0)
            {
                gameObject.AddComponent<AudioListener>();
            }
            else if (listeners.Length > 1 && own != null)
            {
                Destroy(own); // a scene camera brought its own
            }
        }
    }
}
