using System;
using System.Text;

namespace GitCollab
{
    /// <summary>
    /// 파일 경로 인코딩/디코딩 유틸리티
    /// </summary>
    public static class PathEncoder
    {
        /// <summary>
        /// 파일 경로를 Base64로 인코딩 (잠금 파일명으로 사용)
        /// </summary>
        public static string Encode(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            
            // 경로 정규화: 백슬래시를 슬래시로
            string normalized = filePath.Replace("\\", "/");
            byte[] bytes = Encoding.UTF8.GetBytes(normalized);
            
            // URL-safe Base64 (파일명에 사용 가능하도록)
            string base64 = Convert.ToBase64String(bytes);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        /// <summary>
        /// Base64 인코딩된 문자열을 원래 경로로 디코딩
        /// </summary>
        public static string Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded)) return null;
            
            try
            {
                // URL-safe Base64 복원
                string base64 = encoded.Replace("-", "+").Replace("_", "/");
                
                // 패딩 추가
                int padding = 4 - (base64.Length % 4);
                if (padding != 4)
                {
                    base64 += new string('=', padding);
                }
                
                byte[] bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 잠금 파일 경로 생성
        /// </summary>
        public static string GetLockFilePath(string assetPath)
        {
            string encoded = Encode(assetPath);
            return $".gitcollab/locks/{encoded}.lock";
        }

        /// <summary>
        /// 잠금 파일명에서 원본 에셋 경로 추출
        /// </summary>
        public static string GetAssetPathFromLockFile(string lockFileName)
        {
            // .lock 확장자 제거
            string encoded = lockFileName;
            if (encoded.EndsWith(".lock"))
            {
                encoded = encoded.Substring(0, encoded.Length - 5);
            }
            
            return Decode(encoded);
        }
    }
}
