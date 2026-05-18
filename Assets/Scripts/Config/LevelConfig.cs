using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace CardTower.Config
{
    [Serializable]
    public class LevelConfig
    {
        public float DurationSeconds;
        public List<TimelineEntry> Timeline;
    }

    [Serializable]
    public class SpawnStyleConfig
    {
        public int EnemyPrefabId;
        public int Count;
        public float Interval;
        public int BatchSize;
        public float SpawnRadius;
        public string Pattern;
        public float OverrideHealth;
        public float OverrideSpeed;
        public float OverrideScale;
        public Vector3 FixedPosition;
    }

    [Serializable]
    public class TimelineEntry
    {
        public float Time;
        public List<string> Groups;
    }

    [Serializable]
    public class ResolvedLevel
    {
        public float DurationSeconds;
        public List<ResolvedTimelineEntry> Timeline;
    }

    [Serializable]
    public class ResolvedTimelineEntry
    {
        public float Time;
        public List<SpawnStyleConfig> Groups;
    }

    public static class LevelConfigLoader
    {
        static Dictionary<string, SpawnStyleConfig> _globalStyles;

        public static ResolvedLevel Load(int levelId)
        {
            if (_globalStyles == null)
                _globalStyles = LoadGlobalStyles();

            var levelCfg = LoadLevelConfig(levelId);
            if (levelCfg != null)
                return Resolve(levelCfg);

            return GenerateLevel(levelId);
        }

        static Dictionary<string, SpawnStyleConfig> LoadGlobalStyles()
        {
            var asset = Resources.Load<TextAsset>("Config/SpawnStyles");
            if (asset == null)
            {
                Debug.LogWarning("SpawnStyles.json not found, using empty style map");
                return new Dictionary<string, SpawnStyleConfig>();
            }

            return JsonConvert.DeserializeObject<Dictionary<string, SpawnStyleConfig>>(asset.text);
        }

        static LevelConfig LoadLevelConfig(int levelId)
        {
            var path = $"Config/Level_{levelId}";
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null)
                return null;

            return JsonConvert.DeserializeObject<LevelConfig>(asset.text);
        }

        static ResolvedLevel Resolve(LevelConfig cfg)
        {
            var resolved = new ResolvedLevel
            {
                DurationSeconds = cfg.DurationSeconds,
                Timeline = new List<ResolvedTimelineEntry>()
            };

            foreach (var entry in cfg.Timeline)
            {
                var resolvedEntry = new ResolvedTimelineEntry
                {
                    Time = entry.Time,
                    Groups = new List<SpawnStyleConfig>()
                };

                foreach (var groupId in entry.Groups)
                {
                    if (_globalStyles.TryGetValue(groupId, out var style))
                        resolvedEntry.Groups.Add(style);
                    else
                        Debug.LogWarning($"Spawn style '{groupId}' not found");
                }

                resolved.Timeline.Add(resolvedEntry);
            }

            return resolved;
        }

        // ── Procedural generation ──

        static ResolvedLevel GenerateLevel(int levelId)
        {
            var duration = GetDuration(levelId);
            var waveCount = GetWaveCount(levelId);
            var availableStyleKeys = GetAvailableStyleKeys(levelId);
            var (healthMult, countMult, speedMult) = GetScaling(levelId);

            var timeline = new List<ResolvedTimelineEntry>();

            for (var w = 0; w < waveCount; w++)
            {
                var t = waveCount == 1
                    ? 0f
                    : (float)w / (waveCount - 1) * duration * 0.85f;

                var groups = new List<SpawnStyleConfig>();

                foreach (var key in availableStyleKeys)
                {
                    if (!_globalStyles.TryGetValue(key, out var baseStyle))
                        continue;

                    var cfg = CloneStyle(baseStyle);
                    cfg.Count = Mathf.Max(1, Mathf.RoundToInt(cfg.Count * countMult));
                    cfg.Interval = Mathf.Max(0.3f, cfg.Interval / Mathf.Sqrt(countMult));
                    cfg.BatchSize = Mathf.Max(1, Mathf.RoundToInt(cfg.BatchSize * Mathf.Sqrt(countMult)));

                    if (cfg.OverrideHealth > 0f)
                        cfg.OverrideHealth *= healthMult;
                    if (cfg.OverrideSpeed > 0f)
                        cfg.OverrideSpeed = Mathf.Min(cfg.OverrideSpeed * speedMult, 4f);

                    groups.Add(cfg);
                }

                timeline.Add(new ResolvedTimelineEntry
                {
                    Time = t,
                    Groups = groups
                });
            }

            return new ResolvedLevel
            {
                DurationSeconds = duration,
                Timeline = timeline
            };
        }

        static float GetDuration(int levelId)
        {
            if (levelId >= 16) return 90f;
            if (levelId >= 11) return 75f;
            if (levelId >= 6) return 60f;
            return 45f;
        }

        static int GetWaveCount(int levelId)
        {
            if (levelId >= 18) return 7;
            if (levelId >= 13) return 6;
            if (levelId >= 8) return 5;
            if (levelId >= 4) return 4;
            return 3;
        }

        static string[] GetAvailableStyleKeys(int levelId)
        {
            if (levelId >= 16)
                return new[] { "grunt_light", "grunt_wave", "fast_squad", "grunt_rush", "tank_pair" };
            if (levelId >= 11)
                return new[] { "grunt_light", "grunt_wave", "fast_squad", "grunt_rush" };
            if (levelId >= 6)
                return new[] { "grunt_light", "grunt_wave", "fast_squad" };
            return new[] { "grunt_light" };
        }

        static (float health, float count, float speed) GetScaling(int levelId)
        {
            var effectiveLevel = levelId;

            var healthMult = 1f + (effectiveLevel - 1) * 0.05f;
            var countMult = 1f + (effectiveLevel - 1) * 0.1f;
            var speedMult = Mathf.Min(1.5f, 1f + (effectiveLevel - 1) * 0.025f);

            // Endless mode: exponential scaling after level 20
            if (levelId > 20)
            {
                var extra = levelId - 20;
                healthMult *= Mathf.Pow(1.15f, extra);
                countMult *= Mathf.Pow(1.1f, extra);
            }

            return (healthMult, countMult, speedMult);
        }

        static SpawnStyleConfig CloneStyle(SpawnStyleConfig src)
        {
            return new SpawnStyleConfig
            {
                EnemyPrefabId = src.EnemyPrefabId,
                Count = src.Count,
                Interval = src.Interval,
                BatchSize = src.BatchSize,
                SpawnRadius = src.SpawnRadius,
                Pattern = src.Pattern,
                OverrideHealth = src.OverrideHealth,
                OverrideSpeed = src.OverrideSpeed,
                OverrideScale = src.OverrideScale,
                FixedPosition = src.FixedPosition
            };
        }
    }
}
