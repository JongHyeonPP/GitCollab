using System;
using System.IO;
using UnityEngine;

namespace GitCollab
{
    /// <summary>
    /// 프로젝트 설정 관리
    /// </summary>
    public static class SettingsManager
    {
        private const string CONFIG_FILE = ".gitcollab/config.json";
        private static GitCollabSettings _cachedSettings;

        public static GitCollabSettings Settings
        {
            get
            {
                if (_cachedSettings == null)
                {
                    _cachedSettings = LoadSettings();
                }
                return _cachedSettings;
            }
        }

        private static GitCollabSettings LoadSettings()
        {
            string configPath = GetConfigPath();
            
            if (!File.Exists(configPath))
            {
                var defaults = GitCollabSettings.CreateDefaults();
                SaveSettings(defaults);
                return defaults;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                return JsonUtility.FromJson<GitCollabSettings>(json);
            }
            catch
            {
                return GitCollabSettings.CreateDefaults();
            }
        }

        public static void SaveSettings(GitCollabSettings settings)
        {
            string configPath = GetConfigPath();
            string directory = Path.GetDirectoryName(configPath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(settings, true);
            File.WriteAllText(configPath, json);
            _cachedSettings = settings;
        }

        private static string GetConfigPath()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                repoRoot = Directory.GetParent(Application.dataPath).FullName;
            }
            return Path.Combine(repoRoot, CONFIG_FILE);
        }

        public static void InvalidateCache()
        {
            _cachedSettings = null;
        }
    }

    [Serializable]
    public class GitCollabSettings
    {
        public int version = 1;
        
        // 알림 설정
        public bool showNotifications = true;
        
        // UI 설정
        public bool showProjectViewOverlay = true;

        public static GitCollabSettings CreateDefaults()
        {
            return new GitCollabSettings();
        }
    }
}
