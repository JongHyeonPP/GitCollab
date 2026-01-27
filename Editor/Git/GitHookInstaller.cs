using System.IO;
using UnityEngine;
using UnityEditor;

namespace GitCollab
{
    /// <summary>
    /// Git Hooks installer (deprecated - hooks no longer block commits)
    /// Kept for backwards compatibility to clean up old hooks
    /// </summary>
    public static class GitHookInstaller
    {
        private const string HOOK_MARKER = "# Git Collab for Unity - DO NOT EDIT BELOW";

        /// <summary>
        /// Remove old hooks if they exist (called on package update)
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CleanupOldHooks()
        {
            EditorApplication.delayCall += () =>
            {
                if (GitHelper.IsGitRepository())
                {
                    RemoveHooks();
                }
            };
        }

        /// <summary>
        /// Remove all GitCollab hooks
        /// </summary>
        public static void RemoveHooks()
        {
            string hooksDir = GetHooksDirectory();
            if (string.IsNullOrEmpty(hooksDir)) return;

            bool removed = false;
            removed |= RemoveHook(hooksDir, "pre-commit");
            removed |= RemoveHook(hooksDir, "pre-push");

            if (removed)
            {
                Debug.Log("[GitCollab] Old Git hooks removed. Commits are no longer blocked.");
            }
        }

        private static string GetHooksDirectory()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot)) return null;
            return Path.Combine(repoRoot, ".git", "hooks");
        }

        private static bool RemoveHook(string hooksDir, string hookName)
        {
            string hookPath = Path.Combine(hooksDir, hookName);
            if (!File.Exists(hookPath)) return false;

            string existing = File.ReadAllText(hookPath);
            int markerIndex = existing.IndexOf(HOOK_MARKER);
            
            if (markerIndex < 0) return false; // No GitCollab Hook

            string cleaned = existing.Substring(0, markerIndex).TrimEnd();
            
            if (string.IsNullOrEmpty(cleaned))
            {
                File.Delete(hookPath);
            }
            else
            {
                File.WriteAllText(hookPath, cleaned);
            }

            return true;
        }
    }
}
