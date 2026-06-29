using UnityEditor;
using Tripolygon.UModelerX.Editor.MeshOpsProcessor;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
#if UNITY_2021_2_OR_NEWER
    using UnityEditor.Toolbars;
    [EditorToolbarElement("SceneView/UMX Display Settings")]
    public class UMXDisplaySettingsToolbarButton : EditorToolbarDropdown
    {
        public UMXDisplaySettingsToolbarButton()
        {
            this.clicked += ShowDropdown;
            EditorApplication.update += Init;
        }

        private void Init()
        {
            this.icon = TextureProvider.CreateContent(typeof(UMXDisplaySettingsToolbarButton), null);
            this.tooltip = "UModelerX Display Settings";
            EditorApplication.update -= Init;
        }

        private void ShowDropdown()
        {
            UMXDisplaySettingsPopup.ShowPopup(this);
        }
    }

#elif UNITY_2020_1_OR_NEWER
    public class UMXDisplaySettingsToolbarButton : ToolbarDropdownButton
    {
        public UMXDisplaySettingsToolbarButton() : base()
        {
            onClicked += UMXDisplaySettingsPopup.ShowPopup;
        }
    }
    // UNITY_2020_1 ~ UNITY_2021_1
#endif
}

