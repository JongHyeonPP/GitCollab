using UnityEditor;
using UnityEngine;
using System;

namespace GitCollab
{
    /// <summary>
    /// 잠금 상태 새로고침 매니저 (로컬 캐시 갱신만 수행)
    /// </summary>
    [InitializeOnLoad]
    public static class SyncManager
    {
        private static double _lastRefreshTime;
        private static bool _refreshInProgress = false;
        
        // 이벤트
        public static event Action OnSyncCompleted;

        static SyncManager()
        {
            // 에디터 시작 시 초기 로드
            EditorApplication.delayCall += () =>
            {
                Refresh();
            };
        }

        /// <summary>
        /// 잠금 상태 새로고침 (로컬 캐시만 갱신)
        /// </summary>
        public static void Refresh()
        {
            if (_refreshInProgress) return;
            _refreshInProgress = true;
            
            try
            {
                // 로컬 캐시 무효화 및 다시 읽기
                LockManager.InvalidateCache();
                TeamManager.InvalidateCache();
                
                // 프로젝트 뷰 갱신
                ProjectViewOverlay.ForceRefresh();
                
                OnSyncCompleted?.Invoke();
            }
            finally
            {
                _refreshInProgress = false;
                _lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        /// <summary>
        /// 강제 새로고침 (UI에서 호출)
        /// </summary>
        public static void ForceSync()
        {
            _lastRefreshTime = 0;
            Refresh();
        }

        /// <summary>
        /// 새로고침 진행 중 여부
        /// </summary>
        public static bool IsSyncing => _refreshInProgress;

        /// <summary>
        /// 마지막 새로고침으로부터 경과 시간 (초)
        /// </summary>
        public static float SecondsSinceLastSync => 
            (float)(EditorApplication.timeSinceStartup - _lastRefreshTime);
    }

    /// <summary>
    /// 새로고침 결과 (하위 호환성용)
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; } = true;
        public int TotalLocks { get; set; }
        public int NewLocks { get; set; }
        public int RemovedLocks { get; set; }
        public string Error { get; set; }
        public bool HasChanges => NewLocks > 0 || RemovedLocks > 0;
    }
}
