using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace GitCollab
{
    /// <summary>
    /// Git 명령어 실행 헬퍼 클래스
    /// </summary>
    public static class GitHelper
    {
        private static string _cachedUserName;
        private static string _cachedUserEmail;
        private static string _cachedRepoRoot;

        /// <summary>
        /// Git 저장소 루트 경로 가져오기
        /// </summary>
        public static string GetRepoRoot()
        {
            if (_cachedRepoRoot != null) return _cachedRepoRoot;
            
            var result = RunGitCommand("rev-parse --show-toplevel");
            if (result.success)
            {
                _cachedRepoRoot = result.output.Trim().Replace("/", "\\");
            }
            return _cachedRepoRoot;
        }

        /// <summary>
        /// 현재 Git 사용자 이름 가져오기
        /// </summary>
        public static string GetUserName()
        {
            if (_cachedUserName != null) return _cachedUserName;
            
            var result = RunGitCommand("config user.name");
            if (result.success)
            {
                _cachedUserName = result.output.Trim();
            }
            return _cachedUserName ?? "Unknown";
        }

        /// <summary>
        /// 현재 Git 사용자 이메일 가져오기
        /// </summary>
        public static string GetUserEmail()
        {
            if (_cachedUserEmail != null) return _cachedUserEmail;
            
            var result = RunGitCommand("config user.email");
            if (result.success)
            {
                _cachedUserEmail = result.output.Trim();
            }
            return _cachedUserEmail ?? "unknown@unknown.com";
        }

        /// <summary>
        /// 현재 브랜치 이름 가져오기
        /// </summary>
        public static string GetCurrentBranch()
        {
            var result = RunGitCommand("rev-parse --abbrev-ref HEAD");
            return result.success ? result.output.Trim() : "unknown";
        }

        /// <summary>
        /// Git이 설치되어 있고 저장소인지 확인
        /// </summary>
        public static bool IsGitRepository()
        {
            var result = RunGitCommand("rev-parse --is-inside-work-tree");
            return result.success && result.output.Trim() == "true";
        }

        /// <summary>
        /// 파일 추가 (스테이징)
        /// </summary>
        public static bool Add(string filePath)
        {
            var result = RunGitCommand($"add \"{filePath}\"");
            return result.success;
        }

        /// <summary>
        /// 여러 파일 추가
        /// </summary>
        public static bool Add(IEnumerable<string> filePaths)
        {
            var sb = new StringBuilder("add");
            foreach (var path in filePaths)
            {
                sb.Append($" \"{path}\"");
            }
            var result = RunGitCommand(sb.ToString());
            return result.success;
        }

        /// <summary>
        /// 커밋
        /// </summary>
        public static bool Commit(string message)
        {
            var result = RunGitCommand($"commit -m \"{message}\"");
            return result.success;
        }

        /// <summary>
        /// 푸시
        /// </summary>
        public static bool Push()
        {
            var result = RunGitCommand("push");
            return result.success;
        }

        /// <summary>
        /// Fetch (원격 저장소 정보 가져오기)
        /// </summary>
        public static bool Fetch()
        {
            var result = RunGitCommand("fetch --quiet");
            return result.success;
        }

        /// <summary>
        /// Pull
        /// </summary>
        public static bool Pull()
        {
            var result = RunGitCommand("pull");
            return result.success;
        }

        /// <summary>
        /// 파일의 SHA-1 해시 가져오기
        /// </summary>
        public static string GetFileHash(string filePath)
        {
            var result = RunGitCommand($"hash-object \"{filePath}\"");
            return result.success ? result.output.Trim() : null;
        }

        /// <summary>
        /// Git 명령 실행
        /// </summary>
        public static (bool success, string output, string error) RunGitCommand(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Application.dataPath
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    bool success = process.ExitCode == 0;
                    return (success, output, error);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[GitCollab] Git command failed: {ex.Message}");
                return (false, null, ex.Message);
            }
        }

        /// <summary>
        /// 캐시 초기화 (사용자 정보 변경 시)
        /// </summary>
        public static void ClearCache()
        {
            _cachedUserName = null;
            _cachedUserEmail = null;
            _cachedRepoRoot = null;
        }
    }
}
