#if UNITY_2021_2_OR_NEWER
using UnityEngine;
using UnityEditor;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;
using Tripolygon.UModelerX.Editor.Views.Toolbar;

namespace Tripolygon.UModelerX.Editor.MeshOps
{
    using UnityEditor.Overlays;

#if UNITY_2022_1_OR_NEWER
    [Overlay(typeof(SceneView), "UMX Mesh Ops", true, defaultLayout = Layout.HorizontalToolbar, defaultDockZone = DockZone.TopToolbar)]
#else
        [Overlay(typeof(SceneView), "UMX Mesh Ops", true)]
#endif
    [Icon("Assets/UModelerX/Editor/Resources/Tripolygon/UModelerX/Editor/Views/Toolbar/UMXSettings.png")]
    public class UMXMeshOpsToolbar : ToolbarOverlay
    {
        //UMX Toolbar 추가 시 생성자 목록에도 같이 추가되어야함.동일한 string.
        public UMXMeshOpsToolbar() : base("SceneView/UMX UModelerize", "SceneView/UMX Combine", "SceneView/UMX Split Parts",
            "SceneView/UMX Pivot", "SceneView/UMX Reset X Form")
        {
            UMXOverlayBootstrap.SignedInEvent += UMXOverlayBootstrap_SignedInEvent;
        }

        private void UMXOverlayBootstrap_SignedInEvent(object sender, System.EventArgs e)
        {
            EditorApplication.delayCall += () =>
            {
                this.displayed = this.displayed && UMXOverlayBootstrap.ConfigureVisibility();
            };
        }
    }
    // 2021.1 이하 버전은 일단 지원하지 않는 것으로 합니다.
}



#endif
