using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GitCollab
{
    /// <summary>
    /// 팀 관리 시스템
    /// </summary>
    public static class TeamManager
    {
        private const string TEAM_FILE = ".gitcollab/team.json";
        private static TeamData _cachedTeam;

        /// <summary>
        /// 팀 데이터 로드
        /// </summary>
        public static TeamData GetTeam()
        {
            if (_cachedTeam != null) return _cachedTeam;
            
            string teamFilePath = GetTeamFilePath();
            if (!File.Exists(teamFilePath))
            {
                _cachedTeam = CreateDefaultTeam();
                SaveTeam(_cachedTeam);
                return _cachedTeam;
            }

            try
            {
                string json = File.ReadAllText(teamFilePath);
                _cachedTeam = JsonUtility.FromJson<TeamData>(json);
                return _cachedTeam;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GitCollab] Failed to load team: {ex.Message}");
                return CreateDefaultTeam();
            }
        }

        /// <summary>
        /// 팀 데이터 저장
        /// </summary>
        public static void SaveTeam(TeamData team)
        {
            string teamFilePath = GetTeamFilePath();
            string directory = Path.GetDirectoryName(teamFilePath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonUtility.ToJson(team, true);
            File.WriteAllText(teamFilePath, json);
            _cachedTeam = team;
        }

        /// <summary>
        /// 팀원 추가
        /// </summary>
        public static void AddMember(string name, string email, string role = "member")
        {
            var team = GetTeam();
            
            // 중복 확인
            foreach (var member in team.members)
            {
                if (member.email == email)
                {
                    Debug.LogWarning($"[GitCollab] Member already exists: {email}");
                    return;
                }
            }

            var newMember = new TeamMember
            {
                id = Guid.NewGuid().ToString(),
                name = name,
                email = email,
                role = role,
                joinedAt = DateTime.Now.ToString("o"),
                lastSeen = DateTime.Now.ToString("o"),
                color = GetRandomColor()
            };

            var membersList = new List<TeamMember>(team.members);
            membersList.Add(newMember);
            team.members = membersList.ToArray();
            
            SaveTeam(team);
        }

        /// <summary>
        /// 팀원 제거
        /// </summary>
        public static void RemoveMember(string email)
        {
            var team = GetTeam();
            var membersList = new List<TeamMember>(team.members);
            membersList.RemoveAll(m => m.email == email);
            team.members = membersList.ToArray();
            SaveTeam(team);
        }

        /// <summary>
        /// Git 히스토리에서 팀원 자동 감지
        /// </summary>
        public static List<TeamMember> DetectFromGitHistory()
        {
            var detected = new List<TeamMember>();
            var result = GitHelper.RunGitCommand("log --format=\"%an|%ae\" --all");
            
            if (!result.success) return detected;

            var seen = new HashSet<string>();
            var lines = result.output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var parts = line.Trim('"').Split('|');
                if (parts.Length != 2) continue;
                
                string email = parts[1];
                if (seen.Contains(email)) continue;
                seen.Add(email);

                detected.Add(new TeamMember
                {
                    id = Guid.NewGuid().ToString(),
                    name = parts[0],
                    email = email,
                    role = "member",
                    color = GetRandomColor()
                });
            }

            return detected;
        }

        /// <summary>
        /// 현재 사용자가 관리자인지 확인
        /// </summary>
        public static bool IsCurrentUserAdmin()
        {
            string myEmail = GitHelper.GetUserEmail();
            var team = GetTeam();
            
            foreach (var member in team.members)
            {
                if (member.email == myEmail && member.role == "admin")
                {
                    return true;
                }
            }
            return false;
        }

        private static TeamData CreateDefaultTeam()
        {
            return new TeamData
            {
                version = 1,
                projectName = Application.productName,
                created = DateTime.Now.ToString("o"),
                updated = DateTime.Now.ToString("o"),
                members = new[]
                {
                    new TeamMember
                    {
                        id = Guid.NewGuid().ToString(),
                        name = GitHelper.GetUserName(),
                        email = GitHelper.GetUserEmail(),
                        role = "admin",
                        joinedAt = DateTime.Now.ToString("o"),
                        lastSeen = DateTime.Now.ToString("o"),
                        color = "#58a6ff"
                    }
                }
            };
        }

        private static string GetTeamFilePath()
        {
            string repoRoot = GitHelper.GetRepoRoot();
            if (string.IsNullOrEmpty(repoRoot))
            {
                repoRoot = Directory.GetParent(Application.dataPath).FullName;
            }
            return Path.Combine(repoRoot, TEAM_FILE);
        }

        private static string GetRandomColor()
        {
            string[] colors = { "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD", "#98D8C8" };
            return colors[UnityEngine.Random.Range(0, colors.Length)];
        }

        public static void InvalidateCache()
        {
            _cachedTeam = null;
        }
    }

    [Serializable]
    public class TeamData
    {
        public int version;
        public string projectName;
        public string created;
        public string updated;
        public TeamMember[] members;
    }

    [Serializable]
    public class TeamMember
    {
        public string id;
        public string name;
        public string email;
        public string role;
        public string joinedAt;
        public string lastSeen;
        public string color;
    }
}
