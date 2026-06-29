using UnityEditor;
using System;
using System.Reflection;
using UnityEngine;

namespace Tripolygon.UModelerX.Editor.Views.Toolbar
{
    using Tripolygon.UModeler.UI;
#if UNITY_2021_2_OR_NEWER
    using UnityEditor.Toolbars;
    [EditorToolbarElement("SceneView/UMX Coordinate System Setting")]
    public class UMXCoordinateSystemSettingToolbarButton : EditorToolbarDropdown
    {
        MethodInfo repaintMethod;

        public UMXCoordinateSystemSettingToolbarButton()
        {
            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            repaintMethod = toolbarType.GetMethod("RepaintToolbar", BindingFlags.NonPublic | BindingFlags.Static);
            var dropdown = new UMXCoordinateSystemSettingToolbarDropdown(this);
            this.clicked += UMXCoordinateSystemSettingToolbarDropdown.ShowDropdown;
            Tools.pivotRotationChanged -= OnPivotRotationChanged;
            Tools.pivotRotationChanged += OnPivotRotationChanged;
            EditorApplication.update += Init;

        }

        private void Init()
        {
            this.text = Enum.GetName(typeof(UMXCoordinateSystem), CoordinateSystemSetting.CoordinateSystem);
            this.icon = TextureProvider.CreateContent(typeof(UMXCoordinateSystemSettingToolbarButton), this.text);
            this.tooltip = "UModelerX Coordinate System";
            EditorApplication.update -= Init;
        }
        // UNITY_2021_2_OR_NEWER
#elif UNITY_2020_1_OR_NEWER
    public class UMXCoordinateSystemSettingToolbarButton : ToolbarDropdownButton
    {
        Type toolbarType;
        MethodInfo repaintMethod;
        private PivotRotation editorPivot;

        public UMXCoordinateSystemSettingToolbarButton() : base()
        {
            toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            repaintMethod = toolbarType.GetMethod("RepaintToolbar", BindingFlags.NonPublic | BindingFlags.Static);
            this.text = Enum.GetName(typeof(UMXCoordinateSystem), CoordinateSystemSetting.CoordinateSystem);
            editorPivot = Tools.pivotRotation;
            UMXCoordinateSystemSettingToolbarDropdown dropdown = new UMXCoordinateSystemSettingToolbarDropdown(this);
            onClicked += UMXCoordinateSystemSettingToolbarDropdown.ShowDropdown;
        }

        public override void Render(Rect rect)
        {
            // Tools pivotRotation 변경 확인
            if (editorPivot != Tools.pivotRotation)
            {
                editorPivot = Tools.pivotRotation;
                OnPivotRotationChanged();
            }

            base.Render(rect);
        }
#endif //UNITY_2020_1_OR_NEWER

        // 2020, 2021.2 ~ 6 공유 메소드
        private void OnPivotRotationChanged()
        {
            if (Tools.pivotRotation == PivotRotation.Global)
            {
                CoordinateSystemSetting.CoordinateSystem = UMXCoordinateSystem.Global;
                this.text = nameof(UMXCoordinateSystem.Global);
                this.icon = TextureProvider.CreateContent(typeof(UMXCoordinateSystemSettingToolbarButton), nameof(UMXCoordinateSystem.Global));
            }
            else if (Tools.pivotRotation == PivotRotation.Local)
            {
                CoordinateSystemSetting.CoordinateSystem = UMXCoordinateSystem.Local;
                this.text = nameof(UMXCoordinateSystem.Local);
                this.icon = TextureProvider.CreateContent(typeof(UMXCoordinateSystemSettingToolbarButton), nameof(UMXCoordinateSystem.Local));
            }
            repaintMethod?.Invoke(null, null);
        }
    }
}
