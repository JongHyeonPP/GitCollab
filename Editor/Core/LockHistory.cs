using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GitCollab
{
    /// <summary>
    /// Tracks lock/unlock history for audit purposes
    /// </summary>
    public static class LockHistory
    {
        private const string HISTORY_FILE = ".gitcollab/history.json";
        private const int MAX_HISTORY_ENTRIES = 100;
        
        private static HistoryData _cachedHistory;

        /// <summary>
        /// Record a lock event
        /// </summary>
        public static void RecordLock(string filePath, string reason)
        {
            AddEntry(new HistoryEntry
            {
                action = "lock",
                filePath = filePath,
                user = GitHelper.GetUserName(),
                email = GitHelper.GetUserEmail(),
                reason = reason,
                timestamp = DateTime.Now.ToString("o"),
                branch = GitHelper.GetCurrentBranch()
            });
        }

        /// <summary>
        /// Record an unlock event
        /// </summary>
        public static void RecordUnlock(string filePath, bool forced = false)
        {
            AddEntry(new HistoryEntry
            {
                action = forced ? "force_unlock" : "unlock",
                filePath = filePath,
                user = GitHelper.GetUserName(),
                email = GitHelper.GetUserEmail(),
                timestamp = DateTime.Now.ToString("o"),
                branch = GitHelper.GetCurrentBranch()
            });
        }

        /// <summary>
        /// Get recent history entries
        /// </summary>
        public static List<HistoryEntry> GetRecentHistory(int count = 20)
        {
            var history = LoadHistory();
            var result = new List<HistoryEntry>();
            
            int start = Mathf.Max(0, history.entries.Count - count);
            for (int i = history.entries.Count - 1; i >= start; i--)
            {
                result.Add(history.entries[i]);
            }
            
            return result;
        }

        /// <summary>
        /// Get history for a specific file
        /// </summary>
        public static List<HistoryEntry> GetFileHistory(string filePath)
        {
            var history = LoadHistory();
            var result = new List<HistoryEntry>();
            
            foreach (var entry in history.entries)
            {
                if (entry.filePath == filePath)
                {
                    result.Add(entry);
                }
            }
            
            result.Reverse();
            return result;
        }

        private static void AddEntry(HistoryEntry entry)
        {
            var history = LoadHistory();
            history.entries.Add(entry);
            
            // Trim old entries
            while (history.entries.Count > MAX_HISTORY_ENTRIES)
            {
                history.entries.RemoveAt(0);
            }
            
            SaveHistory(history);
        }

        private static HistoryData LoadHistory()
        {
            if (_cachedHistory != null) return _cachedHistory;

            string path = GetHistoryPath();
            
            if (!File.Exists(path))
            {
                _cachedHistory = new HistoryData { entries = new List<HistoryEntry>() };
                return _cachedHistory;
            }

            try
            {
                string json = File.ReadAllText(path);
                _cachedHistory = JsonUtility.FromJson<HistoryData>(json);
                if (_cachedHistory.entries == null)
                {
                    _cachedHistory.entries = new List<HistoryEntry>();
                }
            }
            catch
            {
                _cachedHistory = new HistoryData { entries = new List<HistoryEntry>() };
            }

            return _cachedHistory;
        }

        private static void SaveHistory(HistoryData history)
        {
            string path = GetHistoryPath();
            string dir = Path.GetDirectoryName(path);
            
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(history, true);
            File.WriteAllText(path, json);
            _cachedHistory = history;
        }

        private static string GetHistoryPath()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                repoRoot = Directory.GetParent(Application.dataPath).FullName;
            }
            return Path.Combine(repoRoot, HISTORY_FILE);
        }

        public static void InvalidateCache()
        {
            _cachedHistory = null;
        }
    }

    [Serializable]
    public class HistoryData
    {
        public List<HistoryEntry> entries;
    }

    [Serializable]
    public class HistoryEntry
    {
        public string action;
        public string filePath;
        public string user;
        public string email;
        public string reason;
        public string timestamp;
        public string branch;

        public string FormattedTime
        {
            get
            {
                if (DateTime.TryParse(timestamp, out DateTime time))
                {
                    var elapsed = DateTime.Now - time;
                    if (elapsed.TotalMinutes < 1) return "Just now";
                    if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes}m ago";
                    if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours}h ago";
                    return $"{(int)elapsed.TotalDays}d ago";
                }
                return "Unknown";
            }
        }

        public string ActionIcon => action switch
        {
            "lock" => "🔒",
            "unlock" => "🔓",
            "force_unlock" => "⚠️",
            _ => "•"
        };
    }
}
