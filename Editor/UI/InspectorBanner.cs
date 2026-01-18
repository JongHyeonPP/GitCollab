using UnityEngine;
using UnityEditor;

namespace GitCollab
{
    /// <summary>
    /// Inspector header banner showing lock status
    /// </summary>
    [InitializeOnLoad]
    public static class InspectorBanner
    {
        private static GUIStyle _bannerStyle;
        private static GUIStyle _lockedByMeStyle;
        private static GUIStyle _lockedByOtherStyle;

        static InspectorBanner()
        {
            Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        private static void InitStyles()
        {
            if (_bannerStyle != null) return;

            _bannerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 6, 6)
            };

            _lockedByMeStyle = new GUIStyle(_bannerStyle);
            _lockedByMeStyle.normal.textColor = new Color(0.2f, 0.7f, 0.3f);

            _lockedByOtherStyle = new GUIStyle(_bannerStyle);
            _lockedByOtherStyle.normal.textColor = new Color(0.9f, 0.3f, 0.3f);
        }

        private static void OnHeaderGUI(Editor editor)
        {
            if (editor.target == null) return;
            if (!SettingsManager.Settings.showProjectViewOverlay) return;

            string assetPath = AssetDatabase.GetAssetPath(editor.target);
            if (string.IsNullOrEmpty(assetPath)) return;

            // Check if this asset or its scene is locked
            var lockInfo = LockManager.GetLockInfo(assetPath);
            
            // For scene objects, check the scene lock
            if (lockInfo == null && editor.target is GameObject go)
            {
                string scenePath = go.scene.path;
                if (!string.IsNullOrEmpty(scenePath))
                {
                    lockInfo = LockManager.GetLockInfo(scenePath);
                }
            }

            if (lockInfo == null) return;

            InitStyles();

            EditorGUILayout.Space(2);

            if (lockInfo.IsOwnedByMe)
            {
                EditorGUILayout.LabelField(
                    $"🔒 Locked by you - {lockInfo.reason}",
                    _lockedByMeStyle
                );
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    $"🔒 Locked by {lockInfo.lockedBy.name}",
                    _lockedByOtherStyle
                );
                
                if (GUILayout.Button("Request", GUILayout.Width(60)))
                {
                    EditorUtility.DisplayDialog(
                        "Request Unlock",
                        $"Contact {lockInfo.lockedBy.name} to unlock this file.\n\nEmail: {lockInfo.lockedBy.email}",
                        "OK"
                    );
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(2);
        }
    }
}
