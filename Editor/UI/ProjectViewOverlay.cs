using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GitCollab
{
    /// <summary>
    /// 프로젝트 뷰에 잠금 아이콘 오버레이 표시
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectViewOverlay
    {
        private static Texture2D _lockIconGreen;
        private static Texture2D _lockIconRed;
        private static Texture2D _lockIconYellow;
        
        private static Dictionary<string, LockInfo> _visibleLocks = new Dictionary<string, LockInfo>();
        private static double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 5.0; // 5초마다 갱신

        static ProjectViewOverlay()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            EditorApplication.update += OnEditorUpdate;
            LoadIcons();
        }

        private static void LoadIcons()
        {
            // 아이콘이 없으면 동적 생성
            _lockIconGreen = CreateLockIcon(new Color(0.2f, 0.8f, 0.2f));
            _lockIconRed = CreateLockIcon(new Color(0.9f, 0.2f, 0.2f));
            _lockIconYellow = CreateLockIcon(new Color(0.9f, 0.7f, 0.1f));
        }

        private static Texture2D CreateLockIcon(Color color)
        {
            int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            // 투명 배경
            Color transparent = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            // 간단한 자물쇠 모양 그리기
            Color outline = new Color(0, 0, 0, 0.8f);
            
            // 자물쇠 몸통 (사각형)
            for (int y = 2; y <= 8; y++)
            {
                for (int x = 3; x <= 12; x++)
                {
                    if (y == 2 || y == 8 || x == 3 || x == 12)
                        texture.SetPixel(x, y, outline);
                    else
                        texture.SetPixel(x, y, color);
                }
            }

            // 자물쇠 고리 (위쪽 반원)
            for (int y = 9; y <= 13; y++)
            {
                for (int x = 5; x <= 10; x++)
                {
                    if ((x == 5 || x == 10) && y >= 9)
                    {
                        texture.SetPixel(x, y, outline);
                    }
                    if (y == 13 && x >= 6 && x <= 9)
                    {
                        texture.SetPixel(x, y, outline);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private static void OnEditorUpdate()
        {
            // 주기적으로 잠금 상태 갱신
            if (EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                RefreshLockCache();
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private static void RefreshLockCache()
        {
            _visibleLocks.Clear();
            
            var allLocks = LockManager.GetAllLocks();
            foreach (var lockInfo in allLocks)
            {
                _visibleLocks[lockInfo.filePath] = lockInfo;
            }
        }

        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return;

            // 잠금 가능 파일인지 확인
            if (!LockManager.IsLockableFile(path)) return;

            // 잠금 정보 확인
            if (!_visibleLocks.TryGetValue(path, out LockInfo lockInfo))
            {
                lockInfo = LockManager.GetLockInfo(path);
                if (lockInfo != null)
                {
                    _visibleLocks[path] = lockInfo;
                }
            }

            if (lockInfo == null) return;

            // Ensure icons are loaded
            if (_lockIconGreen == null || _lockIconRed == null || _lockIconYellow == null)
            {
                LoadIcons();
            }

            // 아이콘 선택
            Texture2D icon = lockInfo.IsOwnedByMe ? _lockIconGreen : _lockIconRed;
            if (lockInfo.IsExpired)
            {
                icon = _lockIconYellow;
            }

            // Null check for icon
            if (icon == null) return;

            // 아이콘 위치 계산 (왼쪽 상단)
            Rect iconRect = new Rect(
                selectionRect.x + 2,
                selectionRect.y + 2,
                14,
                14
            );

            // List view와 Grid view 구분
            if (selectionRect.height > 20)
            {
                // Grid view (썸네일 모드)
                iconRect = new Rect(
                    selectionRect.x + 2,
                    selectionRect.y + 2,
                    16,
                    16
                );
            }

            // 아이콘 그리기
            GUI.DrawTexture(iconRect, icon);

            // 툴팁 (마우스 호버 시)
            if (selectionRect.Contains(Event.current.mousePosition))
            {
                string owner = lockInfo.IsOwnedByMe ? "You" : lockInfo.lockedBy.name;
                string tooltip = $"Locked by {owner}\n{lockInfo.reason}\n{lockInfo.TimeSinceLock}";
                
                // 커스텀 툴팁 표시
                ShowTooltip(tooltip, Event.current.mousePosition);
            }
        }

        private static void ShowTooltip(string text, Vector2 position)
        {
            // GUIContent를 사용한 간단한 툴팁
            // (Unity 내장 툴팁 시스템 활용)
            var content = new GUIContent("", text);
            GUI.Label(new Rect(position.x, position.y, 1, 1), content);
        }

        /// <summary>
        /// 캐시 강제 갱신
        /// </summary>
        public static void ForceRefresh()
        {
            RefreshLockCache();
            EditorApplication.RepaintProjectWindow();
        }
    }
}
