using UnityEditor;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
#if UNITY_2021_2_OR_NEWER
    using UnityEditor.Toolbars;
    [EditorToolbarElement("SceneView/UMX Snap Setting")]
    public class UMXSnapSettingToolbarButton : EditorToolbarDropdown
    {
        public UMXSnapSettingToolbarButton()
        {
            this.clicked += ShowDropdown;
            EditorApplication.update += Init;
        }

        private void Init()
        {
            this.icon = TextureProvider.CreateContent(typeof(UMXSnapSettingToolbarButton), null);
            this.tooltip = "UModelerX Snap Settings";
            EditorApplication.update -= Init;
        }

        private void ShowDropdown()
        {
            Tripolygon.UModelerX.Editor.Views.GridSnapping.UMXSnapSettingPopup.ShowPopup(this);
        }
    }
#elif UNITY_2020_1_OR_NEWER
    class UMXSnapSettingToolbarButton : ToolbarDropdownButton
    {
        public UMXSnapSettingToolbarButton() : base()
        {
            onClicked += Tripolygon.UModelerX.Editor.Views.GridSnapping.UMXSnapSettingPopup.ShowPopup;
        }
    }
    //UNITY_2021_2_OR_NEWER
#endif
}
