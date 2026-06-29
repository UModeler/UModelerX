using UnityEngine;
using UnityEditor;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
#if UNITY_2021_2_OR_NEWER
    using UnityEditor.Overlays;
#if UNITY_2022_1_OR_NEWER // 2022 ~ 6
    [Overlay(typeof(SceneView), "UMX Settings", true, defaultLayout = Layout.HorizontalToolbar, defaultDockZone = DockZone.TopToolbar)]
#else // 2021_2 ~ 2021_3
    [Overlay(typeof(SceneView), "UMX Settings", true)]
#endif
    [Icon("Assets/UModelerX/Editor/Resources/Tripolygon/UModelerX/Editor/Views/Toolbar/UMXSettings.png")]
    public class UMXSettingsToolbar : ToolbarOverlay
    {
        // UMX Toolbar 추가 시 생성자 목록에도 같이 추가되어야함. 동일한 string.
        public UMXSettingsToolbar() : base("SceneView/UMX Display Settings", "SceneView/UMX Snap Setting", "SceneView/UMX Coordinate System Setting")
        {

        }

#if !UNITY_2022_1_OR_NEWER
        public override void OnCreated()
        {
            UMXToolbarSetter.Init(this.containerWindow);
        }
#endif
    }
#elif UNITY_2020_1_OR_NEWER // 2020_1 ~ 2021_1
    using UnityEngine.UIElements;

    [InitializeOnLoad]
    public class UMXSettingsToolbar : VisualElement
    {
        private static ToolbarDropdownButton displaySettingButton;
        private static UMXSnapSettingToolbarButton snapSettingButton;
        private static UMXCoordinateSystemSettingToolbarButton coordinateSettingButton;

        static UMXSettingsToolbar()
        {
            UMXToolbarSetter.handler -= OnGUI;
            UMXToolbarSetter.handler += OnGUI;
            EditorApplication.update += Init;
        }

        // 2020 버전에서는 툴바 버튼 자동 등록이 아닌 개별 등록으로 임시 구현.
        private static void Init()
        {
            displaySettingButton = new UMXDisplaySettingsToolbarButton();
            coordinateSettingButton = new UMXCoordinateSystemSettingToolbarButton();
            snapSettingButton = new UMXSnapSettingToolbarButton();
            EditorApplication.update -= Init;
        }

        public static void OnGUI()
        {
            Vector2 position = UMXToolbarSetter.BasePosition;
            Vector2 size = new Vector2(UMXToolbarStyle.buttonStyle.fixedWidth, UMXToolbarStyle.buttonStyle.fixedHeight);
            UnityEngine.Rect leftRect = new UnityEngine.Rect(position, size);

            displaySettingButton.Render(leftRect);
            leftRect.position += Vector2.right * (displaySettingButton.Width + UMXToolbarSetter.padding);
            snapSettingButton.Render(leftRect);
            leftRect.position += Vector2.right * (snapSettingButton.Width + UMXToolbarSetter.padding);
            coordinateSettingButton.Render(leftRect);
        }
    }
    // UNITY_2020_1 ~ UNITY_2021_1
#endif
}
