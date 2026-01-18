using System.IO;
using UnityEngine;
using UnityEditor;

namespace GitCollab
{
    /// <summary>
    /// Git Hooks 자동 설치 및 관리
    /// </summary>
    [InitializeOnLoad]
    public static class GitHookInstaller
    {
        private const string HOOK_MARKER = "# Git Collab for Unity - DO NOT EDIT BELOW";
        private const string HOOK_VERSION = "1.0.0";

        static GitHookInstaller()
        {
            // 에디터 시작 시 Hook 설치 확인
            EditorApplication.delayCall += () =>
            {
                if (GitHelper.IsGitRepository())
                {
                    InstallHooksIfNeeded();
                }
            };
        }

        /// <summary>
        /// 필요 시 Hook 설치
        /// </summary>
        public static void InstallHooksIfNeeded()
        {
            string hooksDir = GetHooksDirectory();
            if (string.IsNullOrEmpty(hooksDir)) return;

            if (!Directory.Exists(hooksDir))
            {
                Directory.CreateDirectory(hooksDir);
            }

            InstallHook(hooksDir, "pre-commit", GetPreCommitHook());
            InstallHook(hooksDir, "pre-push", GetPrePushHook());
        }

        /// <summary>
        /// 모든 Hook 강제 재설치
        /// </summary>
        public static void ReinstallHooks()
        {
            string hooksDir = GetHooksDirectory();
            if (string.IsNullOrEmpty(hooksDir)) return;

            ForceInstallHook(hooksDir, "pre-commit", GetPreCommitHook());
            ForceInstallHook(hooksDir, "pre-push", GetPrePushHook());

            Debug.Log("[GitCollab] Git hooks reinstalled successfully.");
        }

        /// <summary>
        /// 모든 Hook 제거
        /// </summary>
        public static void RemoveHooks()
        {
            string hooksDir = GetHooksDirectory();
            if (string.IsNullOrEmpty(hooksDir)) return;

            RemoveHook(hooksDir, "pre-commit");
            RemoveHook(hooksDir, "pre-push");

            Debug.Log("[GitCollab] Git hooks removed.");
        }

        private static string GetHooksDirectory()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot)) return null;
            return Path.Combine(repoRoot, ".git", "hooks");
        }

        private static void InstallHook(string hooksDir, string hookName, string hookContent)
        {
            string hookPath = Path.Combine(hooksDir, hookName);

            if (File.Exists(hookPath))
            {
                string existing = File.ReadAllText(hookPath);
                if (existing.Contains(HOOK_MARKER))
                {
                    // 이미 설치됨 - 버전 체크
                    if (existing.Contains($"Version: {HOOK_VERSION}"))
                    {
                        return; // 최신 버전
                    }
                    // 구버전 - 업데이트 필요
                    ForceInstallHook(hooksDir, hookName, hookContent);
                    return;
                }

                // 기존 Hook이 있음 - 병합
                hookContent = existing + "\n\n" + hookContent;
            }

            File.WriteAllText(hookPath, hookContent);
            Debug.Log($"[GitCollab] Installed {hookName} hook.");
        }

        private static void ForceInstallHook(string hooksDir, string hookName, string hookContent)
        {
            string hookPath = Path.Combine(hooksDir, hookName);

            if (File.Exists(hookPath))
            {
                string existing = File.ReadAllText(hookPath);
                
                // 기존 GitCollab 섹션 제거
                int markerIndex = existing.IndexOf(HOOK_MARKER);
                if (markerIndex >= 0)
                {
                    existing = existing.Substring(0, markerIndex).TrimEnd();
                }

                if (!string.IsNullOrEmpty(existing))
                {
                    hookContent = existing + "\n\n" + hookContent;
                }
            }

            File.WriteAllText(hookPath, hookContent);
        }

        private static void RemoveHook(string hooksDir, string hookName)
        {
            string hookPath = Path.Combine(hooksDir, hookName);
            if (!File.Exists(hookPath)) return;

            string existing = File.ReadAllText(hookPath);
            int markerIndex = existing.IndexOf(HOOK_MARKER);
            
            if (markerIndex < 0) return; // GitCollab Hook 없음

            string cleaned = existing.Substring(0, markerIndex).TrimEnd();
            
            if (string.IsNullOrEmpty(cleaned))
            {
                File.Delete(hookPath);
            }
            else
            {
                File.WriteAllText(hookPath, cleaned);
            }
        }

        private static string GetPreCommitHook()
        {
            return $@"
{HOOK_MARKER}
# Version: {HOOK_VERSION}
#===================================================================
# Git Collab for Unity - Pre-commit Hook
# Prevents committing files locked by other team members
#===================================================================

LOCKS_DIR="".gitcollab/locks""
MY_EMAIL=$(git config user.email)
STAGED_FILES=$(git diff --cached --name-only)
EXIT_CODE=0

for FILE in $STAGED_FILES; do
    # Skip lock files themselves
    if [[ ""$FILE"" == .gitcollab/* ]]; then
        continue
    fi
    
    # Find matching lock file (Base64 encoded path)
    ENCODED=$(echo -n ""$FILE"" | base64 | tr -d '\n' | tr '+' '-' | tr '/' '_' | tr -d '=')
    LOCK_FILE=""$LOCKS_DIR/${{ENCODED}}.lock""
    
    if [ -f ""$LOCK_FILE"" ]; then
        OWNER_EMAIL=$(grep -o '""email"": *""[^""]*""' ""$LOCK_FILE"" | head -1 | cut -d'""' -f4)
        OWNER_NAME=$(grep -o '""name"": *""[^""]*""' ""$LOCK_FILE"" | head -1 | cut -d'""' -f4)
        
        if [ ""$OWNER_EMAIL"" != ""$MY_EMAIL"" ]; then
            echo ""❌ BLOCKED: '$FILE'""
            echo ""   └─ Locked by: $OWNER_NAME <$OWNER_EMAIL>""
            echo ""   └─ Request unlock in Unity or contact the owner.""
            EXIT_CODE=1
        fi
    fi
done

if [ $EXIT_CODE -ne 0 ]; then
    echo """"
    echo ""============================================""
    echo ""  Commit rejected by Git Collab""
    echo ""  Unlock files first or use --no-verify""
    echo ""============================================""
fi

exit $EXIT_CODE
# End of Git Collab Hook
";
        }

        private static string GetPrePushHook()
        {
            return $@"
{HOOK_MARKER}
# Version: {HOOK_VERSION}
#===================================================================
# Git Collab for Unity - Pre-push Hook
# Warns if local lock state may be outdated
#===================================================================

LOCKS_DIR="".gitcollab/locks""

# Attempt to fetch latest lock state (silent, non-blocking)
git fetch origin --quiet 2>/dev/null || true

echo ""[Git Collab] Push proceeding. Remember to sync locks with team.""
exit 0
# End of Git Collab Hook
";
        }
    }
}
