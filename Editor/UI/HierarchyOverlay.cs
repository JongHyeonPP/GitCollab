using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GitCollab
{
    /// <summary>
    /// Hierarchy view lock icon overlay
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyOverlay
    {
        private static Dictionary<int, LockInfo> _sceneLocks = new Dictionary<int, LockInfo>();
        private static Texture2D _lockIcon;
        private static double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 5.0;

        static HierarchyOverlay()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
            EditorApplication.update += OnEditorUpdate;
            CreateIcon();
        }

        private static void CreateIcon()
        {
            int size = 14;
            _lockIcon = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _lockIcon.filterMode = FilterMode.Point;

            Color transparent = new Color(0, 0, 0, 0);
            Color lockColor = new Color(0.9f, 0.7f, 0.2f);
            Color outline = new Color(0, 0, 0, 0.7f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    _lockIcon.SetPixel(x, y, transparent);
                }
            }

            // Lock body
            for (int y = 2; y <= 7; y++)
            {
                for (int x = 3; x <= 10; x++)
                {
                    if (y == 2 || y == 7 || x == 3 || x == 10)
                        _lockIcon.SetPixel(x, y, outline);
                    else
                        _lockIcon.SetPixel(x, y, lockColor);
                }
            }

            // Lock shackle
            for (int y = 8; y <= 11; y++)
            {
                _lockIcon.SetPixel(5, y, outline);
                _lockIcon.SetPixel(8, y, outline);
            }
            for (int x = 5; x <= 8; x++)
            {
                _lockIcon.SetPixel(x, 11, outline);
            }

            _lockIcon.Apply();
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshSceneLocks();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void RefreshSceneLocks()
        {
            _sceneLocks.Clear();

            // Find all scenes that are locked
            var allLocks = LockManager.GetAllLocks();
            foreach (var lockInfo in allLocks)
            {
                if (lockInfo.filePath.EndsWith(".unity"))
                {
                    // Get scene asset and find its instance ID
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(lockInfo.filePath);
                    if (sceneAsset != null)
                    {
                        _sceneLocks[sceneAsset.GetInstanceID()] = lockInfo;
                    }
                }
            }
        }

        private static void OnHierarchyItemGUI(int instanceID, Rect selectionRect)
        {
            // Check if this is a scene header
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj == null) return;

            // Check if it's a SceneAsset (scene header in hierarchy)
            if (obj is SceneAsset sceneAsset)
            {
                string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
                var lockInfo = LockManager.GetLockInfo(scenePath);

                if (lockInfo != null)
                {
                    DrawLockIcon(selectionRect, lockInfo);
                }
            }
            else if (obj is GameObject go)
            {
                // Check if this GameObject's scene is locked
                string scenePath = go.scene.path;
                if (!string.IsNullOrEmpty(scenePath))
                {
                    var lockInfo = LockManager.GetLockInfo(scenePath);
                    if (lockInfo != null && !lockInfo.IsOwnedByMe)
                    {
                        // Show subtle indicator for objects in locked scenes
                        Rect iconRect = new Rect(selectionRect.xMax - 16, selectionRect.y, 14, 14);
                        GUI.DrawTexture(iconRect, _lockIcon);
                    }
                }
            }
        }

        private static void DrawLockIcon(Rect selectionRect, LockInfo lockInfo)
        {
            Rect iconRect = new Rect(selectionRect.xMax - 18, selectionRect.y + 1, 14, 14);
            
            Color originalColor = GUI.color;
            GUI.color = lockInfo.IsOwnedByMe ? new Color(0.2f, 0.8f, 0.4f) : new Color(0.9f, 0.3f, 0.3f);
            GUI.DrawTexture(iconRect, _lockIcon);
            GUI.color = originalColor;

            // Tooltip on hover
            if (iconRect.Contains(Event.current.mousePosition))
            {
                string owner = lockInfo.IsOwnedByMe ? "You" : lockInfo.lockedBy.name;
                EditorGUI.LabelField(iconRect, new GUIContent("", $"Locked by {owner}\n{lockInfo.reason}"));
            }
        }

        public static void ForceRefresh()
        {
            RefreshSceneLocks();
        }
    }
}
