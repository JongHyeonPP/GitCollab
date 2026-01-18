using UnityEditor;
using UnityEngine;

namespace GitCollab
{
    /// <summary>
    /// 새로고침 단축키 - Ctrl+R로 빠르게 잠금 상태 동기화
    /// </summary>
    public static class RefreshShortcut
    {
        [MenuItem("Window/Git Collab/Refresh Locks #%r", false, 50)]
        private static void RefreshLocks()
        {
            if (!GitHelper.IsGitRepository())
            {
                Debug.LogWarning("[Git Collab] Not a Git repository.");
                return;
            }

            SyncManager.ForceSync();
            NotificationSystem.ShowNotification("Locks refreshed", 1.5f);
        }

        [MenuItem("Window/Git Collab/Refresh Locks #%r", true)]
        private static bool ValidateRefreshLocks()
        {
            return GitHelper.IsGitRepository();
        }
    }
}
