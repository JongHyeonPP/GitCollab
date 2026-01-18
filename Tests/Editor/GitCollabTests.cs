using NUnit.Framework;
using System.IO;

namespace GitCollab.Tests
{
    public class PathEncoderTests
    {
        [Test]
        public void Encode_ValidPath_ReturnsEncodedString()
        {
            string path = "Assets/Scenes/MainScene.unity";
            string encoded = PathEncoder.Encode(path);
            
            Assert.IsNotNull(encoded);
            Assert.IsNotEmpty(encoded);
            Assert.IsFalse(encoded.Contains("/"));
            Assert.IsFalse(encoded.Contains("+"));
        }

        [Test]
        public void Decode_EncodedPath_ReturnsOriginalPath()
        {
            string original = "Assets/Prefabs/Player.prefab";
            string encoded = PathEncoder.Encode(original);
            string decoded = PathEncoder.Decode(encoded);
            
            Assert.AreEqual(original, decoded);
        }

        [Test]
        public void Encode_NullPath_ReturnsNull()
        {
            string result = PathEncoder.Encode(null);
            Assert.IsNull(result);
        }

        [Test]
        public void Encode_EmptyPath_ReturnsNull()
        {
            string result = PathEncoder.Encode("");
            Assert.IsNull(result);
        }

        [Test]
        public void Decode_InvalidBase64_ReturnsNull()
        {
            string result = PathEncoder.Decode("not-valid-base64!!!");
            Assert.IsNull(result);
        }

        [Test]
        public void Encode_PathWithBackslashes_NormalizesToForwardSlashes()
        {
            string pathWithBackslash = "Assets\\Scenes\\Test.unity";
            string encoded = PathEncoder.Encode(pathWithBackslash);
            string decoded = PathEncoder.Decode(encoded);
            
            Assert.AreEqual("Assets/Scenes/Test.unity", decoded);
        }
    }

    public class LockManagerTests
    {
        [Test]
        public void IsLockableFile_UnityScene_ReturnsTrue()
        {
            Assert.IsTrue(LockManager.IsLockableFile("Assets/Scenes/Main.unity"));
        }

        [Test]
        public void IsLockableFile_Prefab_ReturnsTrue()
        {
            Assert.IsTrue(LockManager.IsLockableFile("Assets/Prefabs/Player.prefab"));
        }

        [Test]
        public void IsLockableFile_CSharpScript_ReturnsFalse()
        {
            Assert.IsFalse(LockManager.IsLockableFile("Assets/Scripts/PlayerController.cs"));
        }

        [Test]
        public void IsLockableFile_NullPath_ReturnsFalse()
        {
            Assert.IsFalse(LockManager.IsLockableFile(null));
        }

        [Test]
        public void IsLockableFile_EmptyPath_ReturnsFalse()
        {
            Assert.IsFalse(LockManager.IsLockableFile(""));
        }

        [Test]
        public void IsLockableFile_Material_ReturnsTrue()
        {
            Assert.IsTrue(LockManager.IsLockableFile("Assets/Materials/Ground.mat"));
        }

        [Test]
        public void IsLockableFile_FBX_ReturnsTrue()
        {
            Assert.IsTrue(LockManager.IsLockableFile("Assets/Models/Character.fbx"));
        }
    }

    public class ThemeColorsTests
    {
        [Test]
        public void MyLockColor_ReturnsValidColor()
        {
            var color = ThemeColors.MyLockColor;
            Assert.IsTrue(color.r >= 0 && color.r <= 1);
            Assert.IsTrue(color.g >= 0 && color.g <= 1);
            Assert.IsTrue(color.b >= 0 && color.b <= 1);
        }

        [Test]
        public void CreateLockIcon_ReturnsValidTexture()
        {
            var texture = ThemeColors.CreateLockIcon(LockIconType.Mine, 16);
            
            Assert.IsNotNull(texture);
            Assert.AreEqual(16, texture.width);
            Assert.AreEqual(16, texture.height);
        }
    }
}
