using System.Collections.Generic;
using System.Text;

using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    internal sealed class UMXGltfAssetContext
    {
        private readonly Dictionary<string, int> idCounters = new Dictionary<string, int>();
        private readonly AssetImportContext importContext;

        public UMXGltfAssetContext(AssetImportContext context)
        {
            importContext = context;
        }

        /// <summary>Standalone 모드: AssetImportContext 없이 메모리상에서만 빌드. AddObject는 no-op.</summary>
        public UMXGltfAssetContext()
        {
            importContext = null;
        }

        public void AddObject(string key, Object assetObject)
        {
            if (assetObject == null)
            {
                return;
            }

            var sanitizedKey = SanitizeKey(key);
            if (!idCounters.TryGetValue(sanitizedKey, out var count))
            {
                count = 0;
            }

            idCounters[sanitizedKey] = count + 1;
            if (importContext != null)
            {
                importContext.AddObjectToAsset($"{sanitizedKey}_{count}", assetObject);
            }
        }

        private static string SanitizeKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return "gltf";
            }

            var builder = new StringBuilder(key.Length);
            for (var i = 0; i < key.Length; ++i)
            {
                var ch = key[i];
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }
    }
}
