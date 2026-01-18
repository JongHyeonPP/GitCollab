using UnityEditor;
using UnityEngine;

namespace GitCollab
{
    /// <summary>
    /// Theme-aware color utilities for Light/Dark editor themes
    /// </summary>
    public static class ThemeColors
    {
        /// <summary>
        /// Check if using Pro (dark) theme
        /// </summary>
        public static bool IsDarkTheme => EditorGUIUtility.isProSkin;

        // Lock status colors
        public static Color MyLockColor => IsDarkTheme 
            ? new Color(0.3f, 0.85f, 0.4f) 
            : new Color(0.1f, 0.6f, 0.2f);

        public static Color OtherLockColor => IsDarkTheme 
            ? new Color(0.95f, 0.35f, 0.35f) 
            : new Color(0.8f, 0.2f, 0.2f);

        public static Color ExpiredLockColor => IsDarkTheme 
            ? new Color(0.95f, 0.75f, 0.2f) 
            : new Color(0.7f, 0.55f, 0.1f);

        // UI colors
        public static Color AccentColor => IsDarkTheme 
            ? new Color(0.35f, 0.65f, 1f) 
            : new Color(0.2f, 0.5f, 0.9f);

        public static Color CardBackground => IsDarkTheme 
            ? new Color(0.22f, 0.22f, 0.22f) 
            : new Color(0.9f, 0.9f, 0.9f);

        public static Color HeaderBackground => IsDarkTheme 
            ? new Color(0.15f, 0.15f, 0.15f) 
            : new Color(0.8f, 0.8f, 0.8f);

        public static Color TextPrimary => IsDarkTheme 
            ? new Color(0.9f, 0.9f, 0.9f) 
            : new Color(0.1f, 0.1f, 0.1f);

        public static Color TextSecondary => IsDarkTheme 
            ? new Color(0.6f, 0.6f, 0.6f) 
            : new Color(0.4f, 0.4f, 0.4f);

        /// <summary>
        /// Create a lock icon with theme-appropriate colors
        /// </summary>
        public static Texture2D CreateLockIcon(LockIconType type, int size = 16)
        {
            Color color;
            switch (type)
            {
                case LockIconType.Mine:
                    color = MyLockColor;
                    break;
                case LockIconType.Other:
                    color = OtherLockColor;
                    break;
                case LockIconType.Expired:
                    color = ExpiredLockColor;
                    break;
                default:
                    color = AccentColor;
                    break;
            }

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color transparent = new Color(0, 0, 0, 0);
            Color outline = IsDarkTheme ? new Color(0, 0, 0, 0.8f) : new Color(0.3f, 0.3f, 0.3f, 0.8f);

            // Clear
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, transparent);
                }
            }

            // Lock body
            int bodyTop = size / 2;
            int bodyBottom = 2;
            int bodyLeft = 3;
            int bodyRight = size - 4;

            for (int y = bodyBottom; y <= bodyTop; y++)
            {
                for (int x = bodyLeft; x <= bodyRight; x++)
                {
                    if (y == bodyBottom || y == bodyTop || x == bodyLeft || x == bodyRight)
                        texture.SetPixel(x, y, outline);
                    else
                        texture.SetPixel(x, y, color);
                }
            }

            // Shackle
            int shackleLeft = size / 2 - 2;
            int shackleRight = size / 2 + 1;
            for (int y = bodyTop + 1; y < size - 2; y++)
            {
                texture.SetPixel(shackleLeft, y, outline);
                texture.SetPixel(shackleRight, y, outline);
            }
            for (int x = shackleLeft; x <= shackleRight; x++)
            {
                texture.SetPixel(x, size - 3, outline);
            }

            texture.Apply();
            return texture;
        }
    }

    public enum LockIconType
    {
        Mine,
        Other,
        Expired
    }
}
