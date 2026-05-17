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
            if (levelCfg == null)
                return CreateDefault();

            return Resolve(levelCfg);
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

        static ResolvedLevel CreateDefault()
        {
            return new ResolvedLevel
            {
                DurationSeconds = 180f,
                Timeline = new List<ResolvedTimelineEntry>
                {
                    new ResolvedTimelineEntry
                    {
                        Time = 0f,
                        Groups = new List<SpawnStyleConfig>
                        {
                            new SpawnStyleConfig
                            {
                                EnemyPrefabId = 0,
                                Count = 9999,
                                Interval = 1f,
                                BatchSize = 1,
                                SpawnRadius = 35f,
                                Pattern = "Circle"
                            }
                        }
                    }
                }
            };
        }
    }
}
