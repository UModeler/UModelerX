using Tripolygon.UModelerX.Editor.Utilities.LayoutPreset;
using UnityEditor;

namespace Tripolygon.UModelerX.Editor.Importers.GLTF
{
    /// <summary>
    /// PureCode.Editor 어셈블리 로드 시 GlbMemoryLoader delegate를 등록한다.
    /// </summary>
    static class UMXGltfMemoryLoaderRegistration
    {
        [InitializeOnLoadMethod]
        static void Register()
        {
            PicoBerryAssetLibraryManager.GlbMemoryLoader = UMXGltfMemoryLoader.LoadFromGlbBytes;
        }
    }
}
