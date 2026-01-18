using UnityEngine;
using UnityEditor;

namespace GitCollab
{
    /// <summary>
    /// 프로젝트 뷰 우클릭 메뉴 통합
    /// </summary>
    public static class ContextMenuIntegration
    {
        private const int MENU_PRIORITY = 1000;

        //===========================================
        // Lock File
        //===========================================
        
        [MenuItem("Assets/Git Collab/Lock File", false, MENU_PRIORITY)]
        private static void LockFile()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                var result = LockManager.Lock(path);
                if (result.Success)
                {
                    Debug.Log($"[GitCollab] Locked: {path}");
                }
                else
                {
                    Debug.LogWarning($"[GitCollab] Failed to lock '{path}': {result.Message}");
                }
            }
            
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Git Collab/Lock File", true)]
        private static bool LockFileValidation()
        {
            if (Selection.activeObject == null) return false;
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return LockManager.CanLock(path);
        }

        //===========================================
        // Lock File with Reason
        //===========================================
        
        [MenuItem("Assets/Git Collab/Lock File (with reason)...", false, MENU_PRIORITY + 1)]
        private static void LockFileWithReason()
        {
            var window = LockReasonWindow.ShowWindow();
            window.OnConfirm = (reason) =>
            {
                foreach (var obj in Selection.objects)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;

                    var result = LockManager.Lock(path, reason);
                    if (result.Success)
                    {
                        Debug.Log($"[GitCollab] Locked: {path} - {reason}");
                    }
                    else
                    {
                        Debug.LogWarning($"[GitCollab] Failed to lock '{path}': {result.Message}");
                    }
                }
                AssetDatabase.Refresh();
            };
        }

        [MenuItem("Assets/Git Collab/Lock File (with reason)...", true)]
        private static bool LockFileWithReasonValidation()
        {
            return LockFileValidation();
        }

        //===========================================
        // Unlock File
        //===========================================
        
        [MenuItem("Assets/Git Collab/Unlock File", false, MENU_PRIORITY + 10)]
        private static void UnlockFile()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                var result = LockManager.Unlock(path);
                if (result.Success)
                {
                    Debug.Log($"[GitCollab] Unlocked: {path}");
                }
                else
                {
                    Debug.LogWarning($"[GitCollab] Failed to unlock '{path}': {result.Message}");
                }
            }
            
            AssetDatabase.Refresh();
        }

        [MenuItem("Assets/Git Collab/Unlock File", true)]
        private static bool UnlockFileValidation()
        {
            if (Selection.activeObject == null) return false;
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return LockManager.CanUnlock(path);
        }

        //===========================================
        // View Lock Info
        //===========================================
        
        [MenuItem("Assets/Git Collab/View Lock Info", false, MENU_PRIORITY + 20)]
        private static void ViewLockInfo()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var lockInfo = LockManager.GetLockInfo(path);
            
            if (lockInfo == null)
            {
                EditorUtility.DisplayDialog("Git Collab", "This file is not locked.", "OK");
                return;
            }

            string message = $"File: {lockInfo.filePath}\n" +
                           $"Locked by: {lockInfo.lockedBy.name}\n" +
                           $"Email: {lockInfo.lockedBy.email}\n" +
                           $"Time: {lockInfo.TimeSinceLock}\n" +
                           $"Reason: {lockInfo.reason}\n" +
                           $"Branch: {lockInfo.branch}";
            
            EditorUtility.DisplayDialog("Git Collab - Lock Info", message, "OK");
        }

        [MenuItem("Assets/Git Collab/View Lock Info", true)]
        private static bool ViewLockInfoValidation()
        {
            if (Selection.activeObject == null) return false;
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return LockManager.IsLocked(path);
        }

        //===========================================
        // Force Unlock (Admin)
        //===========================================
        
        [MenuItem("Assets/Git Collab/Force Unlock (Admin)", false, MENU_PRIORITY + 30)]
        private static void ForceUnlock()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var lockInfo = LockManager.GetLockInfo(path);
            
            if (lockInfo == null) return;

            bool confirm = EditorUtility.DisplayDialog(
                "Git Collab - Force Unlock",
                $"Are you sure you want to force unlock '{lockInfo.lockedBy.name}'s lock?\n\n" +
                $"File: {path}\n" +
                $"Reason: {lockInfo.reason}",
                "Force Unlock",
                "Cancel"
            );

            if (confirm)
            {
                var result = LockManager.Unlock(path, force: true);
                if (result.Success)
                {
                    Debug.Log($"[GitCollab] Force unlocked: {path}");
                }
                else
                {
                    Debug.LogError($"[GitCollab] Force unlock failed: {result.Message}");
                }
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Assets/Git Collab/Force Unlock (Admin)", true)]
        private static bool ForceUnlockValidation()
        {
            if (Selection.activeObject == null) return false;
            
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            var lockInfo = LockManager.GetLockInfo(path);
            
            // 타인 잠금이고, 내 것이 아닐 때만 표시
            return lockInfo != null && !lockInfo.IsOwnedByMe;
        }

        //===========================================
        // Open Dashboard
        //===========================================
        
        [MenuItem("Assets/Git Collab/Open Dashboard", false, MENU_PRIORITY + 100)]
        private static void OpenDashboard()
        {
            MainWindow.ShowWindow();
        }
    }

    /// <summary>
    /// 잠금 사유 입력 창
    /// </summary>
    public class LockReasonWindow : EditorWindow
    {
        private string reason = "";
        public System.Action<string> OnConfirm;

        public static LockReasonWindow ShowWindow()
        {
            var window = GetWindow<LockReasonWindow>(true, "Lock Reason", true);
            window.minSize = new Vector2(300, 100);
            window.maxSize = new Vector2(400, 120);
            return window;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Enter lock reason:", EditorStyles.boldLabel);
            
            reason = EditorGUILayout.TextField(reason);
            
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Cancel", GUILayout.Width(80)))
            {
                Close();
            }
            
            if (GUILayout.Button("Lock", GUILayout.Width(80)))
            {
                OnConfirm?.Invoke(string.IsNullOrEmpty(reason) ? "Working" : reason);
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
}
