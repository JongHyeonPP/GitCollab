using UnityEditor;

namespace GitCollab
{
    /// <summary>
    /// 에디터 알림 시스템
    /// </summary>
    public static class NotificationSystem
    {
        /// <summary>
        /// Scene View에 알림 표시
        /// </summary>
        public static void ShowNotification(string message, float duration = 2f)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.ShowNotification(new UnityEngine.GUIContent(message), duration);
            }
        }

        /// <summary>
        /// 잠금 성공 알림
        /// </summary>
        public static void NotifyLockAcquired(string filePath)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            ShowNotification($"Locked: {fileName}");
        }

        /// <summary>
        /// 잠금 해제 알림
        /// </summary>
        public static void NotifyLockReleased(string filePath)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            ShowNotification($"Unlocked: {fileName}");
        }

        /// <summary>
        /// 경고 알림 (타인 잠금)
        /// </summary>
        public static void NotifyLockConflict(string filePath, string ownerName)
        {
            string fileName = System.IO.Path.GetFileName(filePath);
            ShowNotification($"{fileName} is locked by {ownerName}", 4f);
        }

        /// <summary>
        /// 동기화 완료 알림
        /// </summary>
        public static void NotifySyncComplete(int lockCount)
        {
            ShowNotification($"Synced: {lockCount} locks");
        }
    }
}
