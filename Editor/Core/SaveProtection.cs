using UnityEditor;
using UnityEngine;
using System.Linq;

namespace GitCollab
{
    /// <summary>
    /// Prevents saving files that are locked by others
    /// </summary>
    public class SaveProtection : AssetModificationProcessor
    {
        /// <summary>
        /// Called before Unity saves assets
        /// </summary>
        static string[] OnWillSaveAssets(string[] paths)
        {
            if (!GitHelper.IsGitRepository()) return paths;
            if (!SettingsManager.Settings.showProjectViewOverlay) return paths; // Use as enable flag
            
            // Refresh lock cache before checking
            LockManager.InvalidateCache();
            
            var allowedPaths = paths.ToList();
            
            foreach (string path in paths)
            {
                if (!LockManager.IsLockableFile(path)) continue;
                
                var lockInfo = LockManager.GetLockInfo(path);
                if (lockInfo != null && !lockInfo.IsOwnedByMe)
                {
                    // File is locked by someone else - block save
                    allowedPaths.Remove(path);
                    
                    EditorUtility.DisplayDialog(
                        "Git Collab - Save Blocked",
                        $"Cannot save '{System.IO.Path.GetFileName(path)}'\n\n" +
                        $"This file is locked by {lockInfo.lockedBy.name}.\n" +
                        $"Reason: {lockInfo.reason}\n\n" +
                        $"Contact them to unlock or use Force Unlock.",
                        "OK"
                    );
                    
                    Debug.LogWarning($"[GitCollab] Save blocked: {path} is locked by {lockInfo.lockedBy.name}");
                }
            }
            
            return allowedPaths.ToArray();
        }

        /// <summary>
        /// Called when trying to open a file for editing
        /// </summary>
        static bool IsOpenForEdit(string assetPath, out string message)
        {
            message = "";
            
            if (!GitHelper.IsGitRepository()) return true;
            if (!LockManager.IsLockableFile(assetPath)) return true;
            
            var lockInfo = LockManager.GetLockInfo(assetPath);
            if (lockInfo != null && !lockInfo.IsOwnedByMe)
            {
                message = $"Locked by {lockInfo.lockedBy.name}: {lockInfo.reason}";
                return false; // Not open for edit
            }
            
            return true;
        }
    }
}
