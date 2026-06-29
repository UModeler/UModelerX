using System;
using UnityEditor;
using System.Collections.Generic;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
    public class TextureProvider
    {
        private static readonly Dictionary<string, UnityEngine.Texture2D> textureByPath = new Dictionary<string, UnityEngine.Texture2D>();
        private static bool fallbackMissingLogged = false;
        public static UnityEngine.Texture2D CreateContent(Type type, string text)
        {
            if (string.IsNullOrEmpty(text) == false)
            {
                text = GetPath(type, text);
            }
            else
            {
                text = GetPath(type);
            }
            if (text != string.Empty)
            {
                if (Uri.TryCreate(text, UriKind.Absolute, out var uri) == true && uri.Scheme == "unity")
                {
                    return LoadBuiltInIcon(text);
                }
                else
                {
                    return Load(text);
                }
            }
            return null;
        }

        public static string GetPath(Type type, string text = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return $"{type.FullName.Replace(".", "/")}";
            }
            return $"{type.FullName.Replace(".", "/")}/{text}";
        }

        private static UnityEngine.Texture2D LoadBuiltInIcon(string name)
        {
            if (LoadBuiltInIconHigh(name) is UnityEngine.Texture2D highIcon)
            {
                return highIcon;
            }
            else if (LoadBuiltInIconLow(name) is UnityEngine.Texture2D lowIcon)
            {
                return lowIcon;
            }
            UnityEngine.Debug.LogWarning($"빌트인 아이콘 경로 '{name}' 을(를) 찾을 수가 없습니다.");
            return null;
        }

        private static UnityEngine.Texture2D LoadBuiltInIconLow(string name)
        {
            if (EditorGUIUtility.isProSkin == true)
            {
                if (name.StartsWith("d_") == false)
                {
                    if (LoadIcon($"d_{name}", false) is UnityEngine.Texture2D texture)
                    {
                        return texture;
                    }
                }
            }
            return LoadIcon(name, false);
        }

        private static UnityEngine.Texture2D LoadBuiltInIconHigh(string name)
        {
            if (EditorGUIUtility.isProSkin)
            {
                if (name.StartsWith("d_") == false)
                {
                    if (LoadIcon($"d_{name}", true) is UnityEngine.Texture2D texture)
                    {
                        return texture;
                    }
                }
            }
            return LoadIcon(name, true);
        }

        private static UnityEngine.Texture2D LoadIcon(string name, bool isHigh)
        {
            var path = isHigh == true ? $"{name}@2x" : name;
            if (UnityEngine.Resources.Load(path, typeof(UnityEngine.Texture2D)) is UnityEngine.Texture2D resourceTexture)
            {
                return resourceTexture;
            }
            if (GetIcon(name, isHigh) is UnityEngine.Texture2D builtinTexture)
            {
                return builtinTexture;
            }
            return null;
        }

        private static UnityEngine.Texture GetIcon(string iconPath, bool isHigh)
        {
            UnityEngine.Debug.unityLogger.logEnabled = false;
            var path = isHigh == true ? $"{iconPath}@2x" : iconPath;
            var content = UnityEditor.EditorGUIUtility.IconContent(path);
            UnityEngine.Debug.unityLogger.logEnabled = true;
            return content.image;
        }

        public static UnityEngine.Texture2D Load(string path)
        {
            if (UnityEditor.EditorGUIUtility.pixelsPerPoint > 1.0f && LoadHigh(path) is UnityEngine.Texture2D highTexture)
            {
                return highTexture;
            }
            else if (LoadLow(path) is UnityEngine.Texture2D lowTexture)
            {
                return lowTexture;
            }
            else
            {
                if (UnityEditor.EditorGUIUtility.pixelsPerPoint > 1.0f && LoadHigh(FallbackPath) is UnityEngine.Texture2D highFallback)
                {
                    return highFallback;
                }
                else if (LoadLow(FallbackPath) is UnityEngine.Texture2D lowFallback)
                {
                    return lowFallback;
                }
                else
                {
                    if (fallbackMissingLogged == false)
                    {
                        fallbackMissingLogged = true;
                        UnityEngine.Debug.LogWarning($"[TextureProvider] Fallback texture '{FallbackPath}' not found. It will recover automatically after asset import completes.");
                    }
                }
            }
            return null;
        }

        private static UnityEngine.Texture2D LoadLow(string path)
        {
            var directoryPath = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var itemName = System.IO.Path.GetFileName(path);
            var theme = EditorGUIUtility.isProSkin ? string.Empty : $"@Light";
            var texturePath = $"{directoryPath}{theme}/{itemName}";
            var resource = LoadResource(texturePath);
            if (resource == null && theme != "Dark")
            {
                var fallbackPath = $"{directoryPath}/{itemName}";
                resource = LoadResource(fallbackPath);
            }
            return resource;
        }

        private static string FallbackPath => "Tripolygon/None";

        private static UnityEngine.Texture2D LoadHigh(string path)
        {
            var directoryPath = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
            var itemName = System.IO.Path.GetFileName(path);
            var theme = EditorGUIUtility.isProSkin ? string.Empty : $"@Light";
            var texturePath = $"{directoryPath}{theme}/{itemName}@2x";
            var resource = LoadResource(texturePath);
            if (resource == null && theme != "Dark")
            {
                var fallbackPath = $"{directoryPath}/{itemName}@2x";
                resource = LoadResource(fallbackPath);
            }
            return resource;
        }

        private static UnityEngine.Texture2D LoadResource(string path)
        {
            if (textureByPath.TryGetValue(path, out var cached))
            {
                return cached;
            }
            var texture = Load(path, typeof(UnityEngine.Texture2D)) as UnityEngine.Texture2D;
            // null은 캐시하지 않음 — 도메인 리로드 타이밍에 따라 임포트 전 null이 고착되는 현상 방지
            if (texture != null)
            {
                textureByPath.Add(path, texture);
            }
            return texture;
        }

        private static UnityEngine.Object Load(string resourcePath, Type resourceType)
        {
            if (resourcePath == null)
                throw new ArgumentNullException(nameof(resourcePath));
            if (resourceType == null)
                throw new ArgumentNullException(nameof(resourceType));
            if (typeof(UnityEngine.Object).IsAssignableFrom(resourceType) == false)
                throw new ArgumentException($"'{resourceType}' must be a derived class of '{typeof(UnityEngine.Object)}'", nameof(resourceType));

            var obj = UnityEngine.Resources.Load(resourcePath, resourceType);
            return obj;
        }
    }
}
