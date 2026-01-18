using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace GitCollab
{
    /// <summary>
    /// 파일 잠금 관리 핵심 클래스
    /// </summary>
    public static class LockManager
    {
        private const string LOCKS_FOLDER = ".gitcollab/locks";
        private const string CONFIG_FOLDER = ".gitcollab";
        
        private static Dictionary<string, LockInfo> _lockCache = new Dictionary<string, LockInfo>();
        private static bool _cacheValid = false;
        
        /// <summary>
        /// 잠금 가능한 파일 확장자 목록
        /// </summary>
        public static readonly string[] LockableExtensions = new[]
        {
            ".unity", ".prefab", ".asset", ".controller",
            ".mat", ".png", ".jpg", ".jpeg", ".tga", ".psd",
            ".fbx", ".obj", ".blend", ".max",
            ".wav", ".mp3", ".ogg", ".aiff",
            ".anim", ".mask", ".overrideController"
        };

        /// <summary>
        /// Git Collab 폴더 경로 가져오기
        /// </summary>
        private static string GetGitCollabPath()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                // 폴백: Unity 프로젝트 루트 사용
                repoRoot = Directory.GetParent(Application.dataPath).FullName;
            }
            return Path.Combine(repoRoot, CONFIG_FOLDER);
        }

        /// <summary>
        /// 잠금 폴더 경로 가져오기
        /// </summary>
        private static string GetLocksPath()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                repoRoot = Directory.GetParent(Application.dataPath).FullName;
            }
            return Path.Combine(repoRoot, LOCKS_FOLDER);
        }

        /// <summary>
        /// 파일이 잠금 가능한 타입인지 확인
        /// </summary>
        public static bool IsLockableFile(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return false;
            
            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            foreach (var lockableExt in LockableExtensions)
            {
                if (ext == lockableExt) return true;
            }
            return false;
        }

        /// <summary>
        /// 파일 잠금
        /// </summary>
        public static LockResult Lock(string assetPath, string reason = null)
        {
            if (!GitHelper.IsGitRepository())
            {
                return new LockResult(false, "Not a Git repository.");
            }

            if (!IsLockableFile(assetPath))
            {
                return new LockResult(false, "This file type cannot be locked.");
            }

            // 이미 잠겨있는지 확인
            var existingLock = GetLockInfo(assetPath);
            if (existingLock != null)
            {
                if (existingLock.IsOwnedByMe)
                {
                    return new LockResult(false, "Already locked by you.");
                }
                return new LockResult(false, $"Locked by '{existingLock.lockedBy.name}'.");
            }

            // 잠금 폴더 생성
            string locksPath = GetLocksPath();
            if (!Directory.Exists(locksPath))
            {
                Directory.CreateDirectory(locksPath);
            }

            // 잠금 정보 생성
            var lockInfo = new LockInfo
            {
                version = 1,
                filePath = assetPath,
                fileHash = GitHelper.GetFileHash(assetPath),
                lockedBy = new LockInfo.LockOwner
                {
                    name = GitHelper.GetUserName(),
                    email = GitHelper.GetUserEmail()
                },
                lockedAt = DateTime.Now.ToString("o"),
                expiresAt = DateTime.Now.AddHours(24).ToString("o"),
                reason = reason ?? "Working",
                branch = GitHelper.GetCurrentBranch(),
                machineId = Environment.MachineName
            };

            // 잠금 파일 저장
            string lockFilePath = GetLockFilePath(assetPath);
            string json = JsonUtility.ToJson(lockInfo, true);
            File.WriteAllText(lockFilePath, json);

            // 캐시 업데이트
            _lockCache[assetPath] = lockInfo;

            // Git에 추가 (선택적 자동 커밋)
            GitHelper.Add(lockFilePath);

            // Record history
            LockHistory.RecordLock(assetPath, lockInfo.reason);

            return new LockResult(true, "Locked successfully.", lockInfo);
        }

        /// <summary>
        /// 파일 잠금 해제
        /// </summary>
        public static LockResult Unlock(string assetPath, bool force = false)
        {
            var lockInfo = GetLockInfo(assetPath);
            if (lockInfo == null)
            {
                return new LockResult(false, "This file is not locked.");
            }

            if (!lockInfo.IsOwnedByMe && !force)
            {
                return new LockResult(false, $"Locked by '{lockInfo.lockedBy.name}'. Force unlock required.");
            }

            // 잠금 파일 삭제
            string lockFilePath = GetLockFilePath(assetPath);
            if (File.Exists(lockFilePath))
            {
                File.Delete(lockFilePath);
                
                // Git에서 제거
                GitHelper.RunGitCommand($"rm --cached \"{lockFilePath}\"");
            }

            // 캐시에서 제거
            _lockCache.Remove(assetPath);

            // Record history
            LockHistory.RecordUnlock(assetPath, force);

            return new LockResult(true, "Unlocked successfully.");
        }

        /// <summary>
        /// 파일의 잠금 정보 가져오기
        /// </summary>
        public static LockInfo GetLockInfo(string assetPath)
        {
            // 캐시 확인
            if (_lockCache.TryGetValue(assetPath, out LockInfo cached))
            {
                return cached;
            }

            // 파일에서 읽기
            string lockFilePath = GetLockFilePath(assetPath);
            if (!File.Exists(lockFilePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(lockFilePath);
                var lockInfo = JsonUtility.FromJson<LockInfo>(json);
                _lockCache[assetPath] = lockInfo;
                return lockInfo;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitCollab] Failed to read lock file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 파일이 잠겨있는지 확인
        /// </summary>
        public static bool IsLocked(string assetPath)
        {
            return GetLockInfo(assetPath) != null;
        }

        /// <summary>
        /// 파일을 잠글 수 있는지 확인
        /// </summary>
        public static bool CanLock(string assetPath)
        {
            if (!IsLockableFile(assetPath)) return false;
            if (!GitHelper.IsGitRepository()) return false;
            return !IsLocked(assetPath);
        }

        /// <summary>
        /// 파일을 해제할 수 있는지 확인 (내 잠금만)
        /// </summary>
        public static bool CanUnlock(string assetPath)
        {
            var lockInfo = GetLockInfo(assetPath);
            return lockInfo != null && lockInfo.IsOwnedByMe;
        }

        /// <summary>
        /// 모든 잠금 목록 가져오기
        /// </summary>
        public static List<LockInfo> GetAllLocks()
        {
            var locks = new List<LockInfo>();
            string locksPath = GetLocksPath();
            
            if (!Directory.Exists(locksPath))
            {
                return locks;
            }

            foreach (string lockFile in Directory.GetFiles(locksPath, "*.lock"))
            {
                try
                {
                    string json = File.ReadAllText(lockFile);
                    var lockInfo = JsonUtility.FromJson<LockInfo>(json);
                    if (lockInfo != null)
                    {
                        locks.Add(lockInfo);
                        _lockCache[lockInfo.filePath] = lockInfo;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GitCollab] Failed to read lock file {lockFile}: {ex.Message}");
                }
            }

            return locks;
        }

        /// <summary>
        /// 내 잠금 목록 가져오기
        /// </summary>
        public static List<LockInfo> GetMyLocks()
        {
            var myLocks = new List<LockInfo>();
            foreach (var lockInfo in GetAllLocks())
            {
                if (lockInfo.IsOwnedByMe)
                {
                    myLocks.Add(lockInfo);
                }
            }
            return myLocks;
        }

        /// <summary>
        /// 다른 사람의 잠금 목록 가져오기
        /// </summary>
        public static List<LockInfo> GetOtherLocks()
        {
            var otherLocks = new List<LockInfo>();
            foreach (var lockInfo in GetAllLocks())
            {
                if (!lockInfo.IsOwnedByMe)
                {
                    otherLocks.Add(lockInfo);
                }
            }
            return otherLocks;
        }

        /// <summary>
        /// 캐시 무효화
        /// </summary>
        public static void InvalidateCache()
        {
            _lockCache.Clear();
            _cacheValid = false;
        }

        /// <summary>
        /// 잠금 파일 경로 계산
        /// </summary>
        private static string GetLockFilePath(string assetPath)
        {
            string locksPath = GetLocksPath();
            string encoded = PathEncoder.Encode(assetPath);
            return Path.Combine(locksPath, encoded + ".lock");
        }

        /// <summary>
        /// Cleanup expired locks
        /// </summary>
        public static int CleanupExpiredLocks()
        {
            int cleaned = 0;
            string locksPath = GetLocksPath();
            
            if (!Directory.Exists(locksPath)) return 0;

            foreach (string lockFile in Directory.GetFiles(locksPath, "*.lock"))
            {
                try
                {
                    string json = File.ReadAllText(lockFile);
                    var lockInfo = JsonUtility.FromJson<LockInfo>(json);
                    
                    if (lockInfo != null && lockInfo.IsExpired)
                    {
                        File.Delete(lockFile);
                        _lockCache.Remove(lockInfo.filePath);
                        cleaned++;
                        Debug.Log($"[GitCollab] Cleaned expired lock: {lockInfo.filePath}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[GitCollab] Failed to cleanup lock file: {ex.Message}");
                }
            }

            if (cleaned > 0)
            {
                InvalidateCache();
            }

            return cleaned;
        }

        /// <summary>
        /// Lock all lockable files in a folder
        /// </summary>
        public static List<LockResult> LockFolder(string folderPath, string reason = null)
        {
            var results = new List<LockResult>();
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"[GitCollab] Invalid folder: {folderPath}");
                results.Add(new LockResult(false, "Not a valid folder."));
                return results;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
            Debug.Log($"[GitCollab] Found {guids.Length} assets in folder: {folderPath}");
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // Skip folders
                if (AssetDatabase.IsValidFolder(assetPath)) continue;
                
                if (IsLockableFile(assetPath))
                {
                    if (CanLock(assetPath))
                    {
                        var result = Lock(assetPath, reason);
                        results.Add(result);
                    }
                    else
                    {
                        // Already locked or other reason
                        var lockInfo = GetLockInfo(assetPath);
                        if (lockInfo != null)
                        {
                            Debug.Log($"[GitCollab] Skipped (already locked): {assetPath}");
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Unlock all my locks in a folder
        /// </summary>
        public static List<LockResult> UnlockFolder(string folderPath)
        {
            var results = new List<LockResult>();
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                results.Add(new LockResult(false, "Not a valid folder."));
                return results;
            }

            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                if (CanUnlock(assetPath))
                {
                    var result = Unlock(assetPath);
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// .gitcollab 폴더 초기화
        /// </summary>
        public static void EnsureInitialized()
        {
            string gitCollabPath = GetGitCollabPath();
            string locksPath = GetLocksPath();

            if (!Directory.Exists(gitCollabPath))
            {
                Directory.CreateDirectory(gitCollabPath);
            }

            if (!Directory.Exists(locksPath))
            {
                Directory.CreateDirectory(locksPath);
            }

            // .gitignore에서 .gitcollab 제외 안 되도록 확인
            // (이 폴더는 Git에 포함되어야 함)
        }
    }

    /// <summary>
    /// 잠금 작업 결과
    /// </summary>
    public class LockResult
    {
        public bool Success { get; }
        public string Message { get; }
        public LockInfo LockInfo { get; }

        public LockResult(bool success, string message, LockInfo lockInfo = null)
        {
            Success = success;
            Message = message;
            LockInfo = lockInfo;
        }
    }
}
