using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GitCollab
{
    /// <summary>
    /// Git Collab 메인 대시보드 - 개선된 UI
    /// </summary>
    public class MainWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "My Locks", "Team", "Settings" };
        
        private List<LockInfo> _myLocks = new List<LockInfo>();
        private List<LockInfo> _otherLocks = new List<LockInfo>();
        
        // 스타일 캐시
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _lockCardStyle;
        private GUIStyle _statNumberStyle;
        private GUIStyle _statLabelStyle;
        private GUIStyle _filePathStyle;
        private GUIStyle _tabButtonStyle;
        private GUIStyle _tabButtonSelectedStyle;
        private bool _stylesInitialized;
        
        // 캐시된 상태
        private bool _cachedIsGitRepo;
        private double _lastGitCheckTime;

        // 색상
        private static readonly Color HeaderBgColor = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color CardBgColor = new Color(0.22f, 0.22f, 0.22f);
        private static readonly Color AccentColor = new Color(0.35f, 0.65f, 1f);
        private static readonly Color SuccessColor = new Color(0.2f, 0.8f, 0.4f);
        private static readonly Color DangerColor = new Color(0.9f, 0.3f, 0.3f);
        private static readonly Color WarningColor = new Color(0.9f, 0.7f, 0.2f);

        [MenuItem("Window/Git Collab/Dashboard", false, 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<MainWindow>(true, "Git Collab");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshData();
            SyncManager.OnSyncCompleted += RefreshData;
        }

        private void OnDisable()
        {
            SyncManager.OnSyncCompleted -= RefreshData;
        }

        private void OnFocus()
        {
            RefreshData();
        }

        private void RefreshData()
        {
            _myLocks = LockManager.GetMyLocks();
            _otherLocks = LockManager.GetOtherLocks();
            Repaint();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 10, 8, 8)
            };

            _cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(15, 15, 12, 12),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _lockCardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };

            _statNumberStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter
            };

            _statLabelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };

            _filePathStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = false
            };

            _tabButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fixedHeight = 28
            };
            
            _tabButtonSelectedStyle = new GUIStyle(_tabButtonStyle);
            _tabButtonSelectedStyle.normal.textColor = AccentColor;
            _tabButtonSelectedStyle.fontStyle = FontStyle.Bold;

            _stylesInitialized = true;
        }
        
        private bool IsGitRepository()
        {
            // 1초마다만 실제 체크
            if (EditorApplication.timeSinceStartup - _lastGitCheckTime > 1.0)
            {
                _cachedIsGitRepo = GitHelper.IsGitRepository();
                _lastGitCheckTime = EditorApplication.timeSinceStartup;
            }
            return _cachedIsGitRepo;
        }

        private void OnGUI()
        {
            InitStyles();

            // 배경색
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.18f, 0.18f, 0.18f));

            DrawHeader();
            DrawStatsBar();
            DrawTabs();
            DrawContent();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(45));
            
            // 로고/타이틀
            GUILayout.Space(10);
            GUILayout.Label("Git Collab", _headerStyle, GUILayout.Height(40));
            
            GUILayout.FlexibleSpace();
            
            // 새로고침 버튼
            if (GUILayout.Button("Refresh", GUILayout.Width(70), GUILayout.Height(30)))
            {
                LockManager.InvalidateCache();
                RefreshData();
                ProjectViewOverlay.ForceRefresh();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            
            // 구분선
            EditorGUI.DrawRect(new Rect(0, 45, position.width, 1), new Color(0.3f, 0.3f, 0.3f));
        }

        private void DrawStatsBar()
        {
            EditorGUILayout.BeginHorizontal(_cardStyle, GUILayout.Height(80));
            
            // 내 잠금
            DrawStatBox("My Locks", _myLocks.Count.ToString(), SuccessColor);
            
            GUILayout.Space(20);
            
            // 팀 잠금
            DrawStatBox("Team Locks", _otherLocks.Count.ToString(), DangerColor);
            
            GUILayout.Space(20);
            
            // 총 잠금
            int total = _myLocks.Count + _otherLocks.Count;
            DrawStatBox("Total", total.ToString(), AccentColor);
            
            GUILayout.Space(20);
            
            // Git 상태 (캐시된 값 사용)
            bool isGitRepo = IsGitRepository();
            string gitStatus = isGitRepo ? "✓ Connected" : "✗ No Git";
            Color gitColor = isGitRepo ? SuccessColor : DangerColor;
            DrawStatBox("Git", gitStatus, gitColor, true);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatBox(string label, string value, Color accentColor, bool isText = false)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(90));
            
            GUILayout.FlexibleSpace();
            
            // 숫자 또는 텍스트 - 색상만 임시 변경
            var prevColor = _statNumberStyle.normal.textColor;
            var prevSize = _statNumberStyle.fontSize;
            _statNumberStyle.normal.textColor = accentColor;
            if (isText) _statNumberStyle.fontSize = 12;
            
            GUILayout.Label(value, _statNumberStyle);
            
            _statNumberStyle.normal.textColor = prevColor;
            _statNumberStyle.fontSize = prevSize;
            
            // 라벨
            GUILayout.Label(label, _statLabelStyle);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawTabs()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            
            for (int i = 0; i < _tabNames.Length; i++)
            {
                bool isSelected = _selectedTab == i;
                var style = isSelected ? _tabButtonSelectedStyle : _tabButtonStyle;
                
                if (GUILayout.Button(_tabNames[i], style, GUILayout.Height(28)))
                {
                    _selectedTab = i;
                }
            }
            
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();
            
            // 탭 하단 선
            Rect tabLineRect = GUILayoutUtility.GetRect(position.width, 2);
            EditorGUI.DrawRect(tabLineRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        private void DrawContent()
        {
            EditorGUILayout.Space(5);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            switch (_selectedTab)
            {
                case 0: DrawMyLocksTab(); break;
                case 1: DrawTeamTab(); break;
                case 2: DrawSettingsTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawMyLocksTab()
        {
            if (_myLocks.Count == 0)
            {
                DrawEmptyState("", "No locked files", "Right-click a file and select 'Git Collab > Lock File' to lock it.");
                return;
            }

            EditorGUILayout.Space(5);
            
            foreach (var lockInfo in _myLocks)
            {
                DrawLockCard(lockInfo, true);
            }
        }

        private void DrawTeamTab()
        {
            if (_otherLocks.Count == 0)
            {
                DrawEmptyState("", "No team locks", "Your team members haven't locked any files.");
                return;
            }

            EditorGUILayout.Space(5);
            
            foreach (var lockInfo in _otherLocks)
            {
                DrawLockCard(lockInfo, false);
            }
        }

        private void DrawLockCard(LockInfo lockInfo, bool isMine)
        {
            EditorGUILayout.BeginVertical(_lockCardStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            // 상태 표시 원
            string statusIcon = isMine ? "●" : "●";
            GUILayout.Label(statusIcon, GUILayout.Width(20));
            
            // 파일명
            string fileName = System.IO.Path.GetFileName(lockInfo.filePath);
            var nameStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            
            if (GUILayout.Button(fileName, nameStyle))
            {
                PingFile(lockInfo.filePath);
            }
            
            GUILayout.FlexibleSpace();
            
            // 경과 시간
            GUILayout.Label(lockInfo.TimeSinceLock, EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
            
            // 경로
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24);
            GUILayout.Label(lockInfo.filePath, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            // 소유자 및 사유
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24);
            
            string ownerInfo = isMine ? "You" : lockInfo.lockedBy.name;
            string reason = string.IsNullOrEmpty(lockInfo.reason) ? "" : $" · \"{lockInfo.reason}\"";
            GUILayout.Label($"{ownerInfo}{reason}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
            
            // 버튼
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(24);
            
            if (isMine)
            {
                if (GUILayout.Button("Unlock", GUILayout.Width(100), GUILayout.Height(24)))
                {
                    var result = LockManager.Unlock(lockInfo.filePath);
                    if (result.Success)
                    {
                        NotificationSystem.NotifyLockReleased(lockInfo.filePath);
                        RefreshData();
                        ProjectViewOverlay.ForceRefresh();
                    }
                }
                
                if (GUILayout.Button("Show", GUILayout.Width(80), GUILayout.Height(24)))
                {
                    PingFile(lockInfo.filePath);
                }
            }
            else
            {
                if (GUILayout.Button("Request", GUILayout.Width(100), GUILayout.Height(24)))
                {
                    EditorUtility.DisplayDialog(
                        "Request Unlock",
                        $"Contact {lockInfo.lockedBy.name} to unlock this file.\n\n" +
                        $"Email: {lockInfo.lockedBy.email}",
                        "OK"
                    );
                }
                
                if (GUILayout.Button("Force", GUILayout.Width(80), GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Force Unlock", 
                        $"Force unlock '{lockInfo.filePath}'?\n\nThis will remove {lockInfo.lockedBy.name}'s lock.", 
                        "Force Unlock", "Cancel"))
                    {
                        LockManager.Unlock(lockInfo.filePath, force: true);
                        RefreshData();
                        ProjectViewOverlay.ForceRefresh();
                    }
                }
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawEmptyState(string icon, string title, string message)
        {
            EditorGUILayout.Space(40);
            
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 48,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label(icon, iconStyle, GUILayout.Height(60));
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label(title, titleStyle);
            
            var msgStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            msgStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(message, msgStyle);
        }

        private void DrawSettingsTab()
        {
            var settings = SettingsManager.Settings;
            bool changed = false;
            
            EditorGUILayout.Space(10);
            
            // Git 정보
            EditorGUILayout.LabelField("Git Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_cardStyle);
            EditorGUILayout.LabelField("User", GitHelper.GetUserName());
            EditorGUILayout.LabelField("Email", GitHelper.GetUserEmail());
            EditorGUILayout.LabelField("Branch", GitHelper.GetCurrentBranch());
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(10);
            
            // UI 설정
            EditorGUILayout.LabelField("UI Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_cardStyle);
            
            EditorGUI.BeginChangeCheck();
            settings.showNotifications = EditorGUILayout.Toggle("Show Notifications", settings.showNotifications);
            settings.showProjectViewOverlay = EditorGUILayout.Toggle("Project View Icons", settings.showProjectViewOverlay);
            if (EditorGUI.EndChangeCheck()) changed = true;
            
            EditorGUILayout.EndVertical();
            
            // 저장
            if (changed)
            {
                SettingsManager.SaveSettings(settings);
            }
        }

        private void PingFile(string assetPath)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }
    }
}
