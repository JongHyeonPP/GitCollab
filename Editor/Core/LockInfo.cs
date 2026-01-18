using System;

namespace GitCollab
{
    /// <summary>
    /// 파일 잠금 정보를 담는 데이터 클래스
    /// </summary>
    [Serializable]
    public class LockInfo
    {
        public int version = 1;
        public string filePath;
        public string fileHash;
        public LockOwner lockedBy;
        public string lockedAt;
        public string expiresAt;
        public string reason;
        public string branch;
        public string machineId;

        [Serializable]
        public class LockOwner
        {
            public string name;
            public string email;
        }

        /// <summary>
        /// 현재 사용자가 이 잠금의 소유자인지 확인
        /// </summary>
        public bool IsOwnedByMe => lockedBy?.email == GitHelper.GetUserEmail();

        /// <summary>
        /// 잠금이 만료되었는지 확인
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (string.IsNullOrEmpty(expiresAt)) return false;
                if (DateTime.TryParse(expiresAt, out DateTime expiry))
                {
                    return DateTime.Now > expiry;
                }
                return false;
            }
        }

        /// <summary>
        /// 잠금 시간으로부터 경과 시간 (사람이 읽기 쉬운 형식)
        /// </summary>
        public string TimeSinceLock
        {
            get
            {
                if (!DateTime.TryParse(lockedAt, out DateTime lockTime))
                    return "Unknown";

                var elapsed = DateTime.Now - lockTime;
                if (elapsed.TotalMinutes < 1) return "Just now";
                if (elapsed.TotalMinutes < 60) return $"{(int)elapsed.TotalMinutes} min ago";
                if (elapsed.TotalHours < 24) return $"{(int)elapsed.TotalHours} hours ago";
                return $"{(int)elapsed.TotalDays} days ago";
            }
        }
    }
}
